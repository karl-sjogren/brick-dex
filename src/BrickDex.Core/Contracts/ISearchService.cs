using BrickDex.Core.Models.Search;
using BrickDex.Core.Services.Rebrickable;

namespace BrickDex.Core.Contracts;

/// <summary>
/// Provides search functionality using the Lucene index.
/// </summary>
public interface ISearchService {
    /// <summary>
    /// Search for sets in the global catalog.
    /// </summary>
    Task<SearchResult<SetSearchHit>> SearchSetsAsync(
        SetSearchFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search within a user's collection or wishlist.
    /// </summary>
    Task<SearchResult<UserSetSearchHit>> SearchUserSetsAsync(
        Guid userId,
        UserSetSearchFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for minifigs in the catalog.
    /// </summary>
    Task<SearchResult<MinifigSearchHit>> SearchMinifigsAsync(
        MinifigSearchFilters filters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get facet counts for filtering UI.
    /// </summary>
    Task<FacetResults> GetSetFacetsAsync(
        SetSearchFilters? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get autocomplete suggestions.
    /// </summary>
    Task<IReadOnlyList<string>> SuggestAsync(
        string prefix,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}
