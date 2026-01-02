using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BrickDex.Core.Services;

public class LegoThemeCache : ILegoThemeCache {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LegoThemeCache> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    private IReadOnlyList<RebrickableTheme>? _cachedThemes;
    private Dictionary<int, RebrickableTheme>? _themeLookup;
    private IReadOnlyList<ThemeDisplayItem>? _cachedDisplayItems;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;

    public LegoThemeCache(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<LegoThemeCache> logger) {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RebrickableTheme>> GetThemesAsync(CancellationToken cancellationToken = default) {
        await EnsureCacheLoadedAsync(cancellationToken);
        return _cachedThemes!;
    }

    public async Task<IReadOnlyList<ThemeDisplayItem>> GetThemesForDisplayAsync(CancellationToken cancellationToken = default) {
        await EnsureCacheLoadedAsync(cancellationToken);
        return _cachedDisplayItems!;
    }

    public async Task<IReadOnlyList<int>> GetThemeAndDescendantIdsAsync(int themeId, CancellationToken cancellationToken = default) {
        await EnsureCacheLoadedAsync(cancellationToken);

        if(!_themeLookup!.TryGetValue(themeId, out var theme)) {
            return [themeId];
        }

        var ids = new List<int>();
        CollectDescendantIds(theme, ids);
        return ids;
    }

    public void InvalidateCache() {
        _cachedThemes = null;
        _themeLookup = null;
        _cachedDisplayItems = null;
        _cacheExpiry = DateTimeOffset.MinValue;
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken cancellationToken) {
        // Check if we have valid cached data
        if(_cachedThemes != null && _timeProvider.GetUtcNow() < _cacheExpiry) {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try {
            // Double-check after acquiring lock
            if(_cachedThemes != null && _timeProvider.GetUtcNow() < _cacheExpiry) {
                return;
            }

            _logger.LogInformation("Loading themes from database");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

            var themes = await context.RebrickableThemes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            // Build lookup dictionary and parent/child relationships
            _themeLookup = themes.ToDictionary(t => t.Id);

            // Link children to parents (since AsNoTracking doesn't populate navigation properties)
            foreach(var theme in themes) {
                if(theme.ParentId.HasValue && _themeLookup.TryGetValue(theme.ParentId.Value, out var parent)) {
                    parent.Children.Add(theme);
                }
            }

            _cachedThemes = themes;
            _cachedDisplayItems = BuildDisplayItems(themes);
            _cacheExpiry = _timeProvider.GetUtcNow().Add(_cacheDuration);

            _logger.LogInformation("Cached {Count} themes", _cachedThemes.Count);
        } finally {
            _lock.Release();
        }
    }

    private static List<ThemeDisplayItem> BuildDisplayItems(IReadOnlyList<RebrickableTheme> themes) {
        var displayItems = new List<ThemeDisplayItem>();
        var rootThemes = themes.Where(t => !t.ParentId.HasValue).OrderBy(t => t.Name);

        foreach(var theme in rootThemes) {
            AddThemeAndChildren(theme, 0, displayItems);
        }

        return displayItems;
    }

    private static void AddThemeAndChildren(RebrickableTheme theme, int depth, List<ThemeDisplayItem> items) {
        items.Add(new ThemeDisplayItem(theme.Id, theme.Name, depth));

        foreach(var child in theme.Children.OrderBy(c => c.Name)) {
            AddThemeAndChildren(child, depth + 1, items);
        }
    }

    private static void CollectDescendantIds(RebrickableTheme theme, List<int> ids) {
        ids.Add(theme.Id);

        foreach(var child in theme.Children) {
            CollectDescendantIds(child, ids);
        }
    }
}
