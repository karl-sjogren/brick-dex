using BrickDex.Core.Models.Search;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Lucene.Analysis;
using BrickDex.Lucene.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace BrickDex.Lucene.Search;

/// <summary>
/// Builds Lucene queries from search filters.
/// </summary>
public static class QueryBuilder {
    private const LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;

    /// <summary>
    /// Builds a query for searching sets.
    /// </summary>
    public static Query BuildSetQuery(SetSearchFilters filters) {
        var query = new BooleanQuery();

        // Filter by document type
        query.Add(
            new TermQuery(new Term(DocumentMapper.FieldDocType, DocumentMapper.DocTypeSet)),
            Occur.MUST);

        // Text search
        if(!string.IsNullOrWhiteSpace(filters.Query)) {
            var textQuery = ParseTextQuery(filters.Query);
            query.Add(textQuery, Occur.MUST);
        }

        // Year range
        AddRangeFilter(query, DocumentMapper.FieldYear, filters.MinYear, filters.MaxYear);

        // Parts range
        AddRangeFilter(query, DocumentMapper.FieldNumParts, filters.MinParts, filters.MaxParts);

        // Theme filter
        AddThemeFilter(query, filters.ThemeIds, filters.ThemeId);

        return query;
    }

    /// <summary>
    /// Builds a query for searching user sets.
    /// </summary>
    public static Query BuildUserSetQuery(Guid userId, UserSetSearchFilters filters) {
        var query = new BooleanQuery();

        // Filter by document type
        query.Add(
            new TermQuery(new Term(DocumentMapper.FieldDocType, DocumentMapper.DocTypeUserSet)),
            Occur.MUST);

        // Filter by user
        query.Add(
            new TermQuery(new Term(DocumentMapper.FieldUserId, userId.ToString())),
            Occur.MUST);

        // Text search
        if(!string.IsNullOrWhiteSpace(filters.Query)) {
            var textQuery = ParseTextQuery(filters.Query);
            query.Add(textQuery, Occur.MUST);
        }

        // Year range
        AddRangeFilter(query, DocumentMapper.FieldYear, filters.MinYear, filters.MaxYear);

        // Parts range
        AddRangeFilter(query, DocumentMapper.FieldNumParts, filters.MinParts, filters.MaxParts);

        // Theme filter
        if(filters.ThemeId.HasValue) {
            query.Add(
                NumericRangeQuery.NewInt32Range(
                    DocumentMapper.FieldThemeId,
                    filters.ThemeId.Value,
                    filters.ThemeId.Value,
                    minInclusive: true,
                    maxInclusive: true),
                Occur.MUST);
        }

        // Status filter
        if(filters.Status.HasValue) {
            query.Add(
                NumericRangeQuery.NewInt32Range(
                    DocumentMapper.FieldStatus,
                    (int)filters.Status.Value,
                    (int)filters.Status.Value,
                    minInclusive: true,
                    maxInclusive: true),
                Occur.MUST);
        }

        // Wishlist filter
        if(filters.IsWishlist.HasValue) {
            query.Add(
                new TermQuery(new Term(
                    DocumentMapper.FieldIsWishlist,
                    filters.IsWishlist.Value.ToString().ToLowerInvariant())),
                Occur.MUST);
        }

        return query;
    }

    /// <summary>
    /// Builds a query for searching minifigs.
    /// </summary>
    public static Query BuildMinifigQuery(MinifigSearchFilters filters) {
        var query = new BooleanQuery();

        // Filter by document type
        query.Add(
            new TermQuery(new Term(DocumentMapper.FieldDocType, DocumentMapper.DocTypeMinifig)),
            Occur.MUST);

        // Text search
        if(!string.IsNullOrWhiteSpace(filters.Query)) {
            var textQuery = ParseTextQuery(filters.Query);
            query.Add(textQuery, Occur.MUST);
        }

        // Parts range
        AddRangeFilter(query, DocumentMapper.FieldNumParts, filters.MinParts, filters.MaxParts);

        return query;
    }

    /// <summary>
    /// Creates a sort for the given field and direction.
    /// </summary>
    public static Sort CreateSort(string? sortBy, bool descending) {
        var sortField = sortBy?.ToLowerInvariant() switch {
            "year" => new SortField(DocumentMapper.FieldYear + "_sort", SortFieldType.INT32, descending),
            "num_parts" or "parts" => new SortField(DocumentMapper.FieldNumParts + "_sort", SortFieldType.INT32, descending),
            "set_num" or "setnumber" => new SortField(DocumentMapper.FieldSetNum, SortFieldType.STRING, descending),
            "name" => new SortField(DocumentMapper.FieldNameSort, SortFieldType.STRING, descending),
            _ => new SortField(DocumentMapper.FieldNameSort, SortFieldType.STRING, descending)
        };

        return new Sort(sortField);
    }

    private static Query ParseTextQuery(string queryText) {
        var analyzer = new BrickDexAnalyzer(_luceneVersion);
        var parser = new QueryParser(_luceneVersion, DocumentMapper.FieldSearchText, analyzer) {
            DefaultOperator = Operator.AND,
            AllowLeadingWildcard = false
        };

        try {
            // Escape special characters for safety
            var escapedQuery = QueryParserBase.Escape(queryText);
            return parser.Parse(escapedQuery);
        } catch(ParseException) {
            // Fallback to simple term query if parsing fails
            return new TermQuery(new Term(DocumentMapper.FieldSearchText, queryText.ToLowerInvariant()));
        }
    }

    private static void AddRangeFilter(BooleanQuery query, string field, int? min, int? max) {
        if(!min.HasValue && !max.HasValue) {
            return;
        }

        var rangeQuery = NumericRangeQuery.NewInt32Range(
            field,
            min ?? int.MinValue,
            max ?? int.MaxValue,
            minInclusive: true,
            maxInclusive: true);

        query.Add(rangeQuery, Occur.MUST);
    }

    private static void AddThemeFilter(BooleanQuery query, IReadOnlyList<int>? themeIds, int? singleThemeId) {
        // Use expanded theme IDs if available, otherwise fall back to single theme ID
        if(themeIds is { Count: > 0 }) {
            if(themeIds.Count == 1) {
                // Single theme - use exact match
                query.Add(
                    NumericRangeQuery.NewInt32Range(
                        DocumentMapper.FieldThemeId,
                        themeIds[0],
                        themeIds[0],
                        minInclusive: true,
                        maxInclusive: true),
                    Occur.MUST);
            } else {
                // Multiple themes - OR them together
                var themeQuery = new BooleanQuery();
                foreach(var themeId in themeIds) {
                    themeQuery.Add(
                        NumericRangeQuery.NewInt32Range(
                            DocumentMapper.FieldThemeId,
                            themeId,
                            themeId,
                            minInclusive: true,
                            maxInclusive: true),
                        Occur.SHOULD);
                }

                query.Add(themeQuery, Occur.MUST);
            }
        } else if(singleThemeId.HasValue) {
            // Fall back to single theme ID (legacy behavior)
            query.Add(
                NumericRangeQuery.NewInt32Range(
                    DocumentMapper.FieldThemeId,
                    singleThemeId.Value,
                    singleThemeId.Value,
                    minInclusive: true,
                    maxInclusive: true),
                Occur.MUST);
        }
    }
}
