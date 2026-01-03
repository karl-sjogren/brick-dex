using BrickDex.Core.Data;
using BrickDex.Core.Models.Rebrickable;
using BrickDex.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BrickDex.Core.Tests.Services;

public class LegoThemeCacheTests : IDisposable {
    private readonly ServiceProvider _serviceProvider;
    private readonly BrickDexContext _context;
    private readonly FakeTimeProvider _timeProvider;
    private readonly LegoThemeCache _sut;

    public LegoThemeCacheTests() {
        var options = new DbContextOptionsBuilder<BrickDexContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrickDexContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var services = new ServiceCollection();
        services.AddSingleton<IBrickDexContext>(_context);
        _serviceProvider = services.BuildServiceProvider();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = new NullLogger<LegoThemeCache>();
        _sut = new LegoThemeCache(scopeFactory, _timeProvider, logger);
    }

    public void Dispose() {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    // GetThemesAsync tests

    [Fact]
    public async Task GetThemesAsync_LoadsThemesFromDbAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemesAsync(TestCancellationToken);

        // Assert
        result.Count.ShouldBe(4);
    }

    [Fact]
    public async Task GetThemesAsync_ReturnsOrderedByNameAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemesAsync(TestCancellationToken);

        // Assert
        var names = result.Select(t => t.Name).ToList();
        names.ShouldBe(names.OrderBy(n => n).ToList());
    }

    [Fact]
    public async Task GetThemesAsync_CachesResultAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act - call twice
        await _sut.GetThemesAsync(TestCancellationToken);

        // Add more themes to db (should not affect cached result)
        _context.RebrickableThemes.Add(new RebrickableTheme { Id = 999, Name = "New Theme" });
        await _context.SaveChangesAsync(TestCancellationToken);

        var result = await _sut.GetThemesAsync(TestCancellationToken);

        // Assert - should still have original count
        result.Count.ShouldBe(4);
    }

    [Fact]
    public async Task GetThemesAsync_RefreshesCacheAfterExpiryAsync() {
        // Arrange
        await SeedThemesAsync();
        await _sut.GetThemesAsync(TestCancellationToken); // Load cache

        // Add new theme
        _context.RebrickableThemes.Add(new RebrickableTheme { Id = 999, Name = "New Theme" });
        await _context.SaveChangesAsync(TestCancellationToken);

        // Advance time past cache expiry (24 hours)
        _timeProvider.Advance(TimeSpan.FromHours(25));

        // Act
        var result = await _sut.GetThemesAsync(TestCancellationToken);

        // Assert - should include new theme
        result.Count.ShouldBe(5);
    }

    // GetThemesForDisplayAsync tests

    [Fact]
    public async Task GetThemesForDisplayAsync_ReturnsHierarchicalDisplayItemsAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemesForDisplayAsync(TestCancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);

        // Root themes should have depth 0
        var starWars = result.FirstOrDefault(t => t.Name == "Star Wars");
        starWars.ShouldNotBeNull();
        starWars.Depth.ShouldBe(0);

        // Child themes should have depth 1
        var ucs = result.FirstOrDefault(t => t.Name == "Star Wars Ultimate Collector Series");
        ucs.ShouldNotBeNull();
        ucs.Depth.ShouldBe(1);
    }

    [Fact]
    public async Task GetThemesForDisplayAsync_OrdersChildrenUnderParentsAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemesForDisplayAsync(TestCancellationToken);

        // Assert - Find Star Wars and its UCS child
        var starWarsIndex = result.ToList().FindIndex(t => t.Name == "Star Wars");
        var ucsIndex = result.ToList().FindIndex(t => t.Name == "Star Wars Ultimate Collector Series");

        // UCS should come right after Star Wars
        ucsIndex.ShouldBe(starWarsIndex + 1);
    }

    // GetThemeAndDescendantIdsAsync tests

    [Fact]
    public async Task GetThemeAndDescendantIdsAsync_ReturnsThemeAndChildrenAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemeAndDescendantIdsAsync(158, TestCancellationToken); // Star Wars

        // Assert - should include Star Wars (158) and UCS (171)
        result.ShouldContain(158);
        result.ShouldContain(171);
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetThemeAndDescendantIdsAsync_WithLeafTheme_ReturnsOnlyThemeIdAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemeAndDescendantIdsAsync(1, TestCancellationToken); // Technic (no children)

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain(1);
    }

    [Fact]
    public async Task GetThemeAndDescendantIdsAsync_WithNonexistentTheme_ReturnsThemeIdAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act
        var result = await _sut.GetThemeAndDescendantIdsAsync(999, TestCancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain(999);
    }

    // InvalidateCache tests

    [Fact]
    public async Task InvalidateCache_ClearsCacheAsync() {
        // Arrange
        await SeedThemesAsync();
        await _sut.GetThemesAsync(TestCancellationToken); // Load cache

        // Add new theme
        _context.RebrickableThemes.Add(new RebrickableTheme { Id = 999, Name = "New Theme" });
        await _context.SaveChangesAsync(TestCancellationToken);

        // Act
        _sut.InvalidateCache();
        var result = await _sut.GetThemesAsync(TestCancellationToken);

        // Assert - should include new theme without waiting for expiry
        result.Count.ShouldBe(5);
    }

    // Thread safety tests

    [Fact]
    public async Task GetThemesAsync_ConcurrentCalls_LoadsCacheOnceAsync() {
        // Arrange
        await SeedThemesAsync();

        // Act - call multiple times concurrently
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _sut.GetThemesAsync(TestCancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - all should return same count
        results.ShouldAllBe(r => r.Count == 4);
    }

    // Helper methods

    private async Task SeedThemesAsync() {
        _context.RebrickableThemes.AddRange(
            new RebrickableTheme { Id = 1, Name = "Technic" },
            new RebrickableTheme { Id = 158, Name = "Star Wars" },
            new RebrickableTheme { Id = 171, Name = "Star Wars Ultimate Collector Series", ParentId = 158 },
            new RebrickableTheme { Id = 246, Name = "Architecture" }
        );
        await _context.SaveChangesAsync(TestCancellationToken);
    }
}
