using BrickDex.Core.Services.Rebrickable;

namespace BrickDex.Core.Contracts;

public interface IRebrickableClient {
    Task<RebrickableSet?> GetSetAsync(string setNumber, CancellationToken cancellationToken = default);
    Task<RebrickableSearchResult<RebrickableSet>> SearchSetsAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<RebrickableSearchResult<RebrickableTheme>> GetThemesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
}
