using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Transfers.Infrastructure.Persistence.Outbox;

internal sealed partial class OutboxProcessor(
    TransfersDbContext db,
    DomainEventDispatcher dispatcher,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxProcessor> logger)
{
    /// <summary>
    /// Entrega um lote de eventos pendentes. Usa <c>FOR UPDATE SKIP LOCKED</c>, então várias
    /// instâncias podem rodar ao mesmo tempo sem entregar a mesma mensagem duas vezes.
    /// </summary>
    /// <returns>Quantas mensagens foram lidas do lote (entregues ou com falha).</returns>
    public Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(token);

            var batchSize = options.Value.BatchSize;
            var maxAttempts = options.Value.MaxAttempts;
            var ids = await db.Database
                .SqlQuery<Guid>($"""
                    SELECT id AS "Value" FROM outbox_messages
                    WHERE processed_at IS NULL AND attempts < {maxAttempts}
                    ORDER BY occurred_at, id
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(token);

            if (ids.Count == 0)
            {
                await transaction.CommitAsync(token);
                return 0;
            }

            var messages = await db.OutboxMessages
                .Where(message => ids.Contains(message.Id))
                .OrderBy(message => message.OccurredAt)
                .ThenBy(message => message.Id)
                .ToListAsync(token);

            foreach (var message in messages)
            {
                await DeliverAsync(message, token);
            }

            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return messages.Count;
        }, cancellationToken);
    }

    private async Task DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await dispatcher.DispatchAsync([OutboxSerializer.Deserialize(message)], cancellationToken);
            message.MarkProcessed(clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.MarkFailed(exception.Message);
            LogDeliveryFailure(exception, message.Id, message.Type, message.Attempts);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({EventType}) failed on attempt {Attempt}; it will be retried")]
    private partial void LogDeliveryFailure(Exception exception, Guid messageId, string eventType, int attempt);
}
