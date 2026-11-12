using Transfers.Domain.Common;

namespace Transfers.Domain.Accounts;

public sealed class Account : AggregateRoot
{
    private Account()
    {
        HolderName = string.Empty;
        HolderDocument = string.Empty;
        Limits = null!;
    }

    private Account(string holderName, string holderDocument, decimal balance, decimal overdraftLimit, TransferLimitPolicy limits, DateTimeOffset openedAt)
    {
        Id = Guid.CreateVersion7(openedAt);
        HolderName = holderName;
        HolderDocument = holderDocument;
        Balance = balance;
        OverdraftLimit = overdraftLimit;
        Limits = limits;
        Status = AccountStatus.Active;
        OpenedAt = openedAt;
    }

    public string HolderName { get; private set; }

    public string HolderDocument { get; private set; }

    public decimal Balance { get; private set; }

    public decimal OverdraftLimit { get; private set; }

    public AccountStatus Status { get; private set; }

    public TransferLimitPolicy Limits { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public bool IsActive => Status == AccountStatus.Active;

    public decimal AvailableToTransfer => Balance + OverdraftLimit;

    public decimal OverdraftInUse => Balance < 0 ? -Balance : 0;

    public static Result<Account> Open(
        string holderName,
        string holderDocument,
        decimal initialBalance,
        decimal overdraftLimit,
        TransferLimitPolicy limits,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(holderName))
        {
            return AccountErrors.HolderNameRequired;
        }

        var document = Cpf.Normalize(holderDocument);
        if (document is null)
        {
            return AccountErrors.InvalidDocument;
        }

        if (initialBalance < 0 || !MoneyRules.HasValidPrecision(initialBalance))
        {
            return AccountErrors.InvalidInitialBalance;
        }

        if (!IsValidOverdraftLimit(overdraftLimit))
        {
            return AccountErrors.InvalidOverdraftLimit;
        }

        return new Account(holderName.Trim(), document, initialBalance, overdraftLimit, limits, now);
    }

    public Result ChangeOverdraftLimit(decimal newLimit)
    {
        if (!IsValidOverdraftLimit(newLimit))
        {
            return AccountErrors.InvalidOverdraftLimit;
        }

        if (newLimit < OverdraftInUse)
        {
            return AccountErrors.OverdraftLimitBelowUsage;
        }

        OverdraftLimit = newLimit;
        return Result.Success();
    }

    public void ChangeTransferLimits(TransferLimitPolicy limits) => Limits = limits;

    public Result Block() => ChangeStatus(AccountStatus.Blocked);

    public Result Deactivate() => ChangeStatus(AccountStatus.Inactive);

    public Result Activate() => ChangeStatus(AccountStatus.Active);

    public bool CanWithdraw(decimal amount) => amount > 0 && amount <= AvailableToTransfer;

    public bool CanChangeStatusTo(AccountStatus status) => Status != status;

    internal void Withdraw(decimal amount)
    {
        if (!IsActive || !CanWithdraw(amount))
        {
            throw new InvalidOperationException($"Account {Id} cannot withdraw {amount}.");
        }

        Balance -= amount;
    }

    internal void Deposit(decimal amount)
    {
        if (!IsActive || amount <= 0)
        {
            throw new InvalidOperationException($"Account {Id} cannot receive {amount}.");
        }

        Balance += amount;
    }

    private static bool IsValidOverdraftLimit(decimal limit) => limit >= 0 && MoneyRules.HasValidPrecision(limit);

    private Result ChangeStatus(AccountStatus status)
    {
        if (!CanChangeStatusTo(status))
        {
            return AccountErrors.AlreadyInStatus;
        }

        Status = status;
        return Result.Success();
    }
}
