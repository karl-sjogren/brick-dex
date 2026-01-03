using BrickDex.Core.Models;
using BrickDex.Core.Models.Rebrickable;
using BrickDex.Lucene.Documents;
using Lucene.Net.Documents;

namespace BrickDex.Lucene.Tests.Documents;

public class DocumentMapperTests {
    // ToDocument (RebrickableSet) tests

    [Fact]
    public void ToDocument_RebrickableSet_SetsDocType() {
        // Arrange
        var set = CreateTestSet();

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldDocType).ShouldBe(DocumentMapper.DocTypeSet);
    }

    [Fact]
    public void ToDocument_RebrickableSet_SetsDocId() {
        // Arrange
        var set = CreateTestSet(setNum: "75192-1");

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldDocId).ShouldBe("75192-1");
    }

    [Fact]
    public void ToDocument_RebrickableSet_SetsSetNum() {
        // Arrange
        var set = CreateTestSet(setNum: "75192-1");

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldSetNum).ShouldBe("75192-1");
    }

    [Fact]
    public void ToDocument_RebrickableSet_SetsName() {
        // Arrange
        var set = CreateTestSet(name: "Millennium Falcon");

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldName).ShouldBe("Millennium Falcon");
    }

    [Fact]
    public void ToDocument_RebrickableSet_SetsYear() {
        // Arrange
        var set = CreateTestSet(year: 2017);

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.GetField(DocumentMapper.FieldYear)?.GetInt32Value().ShouldBe(2017);
    }

    [Fact]
    public void ToDocument_RebrickableSet_SetsThemeId() {
        // Arrange
        var set = CreateTestSet(themeId: 158);

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.GetField(DocumentMapper.FieldThemeId)?.GetInt32Value().ShouldBe(158);
    }

    [Fact]
    public void ToDocument_RebrickableSet_SetsNumParts() {
        // Arrange
        var set = CreateTestSet(numParts: 7541);

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.GetField(DocumentMapper.FieldNumParts)?.GetInt32Value().ShouldBe(7541);
    }

    [Fact]
    public void ToDocument_RebrickableSet_WithImageUrl_StoresImageUrl() {
        // Arrange
        var set = CreateTestSet(imageUrl: "https://example.com/image.jpg");

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldImageUrl).ShouldBe("https://example.com/image.jpg");
    }

    [Fact]
    public void ToDocument_RebrickableSet_WithoutImageUrl_NoImageUrlField() {
        // Arrange
        var set = CreateTestSet(imageUrl: null);

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldImageUrl).ShouldBeNull();
    }

    [Fact]
    public void ToDocument_RebrickableSet_WithTheme_StoresThemeName() {
        // Arrange
        var set = CreateTestSet();
        set.Theme = new RebrickableTheme { Id = 158, Name = "Star Wars" };

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldThemeName).ShouldBe("Star Wars");
    }

    [Fact]
    public void ToDocument_RebrickableSet_WithoutTheme_NoThemeNameField() {
        // Arrange
        var set = CreateTestSet();
        set.Theme = null;

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert
        doc.Get(DocumentMapper.FieldThemeName).ShouldBeNull();
    }

    [Fact]
    public void ToDocument_RebrickableSet_CreatesSearchTextField() {
        // Arrange
        var set = CreateTestSet(setNum: "75192-1", name: "Millennium Falcon");
        set.Theme = new RebrickableTheme { Id = 158, Name = "Star Wars" };

        // Act
        var doc = DocumentMapper.ToDocument(set);

        // Assert - search text is not stored, so we verify field exists
        var searchField = doc.GetField(DocumentMapper.FieldSearchText);
        searchField.ShouldNotBeNull();
    }

    // ToDocument (UserSet) tests

    [Fact]
    public void ToDocument_UserSet_SetsDocType() {
        // Arrange
        var userSet = CreateTestUserSet();

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldDocType).ShouldBe(DocumentMapper.DocTypeUserSet);
    }

    [Fact]
    public void ToDocument_UserSet_SetsDocIdAsComposite() {
        // Arrange
        var userId = Guid.NewGuid();
        var userSet = CreateTestUserSet(userId: userId, setNumber: "75192-1");

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldDocId).ShouldBe($"{userId}:75192-1");
    }

    [Fact]
    public void ToDocument_UserSet_SetsUserId() {
        // Arrange
        var userId = Guid.NewGuid();
        var userSet = CreateTestUserSet(userId: userId);

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldUserId).ShouldBe(userId.ToString());
    }

    [Fact]
    public void ToDocument_UserSet_SetsUserSetId() {
        // Arrange
        var userSetId = Guid.NewGuid();
        var userSet = CreateTestUserSet(userSetId: userSetId);

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldUserSetId).ShouldBe(userSetId.ToString());
    }

    [Fact]
    public void ToDocument_UserSet_SetsQuantity() {
        // Arrange
        var userSet = CreateTestUserSet(quantity: 3);

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.GetField(DocumentMapper.FieldQuantity)?.GetInt32Value().ShouldBe(3);
    }

    [Fact]
    public void ToDocument_UserSet_SetsStatus() {
        // Arrange
        var userSet = CreateTestUserSet(status: SetStatus.Built);

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.GetField(DocumentMapper.FieldStatus)?.GetInt32Value().ShouldBe((int)SetStatus.Built);
    }

    [Fact]
    public void ToDocument_UserSet_SetsIsWishlistTrue() {
        // Arrange
        var userSet = CreateTestUserSet(isWishlist: true);

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldIsWishlist).ShouldBe("true");
    }

    [Fact]
    public void ToDocument_UserSet_SetsIsWishlistFalse() {
        // Arrange
        var userSet = CreateTestUserSet(isWishlist: false);

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldIsWishlist).ShouldBe("false");
    }

    [Fact]
    public void ToDocument_UserSet_WithNotes_StoresNotes() {
        // Arrange
        var userSet = CreateTestUserSet(notes: "Great set!");

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldNotes).ShouldBe("Great set!");
    }

    [Fact]
    public void ToDocument_UserSet_WithSetRelation_StoresSetData() {
        // Arrange
        var userSet = CreateTestUserSet();
        userSet.Set = new RebrickableSet {
            SetNum = "75192-1",
            Name = "Millennium Falcon",
            Year = 2017,
            ThemeId = 158,
            NumParts = 7541,
            ImageUrl = "https://example.com/image.jpg",
            Theme = new RebrickableTheme { Id = 158, Name = "Star Wars" }
        };

        // Act
        var doc = DocumentMapper.ToDocument(userSet);

        // Assert
        doc.Get(DocumentMapper.FieldName).ShouldBe("Millennium Falcon");
        doc.GetField(DocumentMapper.FieldYear)?.GetInt32Value().ShouldBe(2017);
        doc.GetField(DocumentMapper.FieldThemeId)?.GetInt32Value().ShouldBe(158);
        doc.GetField(DocumentMapper.FieldNumParts)?.GetInt32Value().ShouldBe(7541);
        doc.Get(DocumentMapper.FieldImageUrl).ShouldBe("https://example.com/image.jpg");
        doc.Get(DocumentMapper.FieldThemeName).ShouldBe("Star Wars");
    }

    // ToDocument (RebrickableMinifig) tests

    [Fact]
    public void ToDocument_RebrickableMinifig_SetsDocType() {
        // Arrange
        var minifig = CreateTestMinifig();

        // Act
        var doc = DocumentMapper.ToDocument(minifig);

        // Assert
        doc.Get(DocumentMapper.FieldDocType).ShouldBe(DocumentMapper.DocTypeMinifig);
    }

    [Fact]
    public void ToDocument_RebrickableMinifig_SetsDocIdWithPrefix() {
        // Arrange
        var minifig = CreateTestMinifig(figNum: "sw0001");

        // Act
        var doc = DocumentMapper.ToDocument(minifig);

        // Assert
        doc.Get(DocumentMapper.FieldDocId).ShouldBe("minifig:sw0001");
    }

    [Fact]
    public void ToDocument_RebrickableMinifig_SetsFigNum() {
        // Arrange
        var minifig = CreateTestMinifig(figNum: "sw0001");

        // Act
        var doc = DocumentMapper.ToDocument(minifig);

        // Assert
        doc.Get(DocumentMapper.FieldFigNum).ShouldBe("sw0001");
    }

    [Fact]
    public void ToDocument_RebrickableMinifig_SetsName() {
        // Arrange
        var minifig = CreateTestMinifig(name: "Luke Skywalker");

        // Act
        var doc = DocumentMapper.ToDocument(minifig);

        // Assert
        doc.Get(DocumentMapper.FieldName).ShouldBe("Luke Skywalker");
    }

    [Fact]
    public void ToDocument_RebrickableMinifig_SetsNumParts() {
        // Arrange
        var minifig = CreateTestMinifig(numParts: 4);

        // Act
        var doc = DocumentMapper.ToDocument(minifig);

        // Assert
        doc.GetField(DocumentMapper.FieldNumParts)?.GetInt32Value().ShouldBe(4);
    }

    [Fact]
    public void ToDocument_RebrickableMinifig_WithImageUrl_StoresImageUrl() {
        // Arrange
        var minifig = CreateTestMinifig(imageUrl: "https://example.com/fig.jpg");

        // Act
        var doc = DocumentMapper.ToDocument(minifig);

        // Assert
        doc.Get(DocumentMapper.FieldImageUrl).ShouldBe("https://example.com/fig.jpg");
    }

    // ToSetSearchHit tests

    [Fact]
    public void ToSetSearchHit_MapsAllFields() {
        // Arrange
        var doc = CreateSetDocument("75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541, "https://example.com/image.jpg");
        var score = 1.5f;

        // Act
        var hit = DocumentMapper.ToSetSearchHit(doc, score);

        // Assert
        hit.SetNum.ShouldBe("75192-1");
        hit.Name.ShouldBe("Millennium Falcon");
        hit.Year.ShouldBe(2017);
        hit.ThemeId.ShouldBe(158);
        hit.ThemeName.ShouldBe("Star Wars");
        hit.NumParts.ShouldBe(7541);
        hit.ImageUrl.ShouldBe("https://example.com/image.jpg");
        hit.Score.ShouldBe(1.5f);
    }

    [Fact]
    public void ToSetSearchHit_WithMissingFields_ReturnsDefaults() {
        // Arrange
        var doc = new Document();
        var score = 1.0f;

        // Act
        var hit = DocumentMapper.ToSetSearchHit(doc, score);

        // Assert
        hit.SetNum.ShouldBe("");
        hit.Name.ShouldBe("");
        hit.Year.ShouldBe(0);
        hit.ThemeId.ShouldBe(0);
        hit.ThemeName.ShouldBeNull();
        hit.NumParts.ShouldBe(0);
        hit.ImageUrl.ShouldBeNull();
    }

    // ToUserSetSearchHit tests

    [Fact]
    public void ToUserSetSearchHit_MapsAllFields() {
        // Arrange
        var userSetId = Guid.NewGuid();
        var doc = CreateUserSetDocument(userSetId, "75192-1", "Millennium Falcon", 2017, 158, "Star Wars", 7541, 2, SetStatus.Built, false, "Notes");
        var score = 2.0f;

        // Act
        var hit = DocumentMapper.ToUserSetSearchHit(doc, score);

        // Assert
        hit.UserSetId.ShouldBe(userSetId);
        hit.SetNum.ShouldBe("75192-1");
        hit.Name.ShouldBe("Millennium Falcon");
        hit.Year.ShouldBe(2017);
        hit.ThemeId.ShouldBe(158);
        hit.ThemeName.ShouldBe("Star Wars");
        hit.NumParts.ShouldBe(7541);
        hit.Quantity.ShouldBe(2);
        hit.Status.ShouldBe(SetStatus.Built);
        hit.IsWishlist.ShouldBeFalse();
        hit.Notes.ShouldBe("Notes");
        hit.Score.ShouldBe(2.0f);
    }

    [Fact]
    public void ToUserSetSearchHit_WithIsWishlistTrue_ParsesCorrectly() {
        // Arrange
        var doc = new Document();
        doc.Add(new StringField(DocumentMapper.FieldIsWishlist, "true", Field.Store.YES));

        // Act
        var hit = DocumentMapper.ToUserSetSearchHit(doc, 1.0f);

        // Assert
        hit.IsWishlist.ShouldBeTrue();
    }

    [Fact]
    public void ToUserSetSearchHit_WithInvalidUserSetId_ReturnsEmptyGuid() {
        // Arrange
        var doc = new Document();
        doc.Add(new StringField(DocumentMapper.FieldUserSetId, "not-a-guid", Field.Store.YES));

        // Act
        var hit = DocumentMapper.ToUserSetSearchHit(doc, 1.0f);

        // Assert
        hit.UserSetId.ShouldBe(Guid.Empty);
    }

    // ToMinifigSearchHit tests

    [Fact]
    public void ToMinifigSearchHit_MapsAllFields() {
        // Arrange
        var doc = CreateMinifigDocument("sw0001", "Luke Skywalker", 4, "https://example.com/fig.jpg");
        var score = 0.8f;

        // Act
        var hit = DocumentMapper.ToMinifigSearchHit(doc, score);

        // Assert
        hit.FigNum.ShouldBe("sw0001");
        hit.Name.ShouldBe("Luke Skywalker");
        hit.NumParts.ShouldBe(4);
        hit.ImageUrl.ShouldBe("https://example.com/fig.jpg");
        hit.Score.ShouldBe(0.8f);
    }

    [Fact]
    public void ToMinifigSearchHit_WithMissingFields_ReturnsDefaults() {
        // Arrange
        var doc = new Document();
        var score = 1.0f;

        // Act
        var hit = DocumentMapper.ToMinifigSearchHit(doc, score);

        // Assert
        hit.FigNum.ShouldBe("");
        hit.Name.ShouldBe("");
        hit.NumParts.ShouldBe(0);
        hit.ImageUrl.ShouldBeNull();
    }

    // CreateDeleteTerm tests

    [Fact]
    public void CreateDeleteTerm_CreatesCorrectTerm() {
        // Arrange
        var docId = "75192-1";

        // Act
        var term = DocumentMapper.CreateDeleteTerm(docId);

        // Assert
        term.Field.ShouldBe(DocumentMapper.FieldDocId);
        term.Text.ShouldBe("75192-1");
    }

    // Helper methods

    private static RebrickableSet CreateTestSet(
        string setNum = "12345-1",
        string name = "Test Set",
        int year = 2024,
        int themeId = 1,
        int numParts = 100,
        string? imageUrl = null) {
        return new RebrickableSet {
            SetNum = setNum,
            Name = name,
            Year = year,
            ThemeId = themeId,
            NumParts = numParts,
            ImageUrl = imageUrl
        };
    }

    private static UserSet CreateTestUserSet(
        Guid? userSetId = null,
        Guid? userId = null,
        string setNumber = "12345-1",
        int quantity = 1,
        SetStatus status = SetStatus.None,
        bool isWishlist = false,
        string? notes = null) {
        return new UserSet {
            Id = userSetId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            SetNumber = setNumber,
            Quantity = quantity,
            Status = status,
            IsWishlist = isWishlist,
            Notes = notes
        };
    }

    private static RebrickableMinifig CreateTestMinifig(
        string figNum = "fig-001",
        string name = "Test Minifig",
        int numParts = 4,
        string? imageUrl = null) {
        return new RebrickableMinifig {
            FigNum = figNum,
            Name = name,
            NumParts = numParts,
            ImageUrl = imageUrl
        };
    }

    private static Document CreateSetDocument(
        string setNum,
        string name,
        int year,
        int themeId,
        string? themeName,
        int numParts,
        string? imageUrl) {
        var doc = new Document {
            new StringField(DocumentMapper.FieldSetNum, setNum, Field.Store.YES),
            new TextField(DocumentMapper.FieldName, name, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldYear, year, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldThemeId, themeId, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldNumParts, numParts, Field.Store.YES)
        };

        if(themeName != null) {
            doc.Add(new TextField(DocumentMapper.FieldThemeName, themeName, Field.Store.YES));
        }

        if(imageUrl != null) {
            doc.Add(new StoredField(DocumentMapper.FieldImageUrl, imageUrl));
        }

        return doc;
    }

    private static Document CreateUserSetDocument(
        Guid userSetId,
        string setNum,
        string name,
        int year,
        int themeId,
        string? themeName,
        int numParts,
        int quantity,
        SetStatus status,
        bool isWishlist,
        string? notes) {
        var doc = new Document {
            new StringField(DocumentMapper.FieldUserSetId, userSetId.ToString(), Field.Store.YES),
            new StringField(DocumentMapper.FieldSetNum, setNum, Field.Store.YES),
            new TextField(DocumentMapper.FieldName, name, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldYear, year, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldThemeId, themeId, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldNumParts, numParts, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldQuantity, quantity, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldStatus, (int)status, Field.Store.YES),
            new StringField(DocumentMapper.FieldIsWishlist, isWishlist.ToString().ToLowerInvariant(), Field.Store.YES)
        };

        if(themeName != null) {
            doc.Add(new TextField(DocumentMapper.FieldThemeName, themeName, Field.Store.YES));
        }

        if(notes != null) {
            doc.Add(new TextField(DocumentMapper.FieldNotes, notes, Field.Store.YES));
        }

        return doc;
    }

    private static Document CreateMinifigDocument(
        string figNum,
        string name,
        int numParts,
        string? imageUrl) {
        var doc = new Document {
            new StringField(DocumentMapper.FieldFigNum, figNum, Field.Store.YES),
            new TextField(DocumentMapper.FieldName, name, Field.Store.YES),
            new Int32Field(DocumentMapper.FieldNumParts, numParts, Field.Store.YES)
        };

        if(imageUrl != null) {
            doc.Add(new StoredField(DocumentMapper.FieldImageUrl, imageUrl));
        }

        return doc;
    }
}
