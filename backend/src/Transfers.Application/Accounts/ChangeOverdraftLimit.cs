using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Application.Accounts;

public sealed record ChangeOverdraftLimitCommand(Guid AccountId, decimal OverdraftLimit);

internal sealed class ChangeOverdraftLimitHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    AccountPresenter presenter) : ICommandHandler<ChangeOverdraftLimitCommand, AccountResponse>
{
    public Task<Result<AccountResponse>> HandleAsync(ChangeOverdraftLimitCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<AccountResponse>(async token =>
        {
            var locked = await accounts.LockAsync([command.AccountId], token);
            if (locked.Count == 0)
            {
                return AccountErrors.NotFound(command.AccountId);
            }

            var account = locked[0];
            var changed = account.ChangeOverdraftLimit(command.OverdraftLimit);
            return changed.IsSuccess
                ? presenter.Present(account)
                : changed.Error!;
        }, cancellationToken);
}
