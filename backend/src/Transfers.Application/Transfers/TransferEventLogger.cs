using Microsoft.Extensions.Logging;
using Transfers.Application.Abstractions;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

internal sealed partial class TransferEventLogger(ILogger<TransferEventLogger> logger) :
    IDomainEventHandler<TransferScheduled>,
    IDomainEventHandler<TransferCompleted>,
    IDomainEventHandler<TransferFailed>,
    IDomainEventHandler<TransferCancelled>
{
    public Task HandleAsync(TransferScheduled domainEvent, CancellationToken cancellationToken)
    {
        LogScheduled(domainEvent.TransferId, domainEvent.ScheduledFor);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransferCompleted domainEvent, CancellationToken cancellationToken)
    {
        LogCompleted(domainEvent.TransferId, domainEvent.SourceAccountId, domainEvent.DestinationAccountId, domainEvent.Amount);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransferFailed domainEvent, CancellationToken cancellationToken)
    {
        LogFailed(domainEvent.TransferId, domainEvent.Reason);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TransferCancelled domainEvent, CancellationToken cancellationToken)
    {
        LogCancelled(domainEvent.TransferId);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Transfer {TransferId} scheduled for {ScheduledFor}")]
    private partial void LogScheduled(Guid transferId, DateTimeOffset scheduledFor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transfer {TransferId} completed: {Amount} from {SourceAccountId} to {DestinationAccountId}")]
    private partial void LogCompleted(Guid transferId, Guid sourceAccountId, Guid destinationAccountId, decimal amount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transfer {TransferId} failed: {Reason}")]
    private partial void LogFailed(Guid transferId, TransferFailureReason reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transfer {TransferId} cancelled")]
    private partial void LogCancelled(Guid transferId);
}
