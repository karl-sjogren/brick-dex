using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.Core.Services;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Tests.Services;

public class LegoSetServiceTests : IDisposable {
    private readonly BrickDexContext _context;
    private readonly IRebrickableClient _rebrickableClient;
    private readonly FakeTimeProvider _timeProvider;
    private readonly LegoSetService _sut;
    private readonly User _testUser;

    public LegoSetServiceTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _rebrickableClient = A.Fake<IRebrickableClient>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        var logger = new NullLogger<LegoSetService>();

        _sut = new LegoSetService(_context, _rebrickableClient, _timeProvider, logger);

        // Create test user
        _testUser = new User {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            CreatedAt = _timeProvider.GetUtcNow(),
            UpdatedAt = _timeProvider.GetUtcNow()
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // Shared set operations

    [Fact]
    public async Task GetByIdAsync_WhenSetExists_ReturnsSetAsync() {
        // Arrange
        var set = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

        // Act
        var result = await _sut.GetByIdAsync(set.Id, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Millennium Falcon");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSetDoesNotExist_ReturnsNullAsync() {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetBySetNumberAsync_WhenSetExists_ReturnsSetAsync() {
        // Arrange
        await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

        // Act
        var result = await _sut.GetBySetNumberAsync("75192-1", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Millennium Falcon");
    }

    [Fact]
    public async Task GetBySetNumberAsync_NormalizesSetNumberAsync() {
        // Arrange
        await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

        // Act
        var result = await _sut.GetBySetNumberAsync("75192", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.SetNumber.ShouldBe("75192-1");
    }

    [Fact]
    public async Task GetOrCreateFromRebrickableAsync_WhenSetExists_ReturnsExistingSetAsync() {
        // Arrange
        var existingSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Existing Set" });

        // Act
        var result = await _sut.GetOrCreateFromRebrickableAsync("75192", TestCancellationToken);

        // Assert
        result.Id.ShouldBe(existingSet.Id);
        result.Name.ShouldBe("Existing Set");
        A.CallTo(() => _rebrickableClient.GetSetAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetOrCreateFromRebrickableAsync_WhenSetNotFoundOnRebrickable_ThrowsExceptionAsync() {
        // Arrange
        A.CallTo(() => _rebrickableClient.GetSetAsync("99999-1", A<CancellationToken>._))
            .Returns((RebrickableSet?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.GetOrCreateFromRebrickableAsync("99999", TestCancellationToken));

        exception.Message.ShouldContain("not found on Rebrickable");
    }

    [Fact]
    public async Task GetOrCreateFromRebrickableAsync_WhenSetFoundOnRebrickable_CreatesSetAsync() {
        // Arrange
        var rebrickableSet = new RebrickableSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon",
            Year = 2017,
            NumParts = 7541,
            ThemeId = 158,
            SetImageUrl = "https://example.com/image.jpg",
            SetUrl = "https://rebrickable.com/sets/75192-1/"
        };

        A.CallTo(() => _rebrickableClient.GetSetAsync("75192-1", A<CancellationToken>._))
            .Returns(rebrickableSet);

        A.CallTo(() => _rebrickableClient.GetThemesAsync(A<int>._, A<int>._, A<CancellationToken>._))
            .Returns(new RebrickableSearchResult<RebrickableTheme> {
                Results = [new RebrickableTheme { Id = 158, Name = "Star Wars" }]
            });

        // Act
        var result = await _sut.GetOrCreateFromRebrickableAsync("75192", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.SetNumber.ShouldBe("75192-1");
        result.Name.ShouldBe("Millennium Falcon");
        result.Year.ShouldBe(2017);
        result.NumParts.ShouldBe(7541);
        result.ThemeName.ShouldBe("Star Wars");
    }

    // User-specific operations

    [Fact]
    public async Task GetUserSetsAsync_WhenNoSets_ReturnsEmptyListAsync() {
        // Act
        var result = await _sut.GetUserSetsAsync(_testUser.Id, new UserSetFilters(), TestCancellationToken);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetUserSetsAsync_ExcludesWishlistAsync() {
        // Arrange
        var collectionSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "1-1", Name = "Collection Set" });
        var wishlistSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "2-1", Name = "Wishlist Set" });

        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = collectionSet.Id, IsWishlist = false });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = wishlistSet.Id, IsWishlist = true });

        // Act
        var result = await _sut.GetUserSetsAsync(_testUser.Id, new UserSetFilters(), TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].LegoSet.Name.ShouldBe("Collection Set");
    }

