using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

internal sealed record TransferParticipants(Account Source, Account Destination)
{
    public TransferView Describe(Transfer transfer) => TransferView.From(transfer, Source, Destination);
}

internal sealed class TransferSettlement(
    IAccountRepository accounts,
    ITransferRepository transfers,
    TransferPeriodSchedule schedule)
{
    public async Task<Result<TransferParticipants>> LockParticipantsAsync(Guid sourceAccountId, Guid destinationAccountId, CancellationToken cancellationToken)
    {
        var locked = await accounts.LockAsync([sourceAccountId, destinationAccountId], cancellationToken);
        var source = locked.FirstOrDefault(account => account.Id == sourceAccountId);
        var destination = locked.FirstOrDefault(account => account.Id == destinationAccountId);

        if (source is null)
        {
            return AccountErrors.NotFound(sourceAccountId);
        }

        if (destination is null)
        {
            return AccountErrors.NotFound(destinationAccountId);
        }

        return new TransferParticipants(source, destination);
    }

    public async Task SettleAsync(Transfer transfer, TransferParticipants participants, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var usage = await transfers.GetHourlyUsageAsync(transfer.SourceAccountId, now - HourlyTransferUsage.Window, cancellationToken);
        FundsTransferService.Settle(transfer, participants.Source, participants.Destination, usage, schedule.Resolve(now), now);
    }
}
