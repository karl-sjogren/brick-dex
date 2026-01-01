using BrickDex.Core.Models;
using BrickDex.Core.Services.Rebrickable;

namespace BrickDex.Core.Contracts;

public interface IUserSetService {
    // Search RebrickableSets in database
    Task<PagedResult<Models.Rebrickable.RebrickableSet>> SearchSetsAsync(SetSearchFilters filters, CancellationToken cancellationToken = default);
    Task<Models.Rebrickable.RebrickableSet?> GetSetBySetNumberAsync(string setNumber, CancellationToken cancellationToken = default);

    // User collection operations
    Task<PagedResult<UserSet>> GetUserSetsAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default);
    Task<PagedResult<UserSet>> GetUserWishlistAsync(Guid userId, UserSetFilters filters, CancellationToken cancellationToken = default);
    Task<UserSet?> GetUserSetAsync(Guid userId, Guid userSetId, CancellationToken cancellationToken = default);
    Task<UserSet?> GetUserSetBySetNumberAsync(Guid userId, string setNumber, CancellationToken cancellationToken = default);
    Task<UserSet> AddToUserCollectionAsync(Guid userId, string setNumber, bool isWishlist = false, CancellationToken cancellationToken = default);
    Task<UserSet> UpdateUserSetAsync(UserSet userSet, CancellationToken cancellationToken = default);
    Task RemoveFromUserCollectionAsync(Guid userId, Guid userSetId, CancellationToken cancellationToken = default);
}