    [Fact]
    public async Task GetUserWishlistAsync_ReturnsOnlyWishlistAsync() {
        // Arrange
        var collectionSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "1-1", Name = "Collection Set" });
        var wishlistSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "2-1", Name = "Wishlist Set" });

        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = collectionSet.Id, IsWishlist = false });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = wishlistSet.Id, IsWishlist = true });

        // Act
        var result = await _sut.GetUserWishlistAsync(_testUser.Id, new UserSetFilters(), TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].LegoSet.Name.ShouldBe("Wishlist Set");
    }

    [Fact]
    public async Task GetUserSetsAsync_ReturnsSortedByNameAsync() {
        // Arrange
        var zebraSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "1-1", Name = "Zebra Set" });
        var alphaSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "2-1", Name = "Alpha Set" });
        var middleSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "3-1", Name = "Middle Set" });

        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = zebraSet.Id });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = alphaSet.Id });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = middleSet.Id });

        // Act
        var result = await _sut.GetUserSetsAsync(_testUser.Id, new UserSetFilters { SortBy = "name" }, TestCancellationToken);

        // Assert
        result.Items[0].LegoSet.Name.ShouldBe("Alpha Set");
        result.Items[1].LegoSet.Name.ShouldBe("Middle Set");
        result.Items[2].LegoSet.Name.ShouldBe("Zebra Set");
    }

    [Fact]
    public async Task GetUserSetsAsync_OnlyReturnsCurrentUserSetsAsync() {
        // Arrange
        var otherUser = new User {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            CreatedAt = _timeProvider.GetUtcNow(),
            UpdatedAt = _timeProvider.GetUtcNow()
        };
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync(TestCancellationToken);

        var set1 = await SeedLegoSetAsync(new LegoSet { SetNumber = "1-1", Name = "My Set" });
        var set2 = await SeedLegoSetAsync(new LegoSet { SetNumber = "2-1", Name = "Other User Set" });

        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = set1.Id });
        await SeedUserSetAsync(new UserSet { UserId = otherUser.Id, LegoSetId = set2.Id });

        // Act
        var result = await _sut.GetUserSetsAsync(_testUser.Id, new UserSetFilters(), TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].LegoSet.Name.ShouldBe("My Set");
    }

    [Fact]
    public async Task GetUserSetAsync_WhenSetExists_ReturnsUserSetAsync() {
        // Arrange
        var legoSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = legoSet.Id, Notes = "My favorite" });

        // Act
        var result = await _sut.GetUserSetAsync(_testUser.Id, legoSet.Id, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Notes.ShouldBe("My favorite");
        result.LegoSet.Name.ShouldBe("Millennium Falcon");
    }

    [Fact]
    public async Task GetUserSetBySetNumberAsync_ReturnsCorrectUserSetAsync() {
        // Arrange
        var legoSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = legoSet.Id, Quantity = 2 });

        // Act
        var result = await _sut.GetUserSetBySetNumberAsync(_testUser.Id, "75192", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task AddToUserCollectionAsync_WhenSetAlreadyInCollection_ThrowsExceptionAsync() {
        // Arrange
        var legoSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Existing Set" });
        await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = legoSet.Id });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AddToUserCollectionAsync(_testUser.Id, "75192", cancellationToken: TestCancellationToken));

        exception.Message.ShouldContain("already in your collection");
    }

    [Fact]
    public async Task AddToUserCollectionAsync_CreatesUserSetAsync() {
        // Arrange
        var rebrickableSet = new RebrickableSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon",
            Year = 2017,
            NumParts = 7541,
            ThemeId = 158
        };

        A.CallTo(() => _rebrickableClient.GetSetAsync("75192-1", A<CancellationToken>._))
            .Returns(rebrickableSet);

        A.CallTo(() => _rebrickableClient.GetThemesAsync(A<int>._, A<int>._, A<CancellationToken>._))
            .Returns(new RebrickableSearchResult<RebrickableTheme> {
                Results = [new RebrickableTheme { Id = 158, Name = "Star Wars" }]
            });

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUser.Id, "75192", cancellationToken: TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(_testUser.Id);
        result.Quantity.ShouldBe(1);
        result.IsWishlist.ShouldBeFalse();
        result.LegoSet.SetNumber.ShouldBe("75192-1");
        result.LegoSet.Name.ShouldBe("Millennium Falcon");
    }

    [Fact]
    public async Task AddToUserCollectionAsync_WhenIsWishlist_SetsWishlistFlagAsync() {
        // Arrange
        var rebrickableSet = new RebrickableSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon",
            ThemeId = 158
        };

        A.CallTo(() => _rebrickableClient.GetSetAsync("75192-1", A<CancellationToken>._))
            .Returns(rebrickableSet);

        A.CallTo(() => _rebrickableClient.GetThemesAsync(A<int>._, A<int>._, A<CancellationToken>._))
            .Returns(new RebrickableSearchResult<RebrickableTheme> {
                Results = [new RebrickableTheme { Id = 158, Name = "Star Wars" }]
            });

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUser.Id, "75192", isWishlist: true, cancellationToken: TestCancellationToken);

        // Assert
        result.IsWishlist.ShouldBeTrue();
    }

    [Fact]
    public async Task AddToUserCollectionAsync_UsesExistingSharedLegoSetAsync() {
        // Arrange - create shared set first
        var existingLegoSet = await SeedLegoSetAsync(new LegoSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon",
            Year = 2017
        });

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUser.Id, "75192", cancellationToken: TestCancellationToken);

        // Assert
        result.LegoSetId.ShouldBe(existingLegoSet.Id);
        A.CallTo(() => _rebrickableClient.GetSetAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateUserSetAsync_WhenSetExists_UpdatesSetAsync() {
        // Arrange
        var legoSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });
        var userSet = await SeedUserSetAsync(new UserSet {
            UserId = _testUser.Id,
            LegoSetId = legoSet.Id,
            Quantity = 1,
            Status = SetStatus.None
        });

        var updatedUserSet = new UserSet {
            Id = userSet.Id,
            UserId = _testUser.Id,
            LegoSetId = legoSet.Id,
            Quantity = 2,
            Notes = "My favorite set",
            Status = SetStatus.Built,
            IsWishlist = false
        };

        _timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        var result = await _sut.UpdateUserSetAsync(updatedUserSet, TestCancellationToken);

        // Assert
        result.Quantity.ShouldBe(2);
        result.Notes.ShouldBe("My favorite set");
        result.Status.ShouldBe(SetStatus.Built);
        result.UpdatedAt.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task UpdateUserSetAsync_WhenSetDoesNotExist_ThrowsExceptionAsync() {
        // Arrange
        var updatedUserSet = new UserSet {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Quantity = 1
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateUserSetAsync(updatedUserSet, TestCancellationToken));

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task RemoveFromUserCollectionAsync_WhenSetExists_DeletesUserSetAsync() {
        // Arrange
        var legoSet = await SeedLegoSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });
        var userSet = await SeedUserSetAsync(new UserSet { UserId = _testUser.Id, LegoSetId = legoSet.Id });

        // Act
        await _sut.RemoveFromUserCollectionAsync(_testUser.Id, userSet.Id, TestCancellationToken);

        // Assert
        var deletedUserSet = await _context.UserSets.FindAsync([userSet.Id], TestCancellationToken);
        deletedUserSet.ShouldBeNull();

        // LegoSet should still exist (shared data)
        var existingLegoSet = await _context.LegoSets.FindAsync([legoSet.Id], TestCancellationToken);
        existingLegoSet.ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveFromUserCollectionAsync_WhenSetDoesNotExist_ThrowsExceptionAsync() {
        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.RemoveFromUserCollectionAsync(_testUser.Id, Guid.NewGuid(), TestCancellationToken));

        exception.Message.ShouldContain("not found");
    }

    private async Task<LegoSet> SeedLegoSetAsync(LegoSet set) {
        set.Id = Guid.NewGuid();
        set.CreatedAt = _timeProvider.GetUtcNow();
        set.UpdatedAt = _timeProvider.GetUtcNow();

        _context.LegoSets.Add(set);
        await _context.SaveChangesAsync(TestCancellationToken);

        return set;
    }

    private async Task<UserSet> SeedUserSetAsync(UserSet userSet) {
        userSet.Id = Guid.NewGuid();
        userSet.CreatedAt = _timeProvider.GetUtcNow();
        userSet.UpdatedAt = _timeProvider.GetUtcNow();

        _context.UserSets.Add(userSet);
        await _context.SaveChangesAsync(TestCancellationToken);

        return userSet;
    }
}
