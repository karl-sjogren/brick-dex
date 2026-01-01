using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using BrickDex.Core.Services.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RebrickableSetEntity = BrickDex.Core.Models.Rebrickable.RebrickableSet;

namespace BrickDex.Core.Services;

public class UserSetService : IUserSetService {
    private readonly IBrickDexContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UserSetService> _logger;

    public UserSetService(
        IBrickDexContext context,
        TimeProvider timeProvider,
        ILogger<UserSetService> logger) {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Search operations

    public async Task<PagedResult<RebrickableSetEntity>> SearchSetsAsync(SetSearchFilters filters, CancellationToken cancellationToken = default) {
        var query = _context.RebrickableSets
            .AsNoTracking()
            .Include(s => s.Theme)
            .AsQueryable();

        // Text filter
        if(!string.IsNullOrWhiteSpace(filters.Query)) {
#pragma warning disable CA1304 // ToLower is intentional for EF Core SQL translation
            var q = filters.Query.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(q) ||
                s.SetNum.ToLower().Contains(q));
#pragma warning restore CA1304
        }

        // Year filter
        if(filters.MinYear.HasValue) {
            query = query.Where(s => s.Year >= filters.MinYear.Value);
        }

        if(filters.MaxYear.HasValue) {
            query = query.Where(s => s.Year <= filters.MaxYear.Value);
        }

        // Parts filter
        if(filters.MinParts.HasValue) {
            query = query.Where(s => s.NumParts >= filters.MinParts.Value);
        }

        if(filters.MaxParts.HasValue) {
            query = query.Where(s => s.NumParts <= filters.MaxParts.Value);
        }

        // Theme filter
        if(filters.ThemeId.HasValue) {
            query = query.Where(s => s.ThemeId == filters.ThemeId.Value);
        }

        // Sorting - parse Ordering field
        var (sortBy, sortDescending) = ParseOrdering(filters.Ordering);
        query = sortBy?.ToLowerInvariant() switch {
            "year" => sortDescending
                ? query.OrderByDescending(s => s.Year)
                : query.OrderBy(s => s.Year),
            "num_parts" => sortDescending
                ? query.OrderByDescending(s => s.NumParts)
                : query.OrderBy(s => s.NumParts),
            "name" => sortDescending
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name),
            "set_num" => sortDescending
                ? query.OrderByDescending(s => s.SetNum)
                : query.OrderBy(s => s.SetNum),
            _ => sortDescending
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RebrickableSetEntity>(items, totalCount, filters.Page, filters.PageSize);
    }

    public async Task<RebrickableSetEntity?> GetSetBySetNumberAsync(string setNumber, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);
        return await _context.RebrickableSets
            .AsNoTracking()
            .Include(s => s.Theme)
            .FirstOrDefaultAsync(s => s.SetNum == normalizedSetNumber, cancellationToken);
    }

    // User collection operations

    public async Task<PagedResult<UserSet>> GetUserSetsAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default) {
        var query = _context.UserSets
            .AsNoTracking()
            .Include(us => us.Set)
                .ThenInclude(s => s.Theme)
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
            .Include(us => us.Set)
                .ThenInclude(s => s.Theme)
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
        // Text filter
        if(!string.IsNullOrWhiteSpace(filters.Query)) {
#pragma warning disable CA1304 // ToLower is intentional for EF Core SQL translation
            var q = filters.Query.ToLower();
            query = query.Where(us =>
                us.Set.Name.ToLower().Contains(q) ||
                us.Set.SetNum.ToLower().Contains(q) ||
                (us.Set.Theme != null && us.Set.Theme.Name.ToLower().Contains(q)));
#pragma warning restore CA1304
        }

        // Sorting
        query = filters.SortBy?.ToLowerInvariant() switch {
            "year" => filters.SortDescending
                ? query.OrderByDescending(us => us.Set.Year)
                : query.OrderBy(us => us.Set.Year),
            "parts" => filters.SortDescending
                ? query.OrderByDescending(us => us.Set.NumParts)
                : query.OrderBy(us => us.Set.NumParts),
            "theme" => filters.SortDescending
                ? query.OrderByDescending(us => us.Set.Theme!.Name)
                : query.OrderBy(us => us.Set.Theme!.Name),
            "setnumber" => filters.SortDescending
                ? query.OrderByDescending(us => us.Set.SetNum)
                : query.OrderBy(us => us.Set.SetNum),
            _ => filters.SortDescending
                ? query.OrderByDescending(us => us.Set.Name)
                : query.OrderBy(us => us.Set.Name)
        };

        return query;
    }

    public async Task<UserSet?> GetUserSetAsync(Guid userId, Guid userSetId, CancellationToken cancellationToken = default) {
        return await _context.UserSets
            .AsNoTracking()
            .AsSplitQuery()
            .Include(us => us.Set)
                .ThenInclude(s => s.Theme)
            .Include(us => us.Set)
                .ThenInclude(s => s.Inventories)
                    .ThenInclude(i => i.InventoryMinifigs)
                        .ThenInclude(im => im.Minifig)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.Id == userSetId, cancellationToken);
    }

    public async Task<UserSet?> GetUserSetBySetNumberAsync(Guid userId, string setNumber, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);
        return await _context.UserSets
            .AsNoTracking()
            .Include(us => us.Set)
                .ThenInclude(s => s.Theme)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SetNumber == normalizedSetNumber, cancellationToken);
    }

    public async Task<UserSet> AddToUserCollectionAsync(Guid userId, string setNumber, bool isWishlist = false, CancellationToken cancellationToken = default) {
        var normalizedSetNumber = NormalizeSetNumber(setNumber);

        // Check if set exists in RebrickableSets
        var rebrickableSet = await _context.RebrickableSets
            .Include(s => s.Theme)
            .FirstOrDefaultAsync(s => s.SetNum == normalizedSetNumber, cancellationToken);

        if(rebrickableSet == null) {
            _logger.LogWarning("Set {SetNumber} not found in database", normalizedSetNumber);
            throw new InvalidOperationException($"Set {normalizedSetNumber} not found in catalog");
        }

        // Check if user already has this set
        var existingUserSet = await GetUserSetBySetNumberAsync(userId, normalizedSetNumber, cancellationToken);
        if(existingUserSet != null) {
            _logger.LogWarning("User {UserId} already has set {SetNumber} in collection", userId, normalizedSetNumber);
            throw new InvalidOperationException($"Set {normalizedSetNumber} is already in your collection");
        }

        var now = _timeProvider.GetUtcNow();
        var userSet = new UserSet {
            Id = Guid.NewGuid(),
            UserId = userId,
            SetNumber = normalizedSetNumber,
            Quantity = 1,
            Status = SetStatus.None,
            IsWishlist = isWishlist,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.UserSets.Add(userSet);
        await _context.SaveChangesAsync(cancellationToken);

        // Load the Set for the returned object
        userSet.Set = rebrickableSet;

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

    private static string NormalizeSetNumber(string setNumber) {
        if(!setNumber.Contains('-', StringComparison.Ordinal)) {
            return $"{setNumber}-1";
        }

        return setNumber;
    }

    private static (string? sortBy, bool sortDescending) ParseOrdering(string? ordering) {
        if(string.IsNullOrWhiteSpace(ordering)) {
            return (null, false);
        }

        var descending = ordering.StartsWith('-');
        var field = descending ? ordering[1..] : ordering;

        return (field, descending);
    }
}
