using Transfers.Domain.Common;

namespace Transfers.Application.Abstractions;

public interface IUnitOfWork
{
    Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken cancellationToken);
}
