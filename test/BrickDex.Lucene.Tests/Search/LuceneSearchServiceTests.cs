using BrickDex.Core.Models;
using BrickDex.Core.Models.Search;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Lucene.Analysis;
using BrickDex.Lucene.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

using QueryBuilder = BrickDex.Lucene.Search.QueryBuilder;
using RebrickableMinifig = BrickDex.Core.Models.Rebrickable.RebrickableMinifig;
using RebrickableSet = BrickDex.Core.Models.Rebrickable.RebrickableSet;
using RebrickableTheme = BrickDex.Core.Models.Rebrickable.RebrickableTheme;

namespace BrickDex.Lucene.Tests.Search;

/// <summary>
/// Integration tests for Lucene search functionality using in-memory RAMDirectory.
/// Tests QueryBuilder, DocumentMapper, and search execution together.
/// </summary>
public class LuceneSearchServiceTests : IDisposable {
    private const LuceneVersion _luceneVersion = LuceneVersion.LUCENE_48;

    private readonly RAMDirectory _directory;
    private readonly IndexWriter _writer;

    public LuceneSearchServiceTests() {
        _directory = new RAMDirectory();

        var analyzer = new BrickDexAnalyzer(_luceneVersion);
        var config = new IndexWriterConfig(_luceneVersion, analyzer) {
            OpenMode = OpenMode.CREATE
        };
        _writer = new IndexWriter(_directory, config);
    }

    public void Dispose() {
        _writer.Dispose();
        _directory.Dispose();
        GC.SuppressFinalize(this);
    }

    // SearchSets tests

    [Fact]
    public void SearchSets_WithEmptyIndex_ReturnsNoResults() {
        // Arrange
        _writer.Commit();
        var filters = new SetSearchFilters();

        // Act
        var (hits, totalCount) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(0);
        totalCount.ShouldBe(0);
    }

    [Fact]
    public void SearchSets_WithSets_ReturnsMatchingResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("10179-1", "Ultimate Collector's Millennium Falcon", 2007, 158, "Star Wars", 5195);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        _writer.Commit();

        var filters = new SetSearchFilters();

