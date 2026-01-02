using BrickDex.Core.Contracts;
using BrickDex.Core.Models.Search;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Lucene.Documents;
using BrickDex.Lucene.Index;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;

namespace BrickDex.Lucene.Search;

/// <summary>
/// Lucene-based search service implementation.
/// </summary>
public class LuceneSearchService : ISearchService {
    private readonly LuceneSearchIndex _index;
    private readonly ILogger<LuceneSearchService> _logger;

    public LuceneSearchService(
        LuceneSearchIndex index,
        ILogger<LuceneSearchService> logger) {
        _index = index;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<SearchResult<SetSearchHit>> SearchSetsAsync(
        SetSearchFilters filters,
        CancellationToken cancellationToken = default) {
        var query = QueryBuilder.BuildSetQuery(filters);
        var sort = QueryBuilder.CreateSort(ParseOrderingField(filters.Ordering), ParseOrderingDescending(filters.Ordering));

        var (hits, totalCount) = ExecuteSearch(query, sort, filters.Page, filters.PageSize);

        var items = hits.Select(h => DocumentMapper.ToSetSearchHit(h.Doc, h.Score)).ToList();

        var result = new SearchResult<SetSearchHit>(
            Items: items,
            TotalCount: totalCount,
            Page: filters.Page,
            PageSize: filters.PageSize
        );

        _logger.LogDebug(
            "Set search for '{Query}' returned {Count} of {Total} results",
            filters.Query, items.Count, totalCount);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SearchResult<UserSetSearchHit>> SearchUserSetsAsync(
        Guid userId,
        UserSetSearchFilters filters,
        CancellationToken cancellationToken = default) {
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);
        var sort = QueryBuilder.CreateSort(filters.SortBy, filters.SortDescending);

        var (hits, totalCount) = ExecuteSearch(query, sort, filters.Page, filters.PageSize);

        var items = hits.Select(h => DocumentMapper.ToUserSetSearchHit(h.Doc, h.Score)).ToList();

        var result = new SearchResult<UserSetSearchHit>(
            Items: items,
            TotalCount: totalCount,
            Page: filters.Page,
            PageSize: filters.PageSize
        );

        _logger.LogDebug(
            "User set search for user {UserId} returned {Count} of {Total} results",
            userId, items.Count, totalCount);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<SearchResult<MinifigSearchHit>> SearchMinifigsAsync(
        MinifigSearchFilters filters,
        CancellationToken cancellationToken = default) {
        var query = QueryBuilder.BuildMinifigQuery(filters);
        var sort = QueryBuilder.CreateSort(filters.SortBy, filters.SortDescending);

        var (hits, totalCount) = ExecuteSearch(query, sort, filters.Page, filters.PageSize);

        var items = hits.Select(h => DocumentMapper.ToMinifigSearchHit(h.Doc, h.Score)).ToList();

        var result = new SearchResult<MinifigSearchHit>(
            Items: items,
            TotalCount: totalCount,
            Page: filters.Page,
            PageSize: filters.PageSize
        );

        _logger.LogDebug(
            "Minifig search for '{Query}' returned {Count} of {Total} results",
            filters.Query, items.Count, totalCount);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<FacetResults> GetSetFacetsAsync(
        SetSearchFilters? filters = null,
        CancellationToken cancellationToken = default) {
        // Build a base query for faceting
        var query = filters != null
            ? QueryBuilder.BuildSetQuery(filters)
            : new TermQuery(new Term(DocumentMapper.FieldDocType, DocumentMapper.DocTypeSet));

        var directory = _index.GetOrCreateDirectory();

        using var reader = DirectoryReader.Open(directory);
        var searcher = new IndexSearcher(reader);

        // Get all matching documents for faceting
        var topDocs = searcher.Search(query, reader.MaxDoc);

        // Count facets
        var themeCounts = new Dictionary<string, int>();
        var yearCounts = new Dictionary<string, int>();
        var partsCounts = new Dictionary<string, int>();

        foreach(var scoreDoc in topDocs.ScoreDocs) {
            var doc = searcher.Doc(scoreDoc.Doc);

            // Theme facet
            var themeName = doc.Get(DocumentMapper.FieldThemeName);
            if(!string.IsNullOrEmpty(themeName)) {
                themeCounts[themeName] = themeCounts.GetValueOrDefault(themeName, 0) + 1;
            }

            // Year facet
            var year = doc.GetField(DocumentMapper.FieldYear)?.GetInt32Value();
            if(year.HasValue) {
                var yearStr = year.Value.ToString();
                yearCounts[yearStr] = yearCounts.GetValueOrDefault(yearStr, 0) + 1;
            }

            // Parts range facet
            var numParts = doc.GetField(DocumentMapper.FieldNumParts)?.GetInt32Value();
            if(numParts.HasValue) {
                var partsRange = GetPartsRange(numParts.Value);
                partsCounts[partsRange] = partsCounts.GetValueOrDefault(partsRange, 0) + 1;
            }
        }

        var facets = new FacetResults(
            Themes: themeCounts
                .OrderByDescending(kv => kv.Value)
                .Take(20)
                .Select(kv => new FacetValue(kv.Key, kv.Key, kv.Value))
                .ToList(),
            Years: yearCounts
                .OrderByDescending(kv => int.Parse(kv.Key))
                .Select(kv => new FacetValue(kv.Key, kv.Key, kv.Value))
                .ToList(),
            PartsRanges: partsCounts
                .OrderBy(kv => GetPartsRangeOrder(kv.Key))
                .Select(kv => new FacetValue(kv.Key, kv.Key, kv.Value))
                .ToList()
        );

        return Task.FromResult(facets);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> SuggestAsync(
        string prefix,
        int maxResults = 10,
        CancellationToken cancellationToken = default) {
        if(string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2) {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var directory = _index.GetOrCreateDirectory();

        using var reader = DirectoryReader.Open(directory);
        var searcher = new IndexSearcher(reader);

        // Search for names starting with the prefix
        var query = new PrefixQuery(
            new Term(DocumentMapper.FieldName, prefix.ToLowerInvariant()));

        var topDocs = searcher.Search(query, maxResults);

        var suggestions = new List<string>();
        foreach(var scoreDoc in topDocs.ScoreDocs) {
            var doc = searcher.Doc(scoreDoc.Doc);
            var name = doc.Get(DocumentMapper.FieldName);
            if(!string.IsNullOrEmpty(name) && !suggestions.Contains(name)) {
                suggestions.Add(name);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(suggestions);
    }

    private (List<(global::Lucene.Net.Documents.Document Doc, float Score)> Hits, int TotalCount) ExecuteSearch(
        Query query,
        Sort sort,
        int page,
        int pageSize) {
        var directory = _index.GetOrCreateDirectory();

        using var reader = DirectoryReader.Open(directory);
        var searcher = new IndexSearcher(reader);

        var skip = (page - 1) * pageSize;
        var take = pageSize;

        // We need to get enough results to skip to our page
        var numHits = skip + take;
        var topDocs = searcher.Search(query, numHits, sort);

        var totalCount = topDocs.TotalHits;

        var hits = new List<(global::Lucene.Net.Documents.Document, float)>();

        // Skip to our page and take the requested number
        for(var i = skip; i < Math.Min(skip + take, topDocs.ScoreDocs.Length); i++) {
            var scoreDoc = topDocs.ScoreDocs[i];
            var doc = searcher.Doc(scoreDoc.Doc);
            hits.Add((doc, scoreDoc.Score));
        }

        return (hits, totalCount);
    }

    private static string? ParseOrderingField(string? ordering) {
        if(string.IsNullOrWhiteSpace(ordering)) {
            return null;
        }

        return ordering.StartsWith('-') ? ordering[1..] : ordering;
    }

    private static bool ParseOrderingDescending(string? ordering) {
        return ordering?.StartsWith('-') ?? false;
    }

    private static string GetPartsRange(int numParts) {
        return numParts switch {
            < 100 => "0-99",
            < 500 => "100-499",
            < 1000 => "500-999",
            < 2000 => "1000-1999",
            < 5000 => "2000-4999",
            _ => "5000+"
        };
    }

    private static int GetPartsRangeOrder(string range) {
        return range switch {
            "0-99" => 0,
            "100-499" => 1,
            "500-999" => 2,
            "1000-1999" => 3,
            "2000-4999" => 4,
            "5000+" => 5,
            _ => 99
        };
    }
}
