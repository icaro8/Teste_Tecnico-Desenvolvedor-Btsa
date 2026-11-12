namespace Transfers.Application.Common;

public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalItems);
