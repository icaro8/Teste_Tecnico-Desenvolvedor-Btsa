using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Repositories;

internal sealed class AccountRepository(TransfersDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public async Task<IReadOnlyList<Account>> GetByIdsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken) =>
        await db.Accounts.AsNoTracking().Where(account => accountIds.Contains(account.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Account>> LockAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken)
    {
        var ids = accountIds.Distinct().ToArray();
        await db.Database.ExecuteSqlAsync($"SELECT 1 FROM accounts WHERE id = ANY({ids}) ORDER BY id FOR UPDATE", cancellationToken);
        return await db.Accounts.Where(account => ids.Contains(account.Id)).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsWithDocumentAsync(string document, CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(account => account.HolderDocument == document, cancellationToken);

    public async Task<PagedList<Account>> ListAsync(ListAccountsQuery query, CancellationToken cancellationToken)
    {
        var accounts = Filter(db.Accounts.AsNoTracking(), query.Search);
        var total = await accounts.CountAsync(cancellationToken);
        var items = await Sort(accounts, query.SortBy, query.Direction)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<Account>(items, total);
    }

    public void Add(Account account) => db.Accounts.Add(account);

    private static IQueryable<Account> Filter(IQueryable<Account> accounts, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return accounts;
        }

        var term = search.Trim();
        if (term.All(character => char.IsAsciiDigit(character) || character is '.' or '-'))
        {
            var digits = new string(term.Where(char.IsAsciiDigit).ToArray());
            return accounts.Where(account => account.HolderDocument.Contains(digits));
        }

        var pattern = $"%{term}%";
        return accounts.Where(account => EF.Functions.ILike(account.HolderName, pattern));
    }

    private static IQueryable<Account> Sort(IQueryable<Account> accounts, AccountSortField field, SortDirection direction) => field switch
    {
        AccountSortField.HolderDocument => accounts.SortBy(account => account.HolderDocument, direction),
        AccountSortField.Status => accounts.SortBy(account => account.Status, direction),
        AccountSortField.Balance => accounts.SortBy(account => account.Balance, direction),
        AccountSortField.OverdraftLimit => accounts.SortBy(account => account.OverdraftLimit, direction),
        AccountSortField.AvailableToTransfer => accounts.SortBy(account => account.Balance + account.OverdraftLimit, direction),
        AccountSortField.OpenedAt => accounts.SortBy(account => account.OpenedAt, direction),
        _ => accounts.SortBy(account => account.HolderName, direction)
    };
}

internal static class QueryableSortingExtensions
{
    public static IQueryable<T> SortBy<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> key, SortDirection direction)
    {
        var ordered = direction == SortDirection.Desc ? source.OrderByDescending(key) : source.OrderBy(key);
        return ordered.ThenBy(item => EF.Property<Guid>(item!, "Id"));
    }
}
