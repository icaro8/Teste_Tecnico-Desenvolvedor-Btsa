using Transfers.Domain.Common;

namespace Transfers.Application.Abstractions;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
