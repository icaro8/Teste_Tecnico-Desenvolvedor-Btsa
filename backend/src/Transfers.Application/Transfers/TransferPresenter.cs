using Transfers.Application.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed class TransferPresenter(DisplayFormatter format)
{
    public TransferResponse Present(TransferView transfer) => new(
        transfer.Id,
        transfer.Kind,
        DisplayFormatter.Label(transfer.Kind),
        transfer.Status,
        DisplayFormatter.Label(transfer.Status),
        DisplayFormatter.ToneOf(transfer.Status),
        new TransferPartyResponse(transfer.SourceAccountId, transfer.SourceHolderName),
        new TransferPartyResponse(transfer.DestinationAccountId, transfer.DestinationHolderName),
        transfer.Amount,
        DisplayFormatter.Money(transfer.Amount),
        transfer.FailureReason,
        transfer.FailureReason is { } reason ? DisplayFormatter.Describe(reason) : null,
        new TransferTimelineResponse(
            transfer.CreatedAt,
            format.DateTime(transfer.CreatedAt)!,
            transfer.ScheduledFor,
            format.DateTime(transfer.ScheduledFor),
            transfer.ProcessedAt,
            format.DateTime(transfer.ProcessedAt),
            transfer.CompletedAt,
            format.DateTime(transfer.CompletedAt),
            transfer.FailedAt,
            format.DateTime(transfer.FailedAt),
            transfer.CancelledAt,
            format.DateTime(transfer.CancelledAt)),
        transfer.Status == TransferStatus.Scheduled);
}
