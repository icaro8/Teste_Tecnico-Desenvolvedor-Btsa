using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Accounts;

public sealed record GetAccountQuery(Guid AccountId);

internal sealed class GetAccountHandler(
    IAccountRepository accounts,
    ITransferRepository transfers,
    TransferPeriodSchedule schedule,
    AccountPresenter presenter,
    TimeProvider clock) : IQueryHandler<GetAccountQuery, AccountDetailsResponse>
{
    public async Task<Result<AccountDetailsResponse>> HandleAsync(GetAccountQuery query, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(query.AccountId, cancellationToken);
        if (account is null)
        {
            return AccountErrors.NotFound(query.AccountId);
        }

        var now = clock.GetUtcNow();
        var usage = await transfers.GetHourlyUsageAsync(account.Id, now - HourlyTransferUsage.Window, cancellationToken);
        return presenter.PresentDetails(account, usage, schedule.Resolve(now));
    }
}
