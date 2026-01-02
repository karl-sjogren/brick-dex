namespace BrickDex.Core.Services.Rebrickable;

public record SetSearchFilters {
    public string? Query { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? MinYear { get; init; }
    public int? MaxYear { get; init; }
    public int? MinParts { get; init; }
    public int? MaxParts { get; init; }
    public int? ThemeId { get; init; }
    /// <summary>
    /// Theme IDs to filter by (includes ThemeId and all its descendants).
    /// If not set, ThemeId will be used as an exact match.
    /// </summary>
    public IReadOnlyList<int>? ThemeIds { get; init; }
    public string? Ordering { get; init; }

    public bool HasActiveFilters =>
        MinYear.HasValue || MaxYear.HasValue ||
        MinParts.HasValue || MaxParts.HasValue ||
        ThemeId.HasValue || !string.IsNullOrWhiteSpace(Ordering);
}
