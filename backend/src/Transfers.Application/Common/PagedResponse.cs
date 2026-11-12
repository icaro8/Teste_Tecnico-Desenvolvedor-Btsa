namespace Transfers.Application.Common;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    string SortBy,
    SortDirection SortDirection)
{
    public static PagedResponse<T> From<TSource, TSort>(
        PagedList<TSource> list,
        PageRequest page,
        TSort sortBy,
        SortDirection direction,
        Func<TSource, T> map)
        where TSort : struct, Enum => new(
            list.Items.Select(map).ToList(),
            page.Page,
            page.PageSize,
            list.TotalItems,
            (int)Math.Ceiling(list.TotalItems / (double)page.PageSize),
            sortBy.ToString(),
            direction);
}
