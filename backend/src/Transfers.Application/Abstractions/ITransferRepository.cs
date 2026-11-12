using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Abstractions;

public interface ITransferRepository
{
    Task<Transfer?> LockAsync(Guid transferId, CancellationToken cancellationToken);

    Task<Transfer?> LockNextDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<Transfer?> FindByIdempotencyKeyAsync(Guid sourceAccountId, string idempotencyKey, CancellationToken cancellationToken);

    Task<HourlyTransferUsage> GetHourlyUsageAsync(Guid sourceAccountId, DateTimeOffset since, CancellationToken cancellationToken);

    Task<TransferView?> GetViewAsync(Guid transferId, CancellationToken cancellationToken);

    Task<PagedList<TransferView>> ListAsync(ListTransfersQuery query, CancellationToken cancellationToken);

    void Add(Transfer transfer);
}
