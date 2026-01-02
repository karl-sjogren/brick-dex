namespace BrickDex.Core.Models.Search;

/// <summary>
/// Search filters for user collections and wishlists with enhanced filtering capabilities.
/// </summary>
public record UserSetSearchFilters {
    public string? Query { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    // Range filters
    public int? MinYear { get; init; }
    public int? MaxYear { get; init; }
    public int? MinParts { get; init; }
    public int? MaxParts { get; init; }

    // Category filters
    public int? ThemeId { get; init; }
    public SetStatus? Status { get; init; }
    public bool? IsWishlist { get; init; }

    // Sorting
    public string? SortBy { get; init; } = "name";
    public bool SortDescending { get; init; }

    // Facets
    public bool IncludeFacets { get; init; }
}
