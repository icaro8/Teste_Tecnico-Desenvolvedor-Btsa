using Microsoft.Extensions.Options;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Application.Accounts;

public sealed record OpenAccountCommand(
    string HolderName,
    string HolderDocument,
    decimal InitialBalance,
    decimal OverdraftLimit,
    HourlyLimitOptions? DaytimeLimit,
    HourlyLimitOptions? NighttimeLimit);

internal sealed class OpenAccountHandler(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IOptions<TransferRulesOptions> rules,
    AccountPresenter presenter,
    TimeProvider clock) : ICommandHandler<OpenAccountCommand, AccountResponse>
{
    public Task<Result<AccountResponse>> HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<AccountResponse>(async token =>
        {
            var limits = BuildLimits(command);
            if (limits.IsFailure)
            {
                return limits.Error!;
            }

            var opened = Account.Open(
                command.HolderName,
                command.HolderDocument,
                command.InitialBalance,
                command.OverdraftLimit,
                limits.Value,
                clock.GetUtcNow());

            if (opened.IsFailure)
            {
                return opened.Error!;
            }

            if (await accounts.ExistsWithDocumentAsync(opened.Value.HolderDocument, token))
            {
                return AccountErrors.DocumentAlreadyRegistered;
            }

            accounts.Add(opened.Value);
            return presenter.Present(opened.Value);
        }, cancellationToken);

    private Result<TransferLimitPolicy> BuildLimits(OpenAccountCommand command) =>
        TransferLimitPolicyFactory.Create(
            command.DaytimeLimit ?? rules.Value.DefaultDaytimeLimit,
            command.NighttimeLimit ?? rules.Value.DefaultNighttimeLimit);
}
