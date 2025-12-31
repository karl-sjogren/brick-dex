using BrickDex.Core.Services.Rebrickable;

namespace BrickDex.Core.Contracts;

public interface ILegoThemeCache {
    Task<IReadOnlyList<RebrickableTheme>> GetThemesAsync(CancellationToken cancellationToken = default);
    void InvalidateCache();
}
