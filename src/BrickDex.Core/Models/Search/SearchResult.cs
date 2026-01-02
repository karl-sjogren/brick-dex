namespace BrickDex.Core.Models.Search;

/// <summary>
/// Generic search result with pagination and optional facets.
/// </summary>
public record SearchResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    FacetResults? Facets = null
) {
    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Search hit for a set in the global catalog.
/// </summary>
public record SetSearchHit(
    string SetNum,
    string Name,
    int Year,
    int ThemeId,
    string? ThemeName,
    int NumParts,
    string? ImageUrl,
    float Score
);

/// <summary>
/// Search hit for a user's set in their collection or wishlist.
/// </summary>
public record UserSetSearchHit(
    Guid UserSetId,
    string SetNum,
    string Name,
    int Year,
    int ThemeId,
    string? ThemeName,
    int NumParts,
    string? ImageUrl,
    int Quantity,
    SetStatus Status,
    bool IsWishlist,
    string? Notes,
    float Score
);

/// <summary>
/// Search hit for a minifig.
/// </summary>
public record MinifigSearchHit(
    string FigNum,
    string Name,
    int NumParts,
    string? ImageUrl,
    float Score
);

/// <summary>
/// Facet results for filtering UI.
/// </summary>
public record FacetResults(
    IReadOnlyList<FacetValue> Themes,
    IReadOnlyList<FacetValue> Years,
    IReadOnlyList<FacetValue> PartsRanges,
    IReadOnlyList<FacetValue>? Statuses = null
);

/// <summary>
/// A single facet value with its count.
/// </summary>
public record FacetValue(string Value, string Label, int Count);
