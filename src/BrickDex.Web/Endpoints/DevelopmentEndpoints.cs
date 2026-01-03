using BrickDex.Core.Contracts;

namespace BrickDex.Web.Endpoints;

public static class DevelopmentEndpoints {
    public static WebApplication MapDevelopmentEndpoints(this WebApplication app) {
        var group = app.MapGroup("/dev").WithTags("Development");

        group.MapPost("/import-rebrickable-catalog", ImportAllAsync);
        group.MapPost("/import-rebrickable-catalog/{entity}", ImportEntityAsync);
        group.MapPost("/rebuild-search-index", RebuildSearchIndexAsync);
        group.MapGet("/search-index-stats", GetSearchIndexStatsAsync);
        group.MapPost("/optimize-search-index", OptimizeSearchIndexAsync);

        return app;
    }

    private static async Task<IResult> ImportAllAsync(
        IRebrickableCatalogImportService importService,
        ISearchIndex searchIndex,
        CancellationToken cancellationToken) {
        await importService.ImportAllAsync(cancellationToken);
        await searchIndex.RebuildIndexAsync(cancellationToken);
        return Results.Ok(new { message = "Rebrickable catalog import and reindex completed successfully" });
    }

    private static async Task<IResult> ImportEntityAsync(
        string entity,
        IRebrickableCatalogImportService importService,
        CancellationToken cancellationToken) {
        switch(entity.ToLowerInvariant()) {
            case "themes":
                await importService.ImportThemesAsync(cancellationToken);
                break;
            case "sets":
                await importService.ImportSetsAsync(cancellationToken);
                break;
            case "minifigs":
                await importService.ImportMinifigsAsync(cancellationToken);
                break;
            case "inventories":
                await importService.ImportInventoriesAsync(cancellationToken);
                break;
            case "inventory-sets":
                await importService.ImportInventorySetsAsync(cancellationToken);
                break;
            case "inventory-minifigs":
                await importService.ImportInventoryMinifigsAsync(cancellationToken);
                break;
            default:
                return Results.BadRequest(new {
                    error = $"Unknown entity: {entity}",
                    validEntities = new[] { "themes", "sets", "minifigs", "inventories", "inventory-sets", "inventory-minifigs" }
                });
        }

        return Results.Ok(new { message = $"Rebrickable {entity} import completed successfully" });
    }

    private static async Task<IResult> RebuildSearchIndexAsync(
        ISearchIndex searchIndex,
        CancellationToken cancellationToken) {
        await searchIndex.RebuildIndexAsync(cancellationToken);
        return Results.Ok(new { message = "Search index rebuilt successfully" });
    }

    private static async Task<IResult> GetSearchIndexStatsAsync(
        ISearchIndex searchIndex,
        CancellationToken cancellationToken) {
        var stats = await searchIndex.GetStatsAsync(cancellationToken);
        return Results.Ok(stats);
    }

    private static async Task<IResult> OptimizeSearchIndexAsync(
        ISearchIndex searchIndex,
        CancellationToken cancellationToken) {
        await searchIndex.OptimizeAsync(cancellationToken);
        return Results.Ok(new { message = "Search index optimized successfully" });
    }
}
