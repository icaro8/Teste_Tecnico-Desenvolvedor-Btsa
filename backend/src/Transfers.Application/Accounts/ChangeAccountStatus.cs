using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Application.Accounts;

public sealed record ChangeAccountStatusCommand(Guid AccountId, AccountStatus TargetStatus);

internal sealed class ChangeAccountStatusHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    AccountPresenter presenter) : ICommandHandler<ChangeAccountStatusCommand, AccountResponse>
{
    public Task<Result<AccountResponse>> HandleAsync(ChangeAccountStatusCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<AccountResponse>(async token =>
        {
            var locked = await accounts.LockAsync([command.AccountId], token);
            if (locked.Count == 0)
            {
                return AccountErrors.NotFound(command.AccountId);
            }

            var account = locked[0];
            var changed = command.TargetStatus switch
            {
                AccountStatus.Active => account.Activate(),
                AccountStatus.Blocked => account.Block(),
                AccountStatus.Inactive => account.Deactivate(),
                _ => throw new ArgumentOutOfRangeException(nameof(command), command.TargetStatus, null)
            };

            return changed.IsSuccess
                ? presenter.Present(account)
                : changed.Error!;
        }, cancellationToken);
}
