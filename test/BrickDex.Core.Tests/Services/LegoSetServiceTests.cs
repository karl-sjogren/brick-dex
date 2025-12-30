using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.Core.Services.Rebrickable;
using BrickDex.Web.Data;
using BrickDex.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Core.Tests.Services;

public class LegoSetServiceTests : IDisposable {
    private readonly BrickDexContext _context;
    private readonly IRebrickableClient _rebrickableClient;
    private readonly FakeTimeProvider _timeProvider;
    private readonly LegoSetService _sut;

    public LegoSetServiceTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _rebrickableClient = A.Fake<IRebrickableClient>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        var logger = new NullLogger<LegoSetService>();

        _sut = new LegoSetService(_context, _rebrickableClient, _timeProvider, logger);
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSets_ReturnsEmptyListAsync() {
        // Act
        var result = await _sut.GetAllAsync(cancellationToken: TestCancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ExcludesWishlistByDefaultAsync() {
        // Arrange
        await SeedSetAsync(new LegoSet { SetNumber = "1-1", Name = "Collection Set", IsWishlist = false });
        await SeedSetAsync(new LegoSet { SetNumber = "2-1", Name = "Wishlist Set", IsWishlist = true });

        // Act
        var result = await _sut.GetAllAsync(cancellationToken: TestCancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Collection Set");
    }

    [Fact]
    public async Task GetAllAsync_IncludesWishlistWhenRequestedAsync() {
        // Arrange
        await SeedSetAsync(new LegoSet { SetNumber = "1-1", Name = "Collection Set", IsWishlist = false });
        await SeedSetAsync(new LegoSet { SetNumber = "2-1", Name = "Wishlist Set", IsWishlist = true });

        // Act
        var result = await _sut.GetAllAsync(includeWishlist: true, cancellationToken: TestCancellationToken);

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSortedByNameAsync() {
        // Arrange
        await SeedSetAsync(new LegoSet { SetNumber = "1-1", Name = "Zebra Set" });
        await SeedSetAsync(new LegoSet { SetNumber = "2-1", Name = "Alpha Set" });
        await SeedSetAsync(new LegoSet { SetNumber = "3-1", Name = "Middle Set" });

        // Act
        var result = await _sut.GetAllAsync(cancellationToken: TestCancellationToken);

        // Assert
        result[0].Name.ShouldBe("Alpha Set");
        result[1].Name.ShouldBe("Middle Set");
        result[2].Name.ShouldBe("Zebra Set");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSetExists_ReturnsSetAsync() {
        // Arrange
        var set = await SeedSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

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
        await SeedSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

        // Act
        var result = await _sut.GetBySetNumberAsync("75192-1", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Millennium Falcon");
    }

    [Fact]
    public async Task GetBySetNumberAsync_NormalizesSetNumberAsync() {
        // Arrange
        await SeedSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

        // Act
        var result = await _sut.GetBySetNumberAsync("75192", TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.SetNumber.ShouldBe("75192-1");
    }

    [Fact]
    public async Task AddAsync_CreatesNewSetAsync() {
        // Arrange
        var legoSet = new LegoSet {
            SetNumber = "75192",
            Name = "Millennium Falcon",
            Year = 2017,
            NumParts = 7541
        };

        // Act
        var result = await _sut.AddAsync(legoSet, TestCancellationToken);

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
        result.SetNumber.ShouldBe("75192-1"); // Normalized
        result.CreatedAt.ShouldBe(_timeProvider.GetUtcNow());
        result.UpdatedAt.ShouldBe(_timeProvider.GetUtcNow());

        var dbSet = await _context.LegoSets.FindAsync([result.Id], TestCancellationToken);
        dbSet.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddFromRebrickableAsync_WhenSetExists_ThrowsExceptionAsync() {
        // Arrange
        await SeedSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Existing Set" });

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AddFromRebrickableAsync("75192", cancellationToken: TestCancellationToken));

        exception.Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task AddFromRebrickableAsync_WhenSetNotFoundOnRebrickable_ThrowsExceptionAsync() {
        // Arrange
        A.CallTo(() => _rebrickableClient.GetSetAsync("99999-1", A<CancellationToken>._))
            .Returns((RebrickableSet?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.AddFromRebrickableAsync("99999", cancellationToken: TestCancellationToken));

        exception.Message.ShouldContain("not found on Rebrickable");
    }

    [Fact]
    public async Task AddFromRebrickableAsync_WhenSetFoundOnRebrickable_AddsSetAsync() {
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
        var result = await _sut.AddFromRebrickableAsync("75192", cancellationToken: TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.SetNumber.ShouldBe("75192-1");
        result.Name.ShouldBe("Millennium Falcon");
        result.Year.ShouldBe(2017);
        result.NumParts.ShouldBe(7541);
        result.ThemeName.ShouldBe("Star Wars");
        result.IsWishlist.ShouldBeFalse();
    }

    [Fact]
    public async Task AddFromRebrickableAsync_WhenIsWishlist_SetsWishlistFlagAsync() {
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
        var result = await _sut.AddFromRebrickableAsync("75192", isWishlist: true, cancellationToken: TestCancellationToken);

        // Assert
        result.IsWishlist.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WhenSetExists_UpdatesSetAsync() {
        // Arrange
        var set = await SeedSetAsync(new LegoSet {
            SetNumber = "75192-1",
            Name = "Millennium Falcon",
            Quantity = 1,
            Status = SetStatus.None
        });

        var updatedSet = new LegoSet {
            Id = set.Id,
            SetNumber = set.SetNumber,
            Name = set.Name,
            Quantity = 2,
            Notes = "My favorite set",
            Status = SetStatus.Built,
            IsWishlist = false
        };

        _timeProvider.Advance(TimeSpan.FromHours(1));

        // Act
        var result = await _sut.UpdateAsync(updatedSet, TestCancellationToken);

        // Assert
        result.Quantity.ShouldBe(2);
        result.Notes.ShouldBe("My favorite set");
        result.Status.ShouldBe(SetStatus.Built);
        result.UpdatedAt.ShouldBe(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task UpdateAsync_WhenSetDoesNotExist_ThrowsExceptionAsync() {
        // Arrange
        var updatedSet = new LegoSet {
            Id = Guid.NewGuid(),
            SetNumber = "99999-1",
            Name = "Non-existent"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(updatedSet, TestCancellationToken));

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteAsync_WhenSetExists_DeletesSetAsync() {
        // Arrange
        var set = await SeedSetAsync(new LegoSet { SetNumber = "75192-1", Name = "Millennium Falcon" });

        // Act
        await _sut.DeleteAsync(set.Id, TestCancellationToken);

        // Assert
        var deletedSet = await _context.LegoSets.FindAsync([set.Id], TestCancellationToken);
        deletedSet.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenSetDoesNotExist_ThrowsExceptionAsync() {
        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.DeleteAsync(Guid.NewGuid(), TestCancellationToken));

        exception.Message.ShouldContain("not found");
    }

    private async Task<LegoSet> SeedSetAsync(LegoSet set) {
        set.Id = Guid.NewGuid();
        set.CreatedAt = _timeProvider.GetUtcNow();
        set.UpdatedAt = _timeProvider.GetUtcNow();

        _context.LegoSets.Add(set);
        await _context.SaveChangesAsync(TestCancellationToken);

        return set;
    }
}
