using BrickDex.Core.Contracts;
using BrickDex.Core.Services.Rebrickable;
using Microsoft.Extensions.Logging;

namespace BrickDex.Core.Services;

public class LegoThemeCache : ILegoThemeCache {
    private readonly IRebrickableClient _rebrickableClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LegoThemeCache> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    private IReadOnlyList<RebrickableTheme>? _cachedThemes;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;

    public LegoThemeCache(IRebrickableClient rebrickableClient, TimeProvider timeProvider, ILogger<LegoThemeCache> logger) {
        _rebrickableClient = rebrickableClient;
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

            _logger.LogInformation("Loading themes from Rebrickable API");

            var allThemes = new List<RebrickableTheme>();
            var page = 1;
            var hasMore = true;

            while(hasMore) {
                var result = await _rebrickableClient.GetThemesAsync(page, 1000, cancellationToken);
                allThemes.AddRange(result.Results);
                hasMore = result.Next != null;
                page++;
            }

            _cachedThemes = allThemes.OrderBy(t => t.Name).ToList();
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
