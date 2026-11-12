using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Repositories;

internal sealed class TransferRepository(TransfersDbContext db) : ITransferRepository
{
    private const string ScheduledStatus = nameof(TransferStatus.Scheduled);

    public async Task<Transfer?> LockAsync(Guid transferId, CancellationToken cancellationToken)
    {
        var locked = await db.Database
            .SqlQuery<Guid>($"SELECT id AS \"Value\" FROM transfers WHERE id = {transferId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return await LoadAsync(locked, cancellationToken);
    }

    public async Task<Transfer?> LockNextDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var locked = await db.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value" FROM transfers
                WHERE status = {ScheduledStatus} AND scheduled_for <= {now}
                ORDER BY scheduled_for, id
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return await LoadAsync(locked, cancellationToken);
    }

    public Task<Transfer?> FindByIdempotencyKeyAsync(Guid sourceAccountId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.Transfers.FirstOrDefaultAsync(
            transfer => transfer.SourceAccountId == sourceAccountId && transfer.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public async Task<HourlyTransferUsage> GetHourlyUsageAsync(Guid sourceAccountId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var usage = await db.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.SourceAccountId == sourceAccountId && transfer.ProcessedAt > since)
            .GroupBy(_ => 1)
            .Select(attempts => new
            {
                Attempts = attempts.Count(),
                Transferred = attempts.Where(transfer => transfer.Status == TransferStatus.Completed).Sum(transfer => (decimal?)transfer.Amount) ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        return usage is null ? HourlyTransferUsage.None : new HourlyTransferUsage(usage.Attempts, usage.Transferred);
    }

    public Task<TransferView?> GetViewAsync(Guid transferId, CancellationToken cancellationToken) =>
        Rows().Where(row => row.Transfer.Id == transferId).Select(ToView).FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedList<TransferView>> ListAsync(ListTransfersQuery query, CancellationToken cancellationToken)
    {
        var rows = Rows();

        if (query.AccountId is { } accountId)
        {
            rows = rows.Where(row => row.Transfer.SourceAccountId == accountId || row.Transfer.DestinationAccountId == accountId);
        }

        if (query.Status is { } status)
        {
            rows = rows.Where(row => row.Transfer.Status == status);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await Sort(rows, query.SortBy, query.Direction)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .Select(ToView)
            .ToListAsync(cancellationToken);

        return new PagedList<TransferView>(items, total);
    }

    public void Add(Transfer transfer) => db.Transfers.Add(transfer);

    private async Task<Transfer?> LoadAsync(List<Guid> lockedIds, CancellationToken cancellationToken)
    {
        if (lockedIds is not [var transferId])
        {
            return null;
        }

        return await db.Transfers.SingleAsync(transfer => transfer.Id == transferId, cancellationToken);
    }

    private static readonly Expression<Func<TransferRow, TransferView>> ToView = row => new TransferView(
        row.Transfer.Id,
        row.Transfer.Kind,
        row.Transfer.Status,
        row.Transfer.SourceAccountId,
        row.SourceHolderName,
        row.Transfer.DestinationAccountId,
        row.DestinationHolderName,
        row.Transfer.Amount,
        row.Transfer.FailureReason,
        row.Transfer.CreatedAt,
        row.Transfer.ScheduledFor,
        row.Transfer.ProcessedAt,
        row.Transfer.CompletedAt,
        row.Transfer.FailedAt,
        row.Transfer.CancelledAt);

    private IQueryable<TransferRow> Rows() =>
        from transfer in db.Transfers.AsNoTracking()
        join source in db.Accounts on transfer.SourceAccountId equals source.Id
        join destination in db.Accounts on transfer.DestinationAccountId equals destination.Id
        select new TransferRow { Transfer = transfer, SourceHolderName = source.HolderName, DestinationHolderName = destination.HolderName };

    private static IQueryable<TransferRow> Sort(IQueryable<TransferRow> rows, TransferSortField field, SortDirection direction)
    {
        var ordered = field switch
        {
            TransferSortField.Kind => Order(rows, row => row.Transfer.Kind, direction),
            TransferSortField.Source => Order(rows, row => row.SourceHolderName, direction),
            TransferSortField.Destination => Order(rows, row => row.DestinationHolderName, direction),
            TransferSortField.Amount => Order(rows, row => row.Transfer.Amount, direction),
            TransferSortField.ScheduledFor => Order(rows, row => row.Transfer.ScheduledFor, direction),
            TransferSortField.Status => Order(rows, row => row.Transfer.Status, direction),
            _ => Order(rows, row => row.Transfer.CreatedAt, direction)
        };

        return ordered.ThenBy(row => row.Transfer.Id);
    }

    private static IOrderedQueryable<TransferRow> Order<TKey>(
        IQueryable<TransferRow> rows,
        Expression<Func<TransferRow, TKey>> key,
        SortDirection direction) =>
        direction == SortDirection.Desc ? rows.OrderByDescending(key) : rows.OrderBy(key);

    private sealed class TransferRow
    {
        public required Transfer Transfer { get; init; }

        public required string SourceHolderName { get; init; }

        public required string DestinationHolderName { get; init; }
    }
}
