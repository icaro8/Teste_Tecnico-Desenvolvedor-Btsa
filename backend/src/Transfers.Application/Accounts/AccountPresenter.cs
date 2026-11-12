using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Accounts;

public sealed class AccountPresenter(DisplayFormatter format)
{
    public AccountResponse Present(Account account) => new(
        account.Id,
        account.HolderName,
        DisplayFormatter.Document(account.HolderDocument),
        account.Status,
        DisplayFormatter.Label(account.Status),
        DisplayFormatter.ToneOf(account.Status),
        account.Balance,
        DisplayFormatter.Money(account.Balance),
        account.OverdraftLimit,
        DisplayFormatter.Money(account.OverdraftLimit),
        account.OverdraftInUse,
        DisplayFormatter.Money(account.OverdraftInUse),
        account.AvailableToTransfer,
        DisplayFormatter.Money(account.AvailableToTransfer),
        account.OpenedAt,
        format.DateTime(account.OpenedAt)!,
        new AccountActionsResponse(
            account.CanChangeStatusTo(AccountStatus.Blocked),
            account.CanChangeStatusTo(AccountStatus.Active),
            account.CanChangeStatusTo(AccountStatus.Inactive),
            account.IsActive));

    public AccountDetailsResponse PresentDetails(Account account, HourlyTransferUsage usage, TransferPeriod period)
    {
        var currentLimit = account.Limits.For(period);
        var remainingAmount = Math.Max(currentLimit.MaxAmount - usage.TransferredAmount, 0);

        return new AccountDetailsResponse(
            Present(account),
            new TransferLimitsResponse(
                period,
                DisplayFormatter.Label(period),
                PresentLimit(account.Limits.Daytime),
                PresentLimit(account.Limits.Nighttime),
                new HourlyUsageResponse(
                    usage.Attempts,
                    Math.Max(currentLimit.MaxAttempts - usage.Attempts, 0),
                    usage.TransferredAmount,
                    DisplayFormatter.Money(usage.TransferredAmount),
                    remainingAmount,
                    DisplayFormatter.Money(remainingAmount))));
    }

    private static HourlyLimitResponse PresentLimit(HourlyLimit limit) =>
        new(limit.MaxAmount, DisplayFormatter.Money(limit.MaxAmount), limit.MaxAttempts);
}
