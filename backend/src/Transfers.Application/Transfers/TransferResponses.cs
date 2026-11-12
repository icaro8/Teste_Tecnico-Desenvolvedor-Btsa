using Transfers.Application.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record TransferResponse(
    Guid Id,
    TransferKind Kind,
    string KindLabel,
    TransferStatus Status,
    string StatusLabel,
    Tone StatusTone,
    TransferPartyResponse Source,
    TransferPartyResponse Destination,
    decimal Amount,
    string AmountDisplay,
    TransferFailureReason? FailureReason,
    string? FailureMessage,
    TransferTimelineResponse Timeline,
    bool CanCancel);

public sealed record TransferPartyResponse(Guid AccountId, string HolderName);

public sealed record TransferTimelineResponse(
    DateTimeOffset CreatedAt,
    string CreatedAtDisplay,
    DateTimeOffset? ScheduledFor,
    string? ScheduledForDisplay,
    DateTimeOffset? ProcessedAt,
    string? ProcessedAtDisplay,
    DateTimeOffset? CompletedAt,
    string? CompletedAtDisplay,
    DateTimeOffset? FailedAt,
    string? FailedAtDisplay,
    DateTimeOffset? CancelledAt,
    string? CancelledAtDisplay);

public sealed record TransferOutcome(TransferResponse Transfer, bool IsReplay)
{
    public bool IsRejected => Transfer.Status == TransferStatus.Failed;
}
