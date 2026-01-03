using BrickDex.Core.Models;
using BrickDex.Core.Models.Search;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Lucene.Documents;
using BrickDex.Lucene.Search;

namespace BrickDex.Lucene.Tests.Search;

public class QueryBuilderTests {
    // BuildSetQuery tests

    [Fact]
    public void BuildSetQuery_WithEmptyFilters_ReturnsDocTypeQuery() {
        // Arrange
        var filters = new SetSearchFilters();

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain($"{DocumentMapper.FieldDocType}:{DocumentMapper.DocTypeSet}");
    }

    [Fact]
    public void BuildSetQuery_WithTextQuery_IncludesTextSearch() {
        // Arrange
        var filters = new SetSearchFilters { Query = "millennium falcon" };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldSearchText);
    }

    [Fact]
    public void BuildSetQuery_WithMinYear_IncludesYearRangeFilter() {
        // Arrange
        var filters = new SetSearchFilters { MinYear = 2020 };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldYear);
    }

    [Fact]
    public void BuildSetQuery_WithMaxYear_IncludesYearRangeFilter() {
        // Arrange
        var filters = new SetSearchFilters { MaxYear = 2024 };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldYear);
    }

    [Fact]
    public void BuildSetQuery_WithYearRange_IncludesYearRangeFilter() {
        // Arrange
        var filters = new SetSearchFilters { MinYear = 2020, MaxYear = 2024 };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldYear);
    }

    [Fact]
    public void BuildSetQuery_WithMinParts_IncludesPartsRangeFilter() {
        // Arrange
        var filters = new SetSearchFilters { MinParts = 500 };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldNumParts);
    }

    [Fact]
    public void BuildSetQuery_WithMaxParts_IncludesPartsRangeFilter() {
        // Arrange
        var filters = new SetSearchFilters { MaxParts = 1000 };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldNumParts);
    }

    [Fact]
    public void BuildSetQuery_WithSingleThemeId_IncludesThemeFilter() {
        // Arrange
        var filters = new SetSearchFilters { ThemeId = 158 };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldThemeId);
    }

    [Fact]
    public void BuildSetQuery_WithMultipleThemeIds_IncludesThemeFilter() {
        // Arrange
        var filters = new SetSearchFilters { ThemeIds = [158, 171, 209] };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldThemeId);
    }

    [Fact]
    public void BuildSetQuery_WithSingleThemeIdInList_UsesExactMatch() {
        // Arrange
        var filters = new SetSearchFilters { ThemeIds = [158] };

        // Act
        var query = QueryBuilder.BuildSetQuery(filters);

        // Assert - should have exactly one theme filter
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldThemeId);
    }

    // BuildUserSetQuery tests

    [Fact]
    public void BuildUserSetQuery_WithEmptyFilters_ReturnsDocTypeAndUserIdQuery() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters();

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain($"{DocumentMapper.FieldDocType}:{DocumentMapper.DocTypeUserSet}");
        queryString.ShouldContain($"{DocumentMapper.FieldUserId}:{userId}");
    }

    [Fact]
    public void BuildUserSetQuery_WithTextQuery_IncludesTextSearch() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { Query = "star wars" };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldSearchText);
    }

    [Fact]
    public void BuildUserSetQuery_WithYearRange_IncludesYearRangeFilter() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { MinYear = 2020, MaxYear = 2024 };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldYear);
    }

    [Fact]
    public void BuildUserSetQuery_WithPartsRange_IncludesPartsRangeFilter() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { MinParts = 100, MaxParts = 500 };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldNumParts);
    }

    [Fact]
    public void BuildUserSetQuery_WithThemeId_IncludesThemeFilter() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { ThemeId = 158 };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldThemeId);
    }

    [Fact]
    public void BuildUserSetQuery_WithStatus_IncludesStatusFilter() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { Status = SetStatus.Built };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldStatus);
    }

    [Fact]
    public void BuildUserSetQuery_WithIsWishlistTrue_IncludesWishlistFilter() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { IsWishlist = true };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain($"{DocumentMapper.FieldIsWishlist}:true");
    }

    [Fact]
    public void BuildUserSetQuery_WithIsWishlistFalse_IncludesWishlistFilter() {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new UserSetSearchFilters { IsWishlist = false };

        // Act
        var query = QueryBuilder.BuildUserSetQuery(userId, filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain($"{DocumentMapper.FieldIsWishlist}:false");
    }

    // BuildMinifigQuery tests

    [Fact]
    public void BuildMinifigQuery_WithEmptyFilters_ReturnsDocTypeQuery() {
        // Arrange
        var filters = new MinifigSearchFilters();

        // Act
        var query = QueryBuilder.BuildMinifigQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain($"{DocumentMapper.FieldDocType}:{DocumentMapper.DocTypeMinifig}");
    }

    [Fact]
    public void BuildMinifigQuery_WithTextQuery_IncludesTextSearch() {
        // Arrange
        var filters = new MinifigSearchFilters { Query = "luke skywalker" };

        // Act
        var query = QueryBuilder.BuildMinifigQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldSearchText);
    }

    [Fact]
    public void BuildMinifigQuery_WithPartsRange_IncludesPartsRangeFilter() {
        // Arrange
        var filters = new MinifigSearchFilters { MinParts = 3, MaxParts = 10 };

        // Act
        var query = QueryBuilder.BuildMinifigQuery(filters);

        // Assert
        var queryString = query.ToString();
        queryString.ShouldContain(DocumentMapper.FieldNumParts);
    }

    // CreateSort tests

    [Fact]
    public void CreateSort_WithYearAscending_ReturnsSortByYear() {
        // Act
        var sort = QueryBuilder.CreateSort("year", descending: false);

        // Assert
        sort.ShouldNotBeNull();
        sort.GetSort().Length.ShouldBe(1);
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldYear + "_sort");
        sort.GetSort()[0].IsReverse.ShouldBeFalse();
    }

    [Fact]
    public void CreateSort_WithYearDescending_ReturnsSortByYearDescending() {
        // Act
        var sort = QueryBuilder.CreateSort("year", descending: true);

        // Assert
        sort.GetSort()[0].IsReverse.ShouldBeTrue();
    }

    [Fact]
    public void CreateSort_WithNumParts_ReturnsSortByNumParts() {
        // Act
        var sort = QueryBuilder.CreateSort("num_parts", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldNumParts + "_sort");
    }

    [Fact]
    public void CreateSort_WithParts_ReturnsSortByNumParts() {
        // Act
        var sort = QueryBuilder.CreateSort("parts", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldNumParts + "_sort");
    }

    [Fact]
    public void CreateSort_WithSetNum_ReturnsSortBySetNum() {
        // Act
        var sort = QueryBuilder.CreateSort("set_num", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldSetNum);
    }

    [Fact]
    public void CreateSort_WithSetNumber_ReturnsSortBySetNum() {
        // Act
        var sort = QueryBuilder.CreateSort("setnumber", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldSetNum);
    }

    [Fact]
    public void CreateSort_WithName_ReturnsSortByName() {
        // Act
        var sort = QueryBuilder.CreateSort("name", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldNameSort);
    }

    [Fact]
    public void CreateSort_WithNull_DefaultsToName() {
        // Act
        var sort = QueryBuilder.CreateSort(null, descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldNameSort);
    }

    [Fact]
    public void CreateSort_WithUnknown_DefaultsToName() {
        // Act
        var sort = QueryBuilder.CreateSort("unknown_field", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldNameSort);
    }

    [Fact]
    public void CreateSort_IsCaseInsensitive() {
        // Act
        var sort = QueryBuilder.CreateSort("YEAR", descending: false);

        // Assert
        sort.GetSort()[0].Field.ShouldBe(DocumentMapper.FieldYear + "_sort");
    }
}
