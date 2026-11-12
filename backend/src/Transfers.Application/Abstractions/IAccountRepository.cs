using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;

namespace Transfers.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> GetByIdsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> LockAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken);

    Task<bool> ExistsWithDocumentAsync(string document, CancellationToken cancellationToken);

    Task<PagedList<Account>> ListAsync(ListAccountsQuery query, CancellationToken cancellationToken);

    void Add(Account account);
}
