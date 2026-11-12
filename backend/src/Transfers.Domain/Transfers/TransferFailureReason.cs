namespace Transfers.Domain.Transfers;

public enum TransferFailureReason
{
    SourceAccountNotActive,
    DestinationAccountNotActive,
    InsufficientFunds,
    HourlyAmountLimitExceeded,
    HourlyAttemptLimitExceeded
}
