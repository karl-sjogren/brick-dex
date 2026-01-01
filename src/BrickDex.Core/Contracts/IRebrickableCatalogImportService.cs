namespace BrickDex.Core.Contracts;

public interface IRebrickableCatalogImportService {
    Task ImportAllAsync(CancellationToken cancellationToken = default);
    Task ImportThemesAsync(CancellationToken cancellationToken = default);
    Task ImportSetsAsync(CancellationToken cancellationToken = default);
    Task ImportMinifigsAsync(CancellationToken cancellationToken = default);
    Task ImportInventoriesAsync(CancellationToken cancellationToken = default);
    Task ImportInventorySetsAsync(CancellationToken cancellationToken = default);
    Task ImportInventoryMinifigsAsync(CancellationToken cancellationToken = default);
}
