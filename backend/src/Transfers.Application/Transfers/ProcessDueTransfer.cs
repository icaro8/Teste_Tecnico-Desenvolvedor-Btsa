using Transfers.Application.Abstractions;
using Transfers.Domain.Common;

namespace Transfers.Application.Transfers;

public sealed record ProcessDueTransferCommand;

internal sealed class ProcessDueTransferHandler(
    ITransferRepository transfers,
    TransferSettlement settlement,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ProcessDueTransferCommand, bool>
{
    public Task<Result<bool>> HandleAsync(ProcessDueTransferCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<bool>(async token =>
        {
            var now = clock.GetUtcNow();
            var transfer = await transfers.LockNextDueAsync(now, token);
            if (transfer is null)
            {
                return false;
            }

            transfer.StartProcessing(now);
            var participants = await settlement.LockParticipantsAsync(transfer.SourceAccountId, transfer.DestinationAccountId, token);
            if (participants.IsFailure)
            {
                return participants.Error!;
            }

            await settlement.SettleAsync(transfer, participants.Value, now, token);
            return true;
        }, cancellationToken);
}
