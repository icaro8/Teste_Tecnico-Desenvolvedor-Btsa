using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Domain.Transfers;

public static class FundsTransferService
{
    public static void Settle(
        Transfer transfer,
        Account source,
        Account destination,
        HourlyTransferUsage usage,
        TransferPeriod period,
        DateTimeOffset now)
    {
        EnsureParticipants(transfer, source, destination);

        var rejection = FindRejection(transfer.Amount, source, destination, usage, period);
        if (rejection is { } reason)
        {
            transfer.Fail(reason, now);
            return;
        }

        source.Withdraw(transfer.Amount);
        destination.Deposit(transfer.Amount);
        transfer.Complete(now);
    }

    /// <summary>
    /// Regra de status verificada ao agendar: uma conta bloqueada ou inativa não pode
    /// agendar nem ser destino de um agendamento. Na execução, tudo é verificado de novo.
    /// </summary>
    public static Error? CheckCanSchedule(Account source, Account destination)
    {
        if (!source.IsActive)
        {
            return TransferErrors.SourceAccountNotActive;
        }

        return destination.IsActive ? null : TransferErrors.DestinationAccountNotActive;
    }

    private static TransferFailureReason? FindRejection(
        decimal amount,
        Account source,
        Account destination,
        HourlyTransferUsage usage,
        TransferPeriod period)
    {
        var limit = source.Limits.For(period);

        if (usage.Attempts >= limit.MaxAttempts)
        {
            return TransferFailureReason.HourlyAttemptLimitExceeded;
        }

        if (!source.IsActive)
        {
            return TransferFailureReason.SourceAccountNotActive;
        }

        if (!destination.IsActive)
        {
            return TransferFailureReason.DestinationAccountNotActive;
        }

        if (usage.TransferredAmount + amount > limit.MaxAmount)
        {
            return TransferFailureReason.HourlyAmountLimitExceeded;
        }

        return source.CanWithdraw(amount) ? null : TransferFailureReason.InsufficientFunds;
    }

    private static void EnsureParticipants(Transfer transfer, Account source, Account destination)
    {
        if (transfer.SourceAccountId != source.Id || transfer.DestinationAccountId != destination.Id)
        {
            throw new InvalidOperationException($"Accounts do not match transfer {transfer.Id}.");
        }
    }
}
