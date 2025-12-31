using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrickDex.Core.Services;

public class LegoSetService : ILegoSetService {
    private readonly IBrickDexContext _context;
    private readonly IRebrickableClient _rebrickableClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LegoSetService> _logger;

    private static readonly Dictionary<int, string> _themeCache = [];

    public LegoSetService(
        IBrickDexContext context,
        IRebrickableClient rebrickableClient,
        TimeProvider timeProvider,
        ILogger<LegoSetService> logger) {
        _context = context;
        _rebrickableClient = rebrickableClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Shared set operations

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

    public async Task<LegoSet> GetOrCreateFromRebrickableAsync(string setNumber, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);

        // Check if set already exists
        var existingSet = await _context.LegoSets
            .FirstOrDefaultAsync(s => s.SetNumber == normalizedSetNumber, cancellationToken);

        if(existingSet != null) {
            return existingSet;
        }

        // Fetch from Rebrickable
        var rebrickableSet = await _rebrickableClient.GetSetAsync(normalizedSetNumber, cancellationToken);
        if(rebrickableSet == null) {
            _logger.LogWarning("Set {SetNumber} not found on Rebrickable", normalizedSetNumber);
            throw new InvalidOperationException($"Set {normalizedSetNumber} not found on Rebrickable");
        }

        // Get theme name
        var themeName = await GetThemeNameAsync(rebrickableSet.ThemeId, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var legoSet = new LegoSet {
            Id = Guid.NewGuid(),
            SetNumber = rebrickableSet.SetNumber,
            Name = rebrickableSet.Name,
            Year = rebrickableSet.Year,
            NumParts = rebrickableSet.NumParts,
            ThemeName = themeName,
            ImageUrl = rebrickableSet.SetImageUrl,
            SetUrl = rebrickableSet.SetUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.LegoSets.Add(legoSet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created LEGO set {SetNumber} - {Name}", legoSet.SetNumber, legoSet.Name);
        return legoSet;
    }

    // User-specific operations

    public async Task<PagedResult<UserSet>> GetUserSetsAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default) {
        var query = _context.UserSets
            .AsNoTracking()
            .Include(us => us.LegoSet)
            .Where(us => us.UserId == userId && !us.IsWishlist);

        query = ApplyFiltersAndSorting(query, filters);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSet>(items, totalCount, filters.Page, filters.PageSize);
    }

    public async Task<PagedResult<UserSet>> GetUserWishlistAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default) {
        var query = _context.UserSets
            .AsNoTracking()
            .Include(us => us.LegoSet)
            .Where(us => us.UserId == userId && us.IsWishlist);

        query = ApplyFiltersAndSorting(query, filters);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSet>(items, totalCount, filters.Page, filters.PageSize);
    }

    private static IQueryable<UserSet> ApplyFiltersAndSorting(IQueryable<UserSet> query, UserSetFilters filters) {
        // Text filter - using ToLower() for EF Core SQL translation (ToLowerInvariant not supported)
        if(!string.IsNullOrWhiteSpace(filters.Query)) {
#pragma warning disable CA1304 // ToLower is intentional for EF Core SQL translation
            var q = filters.Query.ToLower();
            query = query.Where(us =>
                us.LegoSet.Name.ToLower().Contains(q) ||
                us.LegoSet.SetNumber.ToLower().Contains(q) ||
                (us.LegoSet.ThemeName != null && us.LegoSet.ThemeName.ToLower().Contains(q)));
#pragma warning restore CA1304
        }

        // Dynamic sorting
        query = filters.SortBy?.ToLowerInvariant() switch {
            "year" => filters.SortDescending
                ? query.OrderByDescending(us => us.LegoSet.Year)
                : query.OrderBy(us => us.LegoSet.Year),
            "parts" => filters.SortDescending
                ? query.OrderByDescending(us => us.LegoSet.NumParts)
                : query.OrderBy(us => us.LegoSet.NumParts),
            "theme" => filters.SortDescending
                ? query.OrderByDescending(us => us.LegoSet.ThemeName)
                : query.OrderBy(us => us.LegoSet.ThemeName),
            "setnumber" => filters.SortDescending
                ? query.OrderByDescending(us => us.LegoSet.SetNumber)
                : query.OrderBy(us => us.LegoSet.SetNumber),
            _ => filters.SortDescending
                ? query.OrderByDescending(us => us.LegoSet.Name)
                : query.OrderBy(us => us.LegoSet.Name)
        };

        return query;
    }

    public async Task<UserSet?> GetUserSetAsync(Guid userId, Guid legoSetId, CancellationToken cancellationToken = default) {
        return await _context.UserSets
            .AsNoTracking()
            .Include(us => us.LegoSet)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.LegoSetId == legoSetId, cancellationToken);
    }

    public async Task<UserSet?> GetUserSetBySetNumberAsync(Guid userId, string setNumber, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);
        return await _context.UserSets
            .AsNoTracking()
            .Include(us => us.LegoSet)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.LegoSet.SetNumber == normalizedSetNumber, cancellationToken);
    }

    public async Task<UserSet> AddToUserCollectionAsync(Guid userId, string setNumber, bool isWishlist = false, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);

        // Check if user already has this set
        var existingUserSet = await GetUserSetBySetNumberAsync(userId, normalizedSetNumber, cancellationToken);
        if(existingUserSet != null) {
            _logger.LogWarning("User {UserId} already has set {SetNumber} in collection", userId, normalizedSetNumber);
            throw new InvalidOperationException($"Set {normalizedSetNumber} is already in your collection");
        }

        // Get or create the shared LegoSet
        var legoSet = await GetOrCreateFromRebrickableAsync(normalizedSetNumber, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var userSet = new UserSet {
            Id = Guid.NewGuid(),
            UserId = userId,
            LegoSetId = legoSet.Id,
            Quantity = 1,
            Status = SetStatus.None,
            IsWishlist = isWishlist,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.UserSets.Add(userSet);
        await _context.SaveChangesAsync(cancellationToken);

        // Load the LegoSet for the returned object
        userSet.LegoSet = legoSet;

        _logger.LogInformation("Added set {SetNumber} to user {UserId} collection (wishlist: {IsWishlist})",
            normalizedSetNumber, userId, isWishlist);

        return userSet;
    }

    public async Task<UserSet> UpdateUserSetAsync(UserSet userSet, CancellationToken cancellationToken = default) {
        var existingUserSet = await _context.UserSets
            .FirstOrDefaultAsync(us => us.Id == userSet.Id, cancellationToken);

        if(existingUserSet == null) {
            throw new InvalidOperationException($"UserSet with ID {userSet.Id} not found");
        }

        existingUserSet.Quantity = userSet.Quantity;
        existingUserSet.Notes = userSet.Notes;
        existingUserSet.IsWishlist = userSet.IsWishlist;
        existingUserSet.Status = userSet.Status;
        existingUserSet.UpdatedAt = _timeProvider.GetUtcNow();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated UserSet {UserSetId}", existingUserSet.Id);
        return existingUserSet;
    }

    public async Task RemoveFromUserCollectionAsync(Guid userId, Guid userSetId, CancellationToken cancellationToken = default) {
        var userSet = await _context.UserSets
            .FirstOrDefaultAsync(us => us.Id == userSetId && us.UserId == userId, cancellationToken);

        if(userSet == null) {
            throw new InvalidOperationException($"UserSet with ID {userSetId} not found for user {userId}");
        }

        _context.UserSets.Remove(userSet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed UserSet {UserSetId} from user {UserId}", userSetId, userId);
    }

    // Helper methods

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
