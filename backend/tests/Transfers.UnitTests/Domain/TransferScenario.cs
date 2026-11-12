using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

internal static class TransferScenario
{
    public static Account AccountWithBalance(decimal balance, decimal overdraftLimit = 0m, TransferLimitPolicy? limits = null)
    {
        if (balance >= 0)
        {
            return AccountFactory.Open(balance, overdraftLimit, limits);
        }

        var account = AccountFactory.Open(0m, overdraftLimit, limits);
        var sink = AccountFactory.Open();
        Settle(account, sink, -balance);
        return account;
    }

    public static Transfer Settle(
        Account source,
        Account destination,
        decimal amount,
        HourlyTransferUsage? usage = null,
        TransferPeriod period = TransferPeriod.Daytime)
    {
        var transfer = Transfer.CreateImmediate(source.Id, destination.Id, amount, null, AccountFactory.Now).Value;
        FundsTransferService.Settle(transfer, source, destination, usage ?? HourlyTransferUsage.None, period, AccountFactory.Now);
        return transfer;
    }
}
