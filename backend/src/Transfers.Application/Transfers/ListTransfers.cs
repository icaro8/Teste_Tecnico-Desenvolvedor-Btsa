using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public enum TransferSortField
{
    CreatedAt,
    Kind,
    Source,
    Destination,
    Amount,
    ScheduledFor,
    Status
}

public sealed record ListTransfersQuery(
    Guid? AccountId,
    TransferStatus? Status,
    TransferSortField SortBy,
    SortDirection Direction,
    PageRequest Page);

internal sealed class ListTransfersHandler(ITransferRepository transfers, TransferPresenter presenter)
    : IQueryHandler<ListTransfersQuery, PagedResponse<TransferResponse>>
{
    public async Task<Result<PagedResponse<TransferResponse>>> HandleAsync(ListTransfersQuery query, CancellationToken cancellationToken)
    {
        var page = await transfers.ListAsync(query, cancellationToken);
        return PagedResponse<TransferResponse>.From(page, query.Page, query.SortBy, query.Direction, presenter.Present);
    }
}
