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
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;

    public LegoThemeCache(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<LegoThemeCache> logger) {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RebrickableTheme>> GetThemesAsync(CancellationToken cancellationToken = default) {
        // Check if we have valid cached data
        if(_cachedThemes != null && _timeProvider.GetUtcNow() < _cacheExpiry) {
            return _cachedThemes;
        }

        await _lock.WaitAsync(cancellationToken);
        try {
            // Double-check after acquiring lock
            if(_cachedThemes != null && _timeProvider.GetUtcNow() < _cacheExpiry) {
                return _cachedThemes;
            }

            _logger.LogInformation("Loading themes from database");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBrickDexContext>();

            _cachedThemes = await context.RebrickableThemes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);

            _cacheExpiry = _timeProvider.GetUtcNow().Add(_cacheDuration);

            _logger.LogInformation("Cached {Count} themes", _cachedThemes.Count);

            return _cachedThemes;
        } finally {
            _lock.Release();
        }
    }

    public void InvalidateCache() {
        _cachedThemes = null;
        _cacheExpiry = DateTimeOffset.MinValue;
    }
}
