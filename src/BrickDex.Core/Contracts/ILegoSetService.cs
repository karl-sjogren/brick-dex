using BrickDex.Core.Models;

namespace BrickDex.Core.Contracts;

public interface ILegoSetService {
    Task<IReadOnlyList<LegoSet>> GetAllAsync(bool includeWishlist = false, CancellationToken cancellationToken = default);
    Task<LegoSet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LegoSet?> GetBySetNumberAsync(string setNumber, CancellationToken cancellationToken = default);
    Task<LegoSet> AddAsync(LegoSet legoSet, CancellationToken cancellationToken = default);
    Task<LegoSet> AddFromRebrickableAsync(string setNumber, bool isWishlist = false, CancellationToken cancellationToken = default);
    Task<LegoSet> UpdateAsync(LegoSet legoSet, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
