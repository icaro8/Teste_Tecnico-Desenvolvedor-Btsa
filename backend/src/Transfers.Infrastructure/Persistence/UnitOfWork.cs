using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Infrastructure.Persistence.Outbox;

namespace Transfers.Infrastructure.Persistence;

internal sealed class UnitOfWork(TransfersDbContext db) : IUnitOfWork
{
    public Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async token =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(token);

            var outcome = await work(token);
            if (outcome.IsFailure)
            {
                await transaction.RollbackAsync(token);
                return outcome;
            }

            // Os eventos vão para o outbox na mesma transação: ou a mudança de estado e o
            // evento são gravados juntos, ou nenhum dos dois. O OutboxWorker os entrega depois.
            db.OutboxMessages.AddRange(CollectDomainEvents().Select(OutboxMessage.From));
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return outcome;
        }, cancellationToken);
    }

    private IDomainEvent[] CollectDomainEvents()
    {
        var aggregates = db.ChangeTracker.Entries<AggregateRoot>().Select(entry => entry.Entity).ToList();
        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToArray();
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
        return domainEvents;
    }
}
