using BrickDex.Core.Models.Rebrickable;

namespace BrickDex.Core.Contracts;

public interface ILegoThemeCache {
    Task<IReadOnlyList<RebrickableTheme>> GetThemesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThemeDisplayItem>> GetThemesForDisplayAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetThemeAndDescendantIdsAsync(int themeId, CancellationToken cancellationToken = default);
    void InvalidateCache();
}
