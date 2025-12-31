namespace BrickDex.Core.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize) {
    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
