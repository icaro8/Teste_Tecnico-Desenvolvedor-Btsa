using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record GetTransferQuery(Guid TransferId);

internal sealed class GetTransferHandler(ITransferRepository transfers, TransferPresenter presenter)
    : IQueryHandler<GetTransferQuery, TransferResponse>
{
    public async Task<Result<TransferResponse>> HandleAsync(GetTransferQuery query, CancellationToken cancellationToken)
    {
        var view = await transfers.GetViewAsync(query.TransferId, cancellationToken);
        return view is null
            ? TransferErrors.NotFound(query.TransferId)
            : presenter.Present(view);
    }
}
