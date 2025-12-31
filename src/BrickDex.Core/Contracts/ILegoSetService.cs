using BrickDex.Core.Models;

namespace BrickDex.Core.Contracts;

public interface ILegoSetService {
    // Shared set operations
    Task<LegoSet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LegoSet?> GetBySetNumberAsync(string setNumber, CancellationToken cancellationToken = default);
    Task<LegoSet> GetOrCreateFromRebrickableAsync(string setNumber, CancellationToken cancellationToken = default);

    // User-specific operations
    Task<PagedResult<UserSet>> GetUserSetsAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default);
    Task<PagedResult<UserSet>> GetUserWishlistAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default);
    Task<UserSet?> GetUserSetAsync(Guid userId, Guid legoSetId, CancellationToken cancellationToken = default);
    Task<UserSet?> GetUserSetBySetNumberAsync(Guid userId, string setNumber, CancellationToken cancellationToken = default);
    Task<UserSet> AddToUserCollectionAsync(Guid userId, string setNumber, bool isWishlist = false, CancellationToken cancellationToken = default);
    Task<UserSet> UpdateUserSetAsync(UserSet userSet, CancellationToken cancellationToken = default);
    Task RemoveFromUserCollectionAsync(Guid userId, Guid userSetId, CancellationToken cancellationToken = default);
}
