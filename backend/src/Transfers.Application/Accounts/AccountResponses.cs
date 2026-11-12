using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Accounts;

public sealed record AccountResponse(
    Guid Id,
    string HolderName,
    string HolderDocument,
    AccountStatus Status,
    string StatusLabel,
    Tone StatusTone,
    decimal Balance,
    string BalanceDisplay,
    decimal OverdraftLimit,
    string OverdraftLimitDisplay,
    decimal OverdraftInUse,
    string OverdraftInUseDisplay,
    decimal AvailableToTransfer,
    string AvailableToTransferDisplay,
    DateTimeOffset OpenedAt,
    string OpenedAtDisplay,
    AccountActionsResponse Actions);

public sealed record AccountActionsResponse(bool CanBlock, bool CanActivate, bool CanDeactivate, bool CanSendTransfers);

public sealed record AccountDetailsResponse(AccountResponse Account, TransferLimitsResponse Limits);

public sealed record TransferLimitsResponse(
    TransferPeriod CurrentPeriod,
    string CurrentPeriodLabel,
    HourlyLimitResponse Daytime,
    HourlyLimitResponse Nighttime,
    HourlyUsageResponse Usage);

public sealed record HourlyLimitResponse(decimal MaxAmount, string MaxAmountDisplay, int MaxAttempts);

public sealed record HourlyUsageResponse(
    int AttemptsInLastHour,
    int RemainingAttempts,
    decimal TransferredInLastHour,
    string TransferredInLastHourDisplay,
    decimal RemainingAmount,
    string RemainingAmountDisplay);
