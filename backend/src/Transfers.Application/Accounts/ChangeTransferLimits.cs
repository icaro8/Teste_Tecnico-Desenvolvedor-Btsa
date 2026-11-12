using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Accounts;

public sealed record ChangeTransferLimitsCommand(Guid AccountId, HourlyLimitOptions DaytimeLimit, HourlyLimitOptions NighttimeLimit);

internal sealed class ChangeTransferLimitsHandler(
    IAccountRepository accounts,
    ITransferRepository transfers,
    IUnitOfWork unitOfWork,
    TransferPeriodSchedule schedule,
    AccountPresenter presenter,
    TimeProvider clock) : ICommandHandler<ChangeTransferLimitsCommand, AccountDetailsResponse>
{
    public Task<Result<AccountDetailsResponse>> HandleAsync(ChangeTransferLimitsCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<AccountDetailsResponse>(async token =>
        {
            var limits = TransferLimitPolicyFactory.Create(command.DaytimeLimit, command.NighttimeLimit);
            if (limits.IsFailure)
            {
                return limits.Error!;
            }

            var locked = await accounts.LockAsync([command.AccountId], token);
            if (locked.Count == 0)
            {
                return AccountErrors.NotFound(command.AccountId);
            }

            var account = locked[0];
            account.ChangeTransferLimits(limits.Value);

            var now = clock.GetUtcNow();
            var usage = await transfers.GetHourlyUsageAsync(account.Id, now - HourlyTransferUsage.Window, token);
            return presenter.PresentDetails(account, usage, schedule.Resolve(now));
        }, cancellationToken);
}
