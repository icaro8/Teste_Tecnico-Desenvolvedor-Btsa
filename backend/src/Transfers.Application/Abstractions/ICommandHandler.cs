using Transfers.Domain.Common;

namespace Transfers.Application.Abstractions;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
