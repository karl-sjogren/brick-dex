using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Web.Services;

public class LegoSetService : ILegoSetService {
    private readonly BrickDexContext _context;
    private readonly IRebrickableClient _rebrickableClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LegoSetService> _logger;

    private static readonly Dictionary<int, string> _themeCache = [];

    public LegoSetService(
        BrickDexContext context,
        IRebrickableClient rebrickableClient,
        TimeProvider timeProvider,
        ILogger<LegoSetService> logger) {
        _context = context;
        _rebrickableClient = rebrickableClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LegoSet>> GetAllAsync(bool includeWishlist = false, CancellationToken cancellationToken = default) {
        var query = _context.LegoSets.AsNoTracking();

        if(!includeWishlist) {
            query = query.Where(s => !s.IsWishlist);
        }

        return await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<LegoSet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await _context.LegoSets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<LegoSet?> GetBySetNumberAsync(string setNumber, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);
        return await _context.LegoSets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SetNumber == normalizedSetNumber, cancellationToken);
    }

    public async Task<LegoSet> AddAsync(LegoSet legoSet, CancellationToken cancellationToken = default) {
        var now = _timeProvider.GetUtcNow();
        legoSet.Id = Guid.NewGuid();
        legoSet.CreatedAt = now;
        legoSet.UpdatedAt = now;
        legoSet.SetNumber = NormalizeSetNumber(legoSet.SetNumber);

        _context.LegoSets.Add(legoSet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added LEGO set {SetNumber} - {Name}", legoSet.SetNumber, legoSet.Name);
        return legoSet;
    }

    public async Task<LegoSet> AddFromRebrickableAsync(string setNumber, bool isWishlist = false, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);

        // Check if set already exists
        var existingSet = await GetBySetNumberAsync(normalizedSetNumber, cancellationToken);
        if(existingSet != null) {
            _logger.LogWarning("Set {SetNumber} already exists in collection", normalizedSetNumber);
            throw new InvalidOperationException($"Set {normalizedSetNumber} already exists in collection");
        }

        // Fetch from Rebrickable
        var rebrickableSet = await _rebrickableClient.GetSetAsync(normalizedSetNumber, cancellationToken);
        if(rebrickableSet == null) {
            _logger.LogWarning("Set {SetNumber} not found on Rebrickable", normalizedSetNumber);
            throw new InvalidOperationException($"Set {normalizedSetNumber} not found on Rebrickable");
        }

        // Get theme name
        var themeName = await GetThemeNameAsync(rebrickableSet.ThemeId, cancellationToken);

        var legoSet = new LegoSet {
            SetNumber = rebrickableSet.SetNumber,
            Name = rebrickableSet.Name,
            Year = rebrickableSet.Year,
            NumParts = rebrickableSet.NumParts,
            ThemeName = themeName,
            ImageUrl = rebrickableSet.SetImageUrl,
            SetUrl = rebrickableSet.SetUrl,
            IsWishlist = isWishlist,
            Quantity = 1
        };

        return await AddAsync(legoSet, cancellationToken);
    }

    public async Task<LegoSet> UpdateAsync(LegoSet legoSet, CancellationToken cancellationToken = default) {
        var existingSet = await _context.LegoSets
            .FirstOrDefaultAsync(s => s.Id == legoSet.Id, cancellationToken);

        if(existingSet == null) {
            throw new InvalidOperationException($"Set with ID {legoSet.Id} not found");
        }

        existingSet.Quantity = legoSet.Quantity;
        existingSet.Notes = legoSet.Notes;
        existingSet.IsWishlist = legoSet.IsWishlist;
        existingSet.Status = legoSet.Status;
        existingSet.UpdatedAt = _timeProvider.GetUtcNow();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated LEGO set {SetNumber}", existingSet.SetNumber);
        return existingSet;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) {
        var legoSet = await _context.LegoSets
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if(legoSet == null) {
            throw new InvalidOperationException($"Set with ID {id} not found");
        }

        _context.LegoSets.Remove(legoSet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted LEGO set {SetNumber}", legoSet.SetNumber);
    }

    private async Task<string?> GetThemeNameAsync(int themeId, CancellationToken cancellationToken) {
        if(_themeCache.TryGetValue(themeId, out var cachedName)) {
            return cachedName;
        }

        // Load all themes if cache is empty
        if(_themeCache.Count == 0) {
            await LoadThemeCacheAsync(cancellationToken);
        }

        return _themeCache.GetValueOrDefault(themeId);
    }

    private async Task LoadThemeCacheAsync(CancellationToken cancellationToken) {
        var page = 1;
        var hasMore = true;

        while(hasMore) {
            var result = await _rebrickableClient.GetThemesAsync(page, 1000, cancellationToken);

            foreach(var theme in result.Results) {
                _themeCache[theme.Id] = theme.Name;
            }

            hasMore = result.Next != null;
            page++;
        }

        _logger.LogInformation("Loaded {Count} themes into cache", _themeCache.Count);
    }

    private static string NormalizeSetNumber(string setNumber) {
        if(!setNumber.Contains('-', StringComparison.Ordinal)) {
            return $"{setNumber}-1";
        }

        return setNumber;
    }
}