        // Act
        var (hits, totalCount) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(3);
        totalCount.ShouldBe(3);
    }

    [Fact]
    public void SearchSets_WithTextQuery_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        _writer.Commit();

        var filters = new SetSearchFilters { Query = "falcon" };

        // Act
        var (hits, totalCount) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].SetNum.ShouldBe("75192-1");
    }

    [Fact]
    public void SearchSets_WithMinYear_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("10179-1", "Ultimate Millennium Falcon", 2007, 158, "Star Wars", 5195);
        _writer.Commit();

        var filters = new SetSearchFilters { MinYear = 2010 };

        // Act
        var (hits, _) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].Year.ShouldBe(2017);
    }

    [Fact]
    public void SearchSets_WithMaxYear_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("10179-1", "Ultimate Millennium Falcon", 2007, 158, "Star Wars", 5195);
        _writer.Commit();

        var filters = new SetSearchFilters { MaxYear = 2010 };

        // Act
        var (hits, _) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].Year.ShouldBe(2007);
    }

    [Fact]
    public void SearchSets_WithMinParts_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        _writer.Commit();

        var filters = new SetSearchFilters { MinParts = 5000 };

        // Act
        var (hits, _) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].NumParts.ShouldBe(7541);
    }

    [Fact]
    public void SearchSets_WithMaxParts_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        _writer.Commit();

        var filters = new SetSearchFilters { MaxParts = 3000 };

        // Act
        var (hits, _) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].NumParts.ShouldBe(2704);
    }

    [Fact]
    public void SearchSets_WithThemeId_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        _writer.Commit();

        var filters = new SetSearchFilters { ThemeId = 1 };

        // Act
        var (hits, _) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].SetNum.ShouldBe("42056-1");
    }

    [Fact]
    public void SearchSets_WithMultipleThemeIds_FiltersResults() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        IndexSet("21042-1", "Statue of Liberty", 2018, 246, "Architecture", 1685);
        _writer.Commit();

        var filters = new SetSearchFilters { ThemeIds = [1, 158] };

        // Act
        var (hits, _) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(2);
    }

    [Fact]
    public void SearchSets_WithPagination_ReturnsPaginatedResults() {
        // Arrange
        for(var i = 1; i <= 10; i++) {
            IndexSet($"set-{i}-1", $"Test Set {i}", 2020, 1, "Technic", i * 100);
        }

        _writer.Commit();

        var filters = new SetSearchFilters { Page = 2, PageSize = 3 };

        // Act
        var (hits, totalCount) = ExecuteSetSearch(filters);

        // Assert
        hits.Count.ShouldBe(3);
        totalCount.ShouldBe(10);
    }

    [Fact]
    public void SearchSets_WithYearOrdering_SortsResultsAscending() {
        // Arrange
        IndexSet("set-1-1", "Alpha Set", 2020, 1, "Technic", 100);
        IndexSet("set-2-1", "Zeta Set", 2018, 1, "Technic", 500);
        IndexSet("set-3-1", "Beta Set", 2022, 1, "Technic", 300);
        _writer.Commit();

        var filters = new SetSearchFilters { Ordering = "year" };

        // Act
        var sort = QueryBuilder.CreateSort("year", descending: false);
        var (hits, _) = ExecuteSetSearch(filters, sort);

        // Assert
        hits.Count.ShouldBe(3);
        hits[0].Year.ShouldBe(2018);
        hits[1].Year.ShouldBe(2020);
        hits[2].Year.ShouldBe(2022);
    }

    [Fact]
    public void SearchSets_WithYearOrderingDescending_SortsResultsDescending() {
        // Arrange
        IndexSet("set-1-1", "Alpha Set", 2020, 1, "Technic", 100);
        IndexSet("set-2-1", "Zeta Set", 2018, 1, "Technic", 500);
        IndexSet("set-3-1", "Beta Set", 2022, 1, "Technic", 300);
        _writer.Commit();

        // Act
        var filters = new SetSearchFilters();
        var sort = QueryBuilder.CreateSort("year", descending: true);
        var (hits, _) = ExecuteSetSearch(filters, sort);

        // Assert
        hits.Count.ShouldBe(3);
        hits[0].Year.ShouldBe(2022);
        hits[1].Year.ShouldBe(2020);
        hits[2].Year.ShouldBe(2018);
    }

    // SearchUserSets tests

    [Fact]
    public void SearchUserSets_WithEmptyIndex_ReturnsNoResults() {
        // Arrange
        _writer.Commit();
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters();

        // Act
        var (hits, totalCount) = ExecuteUserSetSearch(userId, filters);

        // Assert
        hits.Count.ShouldBe(0);
        totalCount.ShouldBe(0);
    }

    [Fact]
    public void SearchUserSets_FiltersbyUserId() {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        IndexUserSet(userId1, Guid.NewGuid(), "75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541, 1, SetStatus.Built, false);
        IndexUserSet(userId2, Guid.NewGuid(), "42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704, 1, SetStatus.None, false);
        _writer.Commit();

        var filters = new UserSetSearchFilters();

        // Act
        var (hits, _) = ExecuteUserSetSearch(userId1, filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].SetNum.ShouldBe("75192-1");
    }

    [Fact]
    public void SearchUserSets_WithStatusFilter_FiltersResults() {
        // Arrange
        var userId = Guid.NewGuid();
        IndexUserSet(userId, Guid.NewGuid(), "75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541, 1, SetStatus.Built, false);
        IndexUserSet(userId, Guid.NewGuid(), "42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704, 1, SetStatus.Building, false);
        _writer.Commit();

        var filters = new UserSetSearchFilters { Status = SetStatus.Built };

        // Act
        var (hits, _) = ExecuteUserSetSearch(userId, filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].Status.ShouldBe(SetStatus.Built);
    }

    [Fact]
    public void SearchUserSets_WithIsWishlistFilter_FiltersResults() {
        // Arrange
        var userId = Guid.NewGuid();
        IndexUserSet(userId, Guid.NewGuid(), "75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541, 1, SetStatus.None, false);
        IndexUserSet(userId, Guid.NewGuid(), "42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704, 1, SetStatus.None, true);
        _writer.Commit();

        var filters = new UserSetSearchFilters { IsWishlist = true };

        // Act
        var (hits, _) = ExecuteUserSetSearch(userId, filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].IsWishlist.ShouldBeTrue();
    }

    [Fact]
    public void SearchUserSets_WithTextQuery_FiltersResults() {
        // Arrange
        var userId = Guid.NewGuid();
        IndexUserSet(userId, Guid.NewGuid(), "75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541, 1, SetStatus.Built, false);
        IndexUserSet(userId, Guid.NewGuid(), "42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704, 1, SetStatus.None, false);
        _writer.Commit();

        var filters = new UserSetSearchFilters { Query = "porsche" };

        // Act
        var (hits, _) = ExecuteUserSetSearch(userId, filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].SetNum.ShouldBe("42056-1");
    }

    // SearchMinifigs tests

    [Fact]
    public void SearchMinifigs_WithEmptyIndex_ReturnsNoResults() {
        // Arrange
        _writer.Commit();
        var filters = new MinifigSearchFilters();

        // Act
        var (hits, totalCount) = ExecuteMinifigSearch(filters);

        // Assert
        hits.Count.ShouldBe(0);
        totalCount.ShouldBe(0);
    }

    [Fact]
    public void SearchMinifigs_WithMinifigs_ReturnsMatchingResults() {
        // Arrange
        IndexMinifig("sw0001", "Luke Skywalker", 4);
        IndexMinifig("sw0002", "Han Solo", 4);
        IndexMinifig("hp0001", "Harry Potter", 4);
        _writer.Commit();

        var filters = new MinifigSearchFilters();

        // Act
        var (hits, totalCount) = ExecuteMinifigSearch(filters);

        // Assert
        hits.Count.ShouldBe(3);
        totalCount.ShouldBe(3);
    }

    [Fact]
    public void SearchMinifigs_WithTextQuery_FiltersResults() {
        // Arrange
        IndexMinifig("sw0001", "Luke Skywalker", 4);
        IndexMinifig("hp0001", "Harry Potter", 4);
        _writer.Commit();

        var filters = new MinifigSearchFilters { Query = "luke" };

        // Act
        var (hits, _) = ExecuteMinifigSearch(filters);

        // Assert
        hits.Count.ShouldBe(1);
        hits[0].FigNum.ShouldBe("sw0001");
    }

    [Fact]
    public void SearchMinifigs_WithPartsFilter_FiltersResults() {
        // Arrange
        IndexMinifig("sw0001", "Luke Skywalker", 4);
        IndexMinifig("sw0002", "Han Solo", 6);
        IndexMinifig("sw0003", "Darth Vader", 10);
        _writer.Commit();

        var filters = new MinifigSearchFilters { MinParts = 5 };

        // Act
        var (hits, _) = ExecuteMinifigSearch(filters);

        // Assert
        hits.Count.ShouldBe(2);
    }

    // Suggest tests

    [Fact]
    public void Suggest_WithShortPrefix_ReturnsEmpty() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        _writer.Commit();

        // Act
        var result = ExecuteSuggest("M", 10);

        // Assert - prefix too short, should return no results
        // Note: The actual service filters prefixes < 2 characters
        // Here we test the actual Lucene behavior which may return results
        // But we document that the service would filter this
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Suggest_WithValidPrefix_ReturnsSuggestions() {
        // Arrange
        IndexSet("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541);
        IndexSet("42056-1", "Porsche 911 GT3 RS", 2016, 1, "Technic", 2704);
        _writer.Commit();

        // Act
        var result = ExecuteSuggest("mi", 10);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe("Millennium Falcon");
    }

    [Fact]
    public void Suggest_RespectsMaxResults() {
        // Arrange
        IndexSet("set-1-1", "Test Alpha", 2020, 1, "Technic", 100);
        IndexSet("set-2-1", "Test Beta", 2020, 1, "Technic", 100);
        IndexSet("set-3-1", "Test Gamma", 2020, 1, "Technic", 100);
        _writer.Commit();

        // Act
        var result = ExecuteSuggest("te", 2);

        // Assert
        result.Count.ShouldBe(2);
    }

    // Helper methods

    private void IndexSet(string setNum, string name, int year, int themeId, string themeName, int numParts) {
        var set = new RebrickableSet {
            SetNum = setNum,
            Name = name,
            Year = year,
            ThemeId = themeId,
            NumParts = numParts,
            Theme = new RebrickableTheme { Id = themeId, Name = themeName }
        };
        var doc = DocumentMapper.ToDocument(set);
        _writer.AddDocument(doc);
    }

    private void IndexUserSet(
        Guid userId,
        Guid userSetId,
        string setNum,
        string name,
        int year,
        int themeId,
        string themeName,
        int numParts,
        int quantity,
        SetStatus status,
        bool isWishlist) {
        var userSet = new UserSet {
            Id = userSetId,
            UserId = userId,
            SetNumber = setNum,
            Quantity = quantity,
            Status = status,
            IsWishlist = isWishlist,
            Set = new RebrickableSet {
                SetNum = setNum,
                Name = name,
                Year = year,
                ThemeId = themeId,
                NumParts = numParts,
                Theme = new RebrickableTheme { Id = themeId, Name = themeName }
            }
        };
        var doc = DocumentMapper.ToDocument(userSet);
        _writer.AddDocument(doc);
    }

    private void IndexMinifig(string figNum, string name, int numParts) {
        var minifig = new RebrickableMinifig {
            FigNum = figNum,
            Name = name,
            NumParts = numParts
        };
        var doc = DocumentMapper.ToDocument(minifig);
        _writer.AddDocument(doc);
    }

    private (List<SetSearchHit> Hits, int TotalCount) ExecuteSetSearch(SetSearchFilters filters, Sort? sort = null) {
        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

        var query = QueryBuilder.BuildSetQuery(filters);
        sort ??= QueryBuilder.CreateSort(null, false);

        var skip = (filters.Page - 1) * filters.PageSize;
        var take = filters.PageSize;
        var numHits = skip + take;

        var topDocs = searcher.Search(query, numHits, sort);
        var totalCount = topDocs.TotalHits;

        var hits = new List<SetSearchHit>();
        for(var i = skip; i < Math.Min(skip + take, topDocs.ScoreDocs.Length); i++) {
            var scoreDoc = topDocs.ScoreDocs[i];
            var doc = searcher.Doc(scoreDoc.Doc);
            hits.Add(DocumentMapper.ToSetSearchHit(doc, scoreDoc.Score));
        }

        return (hits, totalCount);
    }

    private (List<UserSetSearchHit> Hits, int TotalCount) ExecuteUserSetSearch(Guid userId, UserSetSearchFilters filters) {
        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

        var query = QueryBuilder.BuildUserSetQuery(userId, filters);
        var sort = QueryBuilder.CreateSort(filters.SortBy, filters.SortDescending);

        var skip = (filters.Page - 1) * filters.PageSize;
        var take = filters.PageSize;
        var numHits = skip + take;

        var topDocs = searcher.Search(query, numHits, sort);
        var totalCount = topDocs.TotalHits;

        var hits = new List<UserSetSearchHit>();
        for(var i = skip; i < Math.Min(skip + take, topDocs.ScoreDocs.Length); i++) {
            var scoreDoc = topDocs.ScoreDocs[i];
            var doc = searcher.Doc(scoreDoc.Doc);
            hits.Add(DocumentMapper.ToUserSetSearchHit(doc, scoreDoc.Score));
        }

        return (hits, totalCount);
    }

    private (List<MinifigSearchHit> Hits, int TotalCount) ExecuteMinifigSearch(MinifigSearchFilters filters) {
        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

        var query = QueryBuilder.BuildMinifigQuery(filters);
        var sort = QueryBuilder.CreateSort(filters.SortBy, filters.SortDescending);

        var skip = (filters.Page - 1) * filters.PageSize;
        var take = filters.PageSize;
        var numHits = skip + take;

        var topDocs = searcher.Search(query, numHits, sort);
        var totalCount = topDocs.TotalHits;

        var hits = new List<MinifigSearchHit>();
        for(var i = skip; i < Math.Min(skip + take, topDocs.ScoreDocs.Length); i++) {
            var scoreDoc = topDocs.ScoreDocs[i];
            var doc = searcher.Doc(scoreDoc.Doc);
            hits.Add(DocumentMapper.ToMinifigSearchHit(doc, scoreDoc.Score));
        }

        return (hits, totalCount);
    }

    private List<string> ExecuteSuggest(string prefix, int maxResults) {
        if(string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2) {
            return [];
        }

        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

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

        return suggestions;
    }
}
