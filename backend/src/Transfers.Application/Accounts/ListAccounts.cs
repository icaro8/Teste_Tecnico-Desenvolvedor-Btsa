using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Common;

namespace Transfers.Application.Accounts;

public enum AccountSortField
{
    HolderName,
    HolderDocument,
    Status,
    Balance,
    OverdraftLimit,
    AvailableToTransfer,
    OpenedAt
}

public sealed record ListAccountsQuery(string? Search, AccountSortField SortBy, SortDirection Direction, PageRequest Page);

internal sealed class ListAccountsHandler(IAccountRepository accounts, AccountPresenter presenter)
    : IQueryHandler<ListAccountsQuery, PagedResponse<AccountResponse>>
{
    public async Task<Result<PagedResponse<AccountResponse>>> HandleAsync(ListAccountsQuery query, CancellationToken cancellationToken)
    {
        var page = await accounts.ListAsync(query, cancellationToken);
        return PagedResponse<AccountResponse>.From(page, query.Page, query.SortBy, query.Direction, presenter.Present);
    }
}
