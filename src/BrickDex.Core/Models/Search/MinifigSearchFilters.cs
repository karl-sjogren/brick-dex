namespace BrickDex.Core.Models.Search;

/// <summary>
/// Search filters for minifigs.
/// </summary>
public record MinifigSearchFilters {
    public string? Query { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    // Range filters
    public int? MinParts { get; init; }
    public int? MaxParts { get; init; }

    // Sorting
    public string? SortBy { get; init; } = "name";
    public bool SortDescending { get; init; }
}
