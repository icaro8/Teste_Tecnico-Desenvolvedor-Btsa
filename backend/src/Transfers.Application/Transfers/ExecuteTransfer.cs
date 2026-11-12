using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record ExecuteTransferCommand(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string? IdempotencyKey);

internal sealed class ExecuteTransferHandler(
    ITransferRepository transfers,
    TransferSettlement settlement,
    IdempotencyGuard idempotency,
    IUnitOfWork unitOfWork,
    TransferPresenter presenter,
    TimeProvider clock) : ICommandHandler<ExecuteTransferCommand, TransferOutcome>
{
    public Task<Result<TransferOutcome>> HandleAsync(ExecuteTransferCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<TransferOutcome>(async token =>
        {
            var now = clock.GetUtcNow();
            var created = Transfer.CreateImmediate(command.SourceAccountId, command.DestinationAccountId, command.Amount, command.IdempotencyKey, now);
            if (created.IsFailure)
            {
                return created.Error!;
            }

            var participants = await settlement.LockParticipantsAsync(command.SourceAccountId, command.DestinationAccountId, token);
            if (participants.IsFailure)
            {
                return participants.Error!;
            }

            var replay = await idempotency.FindReplayAsync(created.Value, token);
            if (replay.IsFailure)
            {
                return replay.Error!;
            }

            if (replay.Value is { } previous)
            {
                return new TransferOutcome(presenter.Present(participants.Value.Describe(previous)), IsReplay: true);
            }

            var transfer = created.Value;
            transfers.Add(transfer);
            await settlement.SettleAsync(transfer, participants.Value, now, token);
            return new TransferOutcome(presenter.Present(participants.Value.Describe(transfer)), IsReplay: false);
        }, cancellationToken);
}
