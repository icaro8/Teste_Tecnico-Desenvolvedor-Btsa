using Transfers.Application.Abstractions;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record CancelTransferCommand(Guid TransferId);

internal sealed class CancelTransferHandler(
    ITransferRepository transfers,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    TransferPresenter presenter,
    TimeProvider clock) : ICommandHandler<CancelTransferCommand, TransferResponse>
{
    public Task<Result<TransferResponse>> HandleAsync(CancelTransferCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<TransferResponse>(async token =>
        {
            var transfer = await transfers.LockAsync(command.TransferId, token);
            if (transfer is null)
            {
                return TransferErrors.NotFound(command.TransferId);
            }

            var cancelled = transfer.Cancel(clock.GetUtcNow());
            if (cancelled.IsFailure)
            {
                return cancelled.Error!;
            }

            var parties = await accounts.GetByIdsAsync([transfer.SourceAccountId, transfer.DestinationAccountId], token);
            var source = parties.Single(account => account.Id == transfer.SourceAccountId);
            var destination = parties.Single(account => account.Id == transfer.DestinationAccountId);
            return presenter.Present(TransferView.From(transfer, source, destination));
        }, cancellationToken);
}
