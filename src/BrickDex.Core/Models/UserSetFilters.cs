namespace BrickDex.Core.Models;

public record UserSetFilters {
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Query { get; init; }
    public string? SortBy { get; init; } = "name";
    public bool SortDescending { get; init; }
}
