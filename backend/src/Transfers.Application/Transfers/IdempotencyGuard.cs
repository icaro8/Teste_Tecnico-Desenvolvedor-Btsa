using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

internal sealed class IdempotencyGuard(ITransferRepository transfers)
{
    public async Task<Result<Transfer?>> FindReplayAsync(Transfer requested, CancellationToken cancellationToken)
    {
        if (requested.IdempotencyKey is null)
        {
            return Result.Success<Transfer?>(null);
        }

        var previous = await transfers.FindByIdempotencyKeyAsync(requested.SourceAccountId, requested.IdempotencyKey, cancellationToken);
        if (previous is null)
        {
            return Result.Success<Transfer?>(null);
        }

        return previous.HasSameTerms(requested.SourceAccountId, requested.DestinationAccountId, requested.Amount, requested.ScheduledFor)
            ? previous
            : TransferErrors.IdempotencyKeyReused;
    }
}
