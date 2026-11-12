using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record ScheduleTransferCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    DateTimeOffset ScheduledFor,
    string? IdempotencyKey);

internal sealed class ScheduleTransferHandler(
    ITransferRepository transfers,
    TransferSettlement settlement,
    IdempotencyGuard idempotency,
    IUnitOfWork unitOfWork,
    TransferPresenter presenter,
    TimeProvider clock) : ICommandHandler<ScheduleTransferCommand, TransferOutcome>
{
    public Task<Result<TransferOutcome>> HandleAsync(ScheduleTransferCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<TransferOutcome>(async token =>
        {
            var scheduled = Transfer.Schedule(
                command.SourceAccountId,
                command.DestinationAccountId,
                command.Amount,
                command.ScheduledFor,
                command.IdempotencyKey,
                clock.GetUtcNow());

            if (scheduled.IsFailure)
            {
                return scheduled.Error!;
            }

            var participants = await settlement.LockParticipantsAsync(command.SourceAccountId, command.DestinationAccountId, token);
            if (participants.IsFailure)
            {
                return participants.Error!;
            }

            var replay = await idempotency.FindReplayAsync(scheduled.Value, token);
            if (replay.IsFailure)
            {
                return replay.Error!;
            }

            if (replay.Value is { } previous)
            {
                return new TransferOutcome(presenter.Present(participants.Value.Describe(previous)), IsReplay: true);
            }

            if (FundsTransferService.CheckCanSchedule(participants.Value.Source, participants.Value.Destination) is { } ineligible)
            {
                return ineligible;
            }

            transfers.Add(scheduled.Value);
            return new TransferOutcome(presenter.Present(participants.Value.Describe(scheduled.Value)), IsReplay: false);
        }, cancellationToken);
}
