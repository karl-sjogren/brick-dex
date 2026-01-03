using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using BrickDex.Core.Models.Rebrickable;
using BrickDex.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Tests.Services;

public class UserSetServiceTests : IDisposable {
    private readonly BrickDexContext _context;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ISearchService _fakeSearchService;
    private readonly ISearchIndex _fakeSearchIndex;
    private readonly UserSetService _sut;

    private readonly Guid _testUserId = Guid.NewGuid();

    public UserSetServiceTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _fakeSearchService = A.Fake<ISearchService>();
        _fakeSearchIndex = A.Fake<ISearchIndex>();

        var logger = new NullLogger<UserSetService>();
        _sut = new UserSetService(_context, _timeProvider, logger, _fakeSearchService, _fakeSearchIndex);
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    // NormalizeSetNumber tests (tested indirectly through AddToUserCollectionAsync)

    [Fact]
    public async Task AddToUserCollectionAsync_WithSetNumberWithoutDash_AppendsOneAsync() {
        // Arrange
        await SeedSetAsync("12345-1", "Test Set");

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUserId, "12345", cancellationToken: TestCancellationToken);

        // Assert
        result.SetNumber.ShouldBe("12345-1");
    }

    [Fact]
    public async Task AddToUserCollectionAsync_WithSetNumberWithDash_KeepsOriginalAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Assert
        result.SetNumber.ShouldBe("75192-1");
    }

    // AddToUserCollectionAsync tests

    [Fact]
    public async Task AddToUserCollectionAsync_WithValidSet_AddsToCollectionAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(_testUserId);
        result.SetNumber.ShouldBe("75192-1");
        result.IsWishlist.ShouldBeFalse();
        result.Status.ShouldBe(SetStatus.None);
        result.Quantity.ShouldBe(1);
        result.Set.Name.ShouldBe("Millennium Falcon");

        var dbUserSet = await _context.UserSets.FirstOrDefaultAsync(TestCancellationToken);
        dbUserSet.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddToUserCollectionAsync_WhenWishlistTrue_SetsIsWishlistAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", isWishlist: true, TestCancellationToken);

        // Assert
        result.IsWishlist.ShouldBeTrue();
    }

    [Fact]
    public async Task AddToUserCollectionAsync_CallsSearchIndexAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");

        // Act
        await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Assert
        A.CallTo(() => _fakeSearchIndex.IndexUserSetAsync(_testUserId, "75192-1", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddToUserCollectionAsync_WithNonexistentSet_ThrowsExceptionAsync() {
        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AddToUserCollectionAsync(_testUserId, "99999-1", cancellationToken: TestCancellationToken));

        ex.Message.ShouldContain("99999-1");
        ex.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task AddToUserCollectionAsync_WhenSetAlreadyExists_ThrowsExceptionAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken));

        ex.Message.ShouldContain("already in your collection");
    }

    [Fact]
    public async Task AddToUserCollectionAsync_SetsTimestampsAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");

        // Act
        var result = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Assert
        result.CreatedAt.ShouldBe(_timeProvider.GetUtcNow());
        result.UpdatedAt.ShouldBe(_timeProvider.GetUtcNow());
    }

    // UpdateUserSetAsync tests

    [Fact]
    public async Task UpdateUserSetAsync_UpdatesAllFieldsAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var userSet = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        _timeProvider.Advance(TimeSpan.FromHours(1));

        userSet.Status = SetStatus.Built;
        userSet.Quantity = 2;
        userSet.Notes = "Great set!";
        userSet.IsWishlist = true;

        // Act
        var result = await _sut.UpdateUserSetAsync(userSet, TestCancellationToken);

        // Assert
        var dbUserSet = await _context.UserSets.FindAsync([userSet.Id], TestCancellationToken);
        dbUserSet!.Status.ShouldBe(SetStatus.Built);
        dbUserSet.Quantity.ShouldBe(2);
        dbUserSet.Notes.ShouldBe("Great set!");
        dbUserSet.IsWishlist.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateUserSetAsync_UpdatesTimestampAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var userSet = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);
        var originalUpdatedAt = userSet.UpdatedAt;

        _timeProvider.Advance(TimeSpan.FromHours(1));
        userSet.Status = SetStatus.Built;

        // Act
        await _sut.UpdateUserSetAsync(userSet, TestCancellationToken);

        // Assert
        var dbUserSet = await _context.UserSets.FindAsync([userSet.Id], TestCancellationToken);
        dbUserSet!.UpdatedAt.ShouldBeGreaterThan(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateUserSetAsync_CallsSearchIndexAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var userSet = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);
        Fake.ClearRecordedCalls(_fakeSearchIndex);

        userSet.Status = SetStatus.Built;

        // Act
        await _sut.UpdateUserSetAsync(userSet, TestCancellationToken);

        // Assert
        A.CallTo(() => _fakeSearchIndex.IndexUserSetAsync(_testUserId, "75192-1", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateUserSetAsync_WithNonexistentUserSet_ThrowsExceptionAsync() {
        // Arrange
        var nonexistentUserSet = new UserSet { Id = Guid.NewGuid(), SetNumber = "99999-1" };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateUserSetAsync(nonexistentUserSet, TestCancellationToken));
    }

    // RemoveFromUserCollectionAsync tests

    [Fact]
    public async Task RemoveFromUserCollectionAsync_RemovesSetAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var userSet = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Act
        await _sut.RemoveFromUserCollectionAsync(_testUserId, userSet.Id, TestCancellationToken);

        // Assert
        var dbUserSet = await _context.UserSets.FindAsync([userSet.Id], TestCancellationToken);
        dbUserSet.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFromUserCollectionAsync_CallsRemoveUserSetAsyncAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var userSet = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Act
        await _sut.RemoveFromUserCollectionAsync(_testUserId, userSet.Id, TestCancellationToken);

        // Assert
        A.CallTo(() => _fakeSearchIndex.RemoveUserSetAsync(_testUserId, "75192-1", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RemoveFromUserCollectionAsync_WithNonexistentUserSet_ThrowsExceptionAsync() {
        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.RemoveFromUserCollectionAsync(_testUserId, Guid.NewGuid(), TestCancellationToken));
    }

    [Fact]
    public async Task RemoveFromUserCollectionAsync_WithWrongUser_ThrowsExceptionAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var userSet = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);
        var otherUserId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.RemoveFromUserCollectionAsync(otherUserId, userSet.Id, TestCancellationToken));
    }

    // GetUserSetsAsync tests

    [Fact]
    public async Task GetUserSetsAsync_ReturnsOnlyCollectionItemsAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        await SeedSetAsync("42056-1", "Porsche");
        await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", isWishlist: false, TestCancellationToken);
        await _sut.AddToUserCollectionAsync(_testUserId, "42056-1", isWishlist: true, TestCancellationToken);

        // Act
        var result = await _sut.GetUserSetsAsync(_testUserId, new UserSetFilters(), TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].SetNumber.ShouldBe("75192-1");
    }

    [Fact]
    public async Task GetUserWishlistAsync_ReturnsOnlyWishlistItemsAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        await SeedSetAsync("42056-1", "Porsche");
        await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", isWishlist: false, TestCancellationToken);
        await _sut.AddToUserCollectionAsync(_testUserId, "42056-1", isWishlist: true, TestCancellationToken);

        // Act
        var result = await _sut.GetUserWishlistAsync(_testUserId, new UserSetFilters(), TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].SetNumber.ShouldBe("42056-1");
    }

    [Fact]
    public async Task GetUserSetsAsync_AppliesPaginationAsync() {
        // Arrange
        await SeedMultipleSetsAsync(5);
        for(var i = 1; i <= 5; i++) {
            await _sut.AddToUserCollectionAsync(_testUserId, $"set-{i}-1", cancellationToken: TestCancellationToken);
        }

        var filters = new UserSetFilters { Page = 2, PageSize = 2 };

        // Act
        var result = await _sut.GetUserSetsAsync(_testUserId, filters, TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(2);
    }

    [Fact]
    public async Task GetUserSetsAsync_FiltersbyQueryAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        await SeedSetAsync("42056-1", "Porsche 911");
        await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);
        await _sut.AddToUserCollectionAsync(_testUserId, "42056-1", cancellationToken: TestCancellationToken);

        var filters = new UserSetFilters { Query = "falcon" };

        // Act
        var result = await _sut.GetUserSetsAsync(_testUserId, filters, TestCancellationToken);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items[0].SetNumber.ShouldBe("75192-1");
    }

    // GetUserSetAsync tests

    [Fact]
    public async Task GetUserSetAsync_ReturnsSetWithDetailsAsync() {
        // Arrange
        await SeedSetAsync("75192-1", "Millennium Falcon");
        var created = await _sut.AddToUserCollectionAsync(_testUserId, "75192-1", cancellationToken: TestCancellationToken);

        // Act
        var result = await _sut.GetUserSetAsync(_testUserId, created.Id, TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Set.ShouldNotBeNull();
        result.Set.Name.ShouldBe("Millennium Falcon");
    }

    [Fact]
    public async Task GetUserSetAsync_WithNonexistentId_ReturnsNullAsync() {
        // Act
        var result = await _sut.GetUserSetAsync(_testUserId, Guid.NewGuid(), TestCancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    // Helper methods

    private async Task SeedSetAsync(string setNum, string name, int? themeId = null) {
        if(themeId.HasValue && !await _context.RebrickableThemes.AnyAsync(t => t.Id == themeId.Value, TestCancellationToken)) {
            _context.RebrickableThemes.Add(new RebrickableTheme { Id = themeId.Value, Name = $"Theme {themeId}" });
        }

        _context.RebrickableSets.Add(new RebrickableSet {
            SetNum = setNum,
            Name = name,
            Year = 2024,
            ThemeId = themeId ?? 1,
            NumParts = 1000
        });

        if(!await _context.RebrickableThemes.AnyAsync(t => t.Id == 1, TestCancellationToken)) {
            _context.RebrickableThemes.Add(new RebrickableTheme { Id = 1, Name = "Default Theme" });
        }

        await _context.SaveChangesAsync(TestCancellationToken);
    }

    private async Task SeedMultipleSetsAsync(int count) {
        if(!await _context.RebrickableThemes.AnyAsync(t => t.Id == 1, TestCancellationToken)) {
            _context.RebrickableThemes.Add(new RebrickableTheme { Id = 1, Name = "Default Theme" });
        }

        for(var i = 1; i <= count; i++) {
            _context.RebrickableSets.Add(new RebrickableSet {
                SetNum = $"set-{i}-1",
                Name = $"Set {i}",
                Year = 2024,
                ThemeId = 1,
                NumParts = 100 * i
            });
        }

        await _context.SaveChangesAsync(TestCancellationToken);
    }
}
