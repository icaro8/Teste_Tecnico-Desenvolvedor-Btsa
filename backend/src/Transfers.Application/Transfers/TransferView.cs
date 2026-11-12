using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record TransferView(
    Guid Id,
    TransferKind Kind,
    TransferStatus Status,
    Guid SourceAccountId,
    string SourceHolderName,
    Guid DestinationAccountId,
    string DestinationHolderName,
    decimal Amount,
    TransferFailureReason? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? CancelledAt)
{
    public static TransferView From(Transfer transfer, Account source, Account destination) => new(
        transfer.Id,
        transfer.Kind,
        transfer.Status,
        source.Id,
        source.HolderName,
        destination.Id,
        destination.HolderName,
        transfer.Amount,
        transfer.FailureReason,
        transfer.CreatedAt,
        transfer.ScheduledFor,
        transfer.ProcessedAt,
        transfer.CompletedAt,
        transfer.FailedAt,
        transfer.CancelledAt);
}
