using System.Diagnostics;
using System.Security.Claims;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.ServiceDefaults;
using BrickDex.Core.Data;
using BrickDex.Web.Extensions;
using BrickDex.Web.Options;
using Microsoft.Extensions.Options;
using Serilog;
using Shorthand.Vite;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/brickdex-web.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try {
    Log.Information("Starting BrickDex web application");

    var builder = WebApplication.CreateBuilder(args);

    if(builder.Environment.IsProduction()) {
        builder.Configuration.AddAzureKeyVault(
            new Uri("https://brickdex-keyvault.vault.azure.net/"),
            new DefaultAzureCredential(),
            new AzureKeyVaultConfigurationOptions {
                ReloadInterval = TimeSpan.FromMinutes(5)
            }
        );
    }

    builder.AddServiceDefaults();

    builder.Services.AddSerilog();

    // Add database context (SQL Server via Aspire)
    builder.AddSqlServerDbContext<BrickDexContext>("brickdex");

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Add authentication
    builder.Services.AddBrickDexAuthentication(builder.Configuration);

    // Add services
    builder.Services.AddRebrickableClient(builder.Configuration);
    builder.Services.AddBrickDexServices(builder.Configuration);

    // Add webhook configuration
    builder.Services.AddOptions<WebhookOptions>()
        .Bind(builder.Configuration.GetSection(WebhookOptions.SectionName))
        .ValidateDataAnnotations();

    // Add Vite integration
    builder.Services.AddVite(options => {
        options.ManifestFileName = ".vite/manifest.json";
        options.Hostname = "localhost";
        options.Port = 5010;
        options.Https = true;
    });

    // Add Razor Pages
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if(app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();

        // Development-only endpoints for Rebrickable catalog import
        var devGroup = app.MapGroup("/dev").WithTags("Development");

        devGroup.MapPost("/import-rebrickable-catalog", async (
            IRebrickableCatalogImportService importService,
            ISearchIndex searchIndex,
            CancellationToken cancellationToken) => {
                await importService.ImportAllAsync(cancellationToken);
                await searchIndex.RebuildIndexAsync(cancellationToken);
                return Results.Ok(new { message = "Rebrickable catalog import and reindex completed successfully" });
            });

        devGroup.MapPost("/import-rebrickable-catalog/{entity}", async (
            string entity,
            IRebrickableCatalogImportService importService,
            CancellationToken cancellationToken) => {
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
            });

        // Search index management endpoints
        devGroup.MapPost("/rebuild-search-index", async (
            ISearchIndex searchIndex,
            CancellationToken cancellationToken) => {
                await searchIndex.RebuildIndexAsync(cancellationToken);
                return Results.Ok(new { message = "Search index rebuilt successfully" });
            });

        devGroup.MapGet("/search-index-stats", async (
            ISearchIndex searchIndex,
            CancellationToken cancellationToken) => {
                var stats = await searchIndex.GetStatsAsync(cancellationToken);
                return Results.Ok(stats);
            });

        devGroup.MapPost("/optimize-search-index", async (
            ISearchIndex searchIndex,
            CancellationToken cancellationToken) => {
                await searchIndex.OptimizeAsync(cancellationToken);
                return Results.Ok(new { message = "Search index optimized successfully" });
            });
    } else {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    app.MapDefaultEndpoints();

    app.MapRazorPages();

    // API endpoints
    var apiGroup = app.MapGroup("/api").RequireAuthorization();

    apiGroup.MapPost("/usersets/{id:guid}/status", async (
        Guid id,
        UpdateStatusRequest request,
        IUserSetService userSetService,
        IUserService userService,
        ClaimsPrincipal user) => {
            var currentUser = await userService.GetCurrentUserAsync(user);
            if(currentUser == null) {
                return Results.Unauthorized();
            }

            var userSet = await userSetService.GetUserSetAsync(currentUser.Id, id);
            if(userSet == null) {
                return Results.NotFound();
            }

            userSet.Status = request.Status;
            await userSetService.UpdateUserSetAsync(userSet);

            return Results.Ok(new { status = userSet.Status.ToString() });
        });

    apiGroup.MapPost("/usersets/collection", async (
        AddSetRequest request,
        IUserSetService userSetService,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken) => {
            var currentUser = await userService.GetCurrentUserAsync(user, cancellationToken);
            if(currentUser == null) {
                return Results.Unauthorized();
            }

            try {
                var userSet = await userSetService.AddToUserCollectionAsync(
                    currentUser.Id, request.SetNumber, isWishlist: false, cancellationToken);
                return Results.Ok(new AddSetResponse(userSet.Id, userSet.SetNumber, userSet.Set.Name));
            } catch(InvalidOperationException ex) {
                logger.LogWarning(ex, "Failed to add set {SetNumber} to collection", request.SetNumber);
                return Results.BadRequest(new { error = ex.Message });
            }
        });

    apiGroup.MapPost("/usersets/wishlist", async (
        AddSetRequest request,
        IUserSetService userSetService,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken) => {
            var currentUser = await userService.GetCurrentUserAsync(user, cancellationToken);
            if(currentUser == null) {
                return Results.Unauthorized();
            }

            try {
                var userSet = await userSetService.AddToUserCollectionAsync(
                    currentUser.Id, request.SetNumber, isWishlist: true, cancellationToken);
                return Results.Ok(new AddSetResponse(userSet.Id, userSet.SetNumber, userSet.Set.Name));
            } catch(InvalidOperationException ex) {
                logger.LogWarning(ex, "Failed to add set {SetNumber} to wishlist", request.SetNumber);
                return Results.BadRequest(new { error = ex.Message });
            }
        });

    // Webhook endpoints (authenticated via API key)
    var webhookGroup = app.MapGroup("/api/webhooks").WithTags("Webhooks");

    webhookGroup.MapPost("/reindex", async (
        HttpContext httpContext,
        ISearchIndex searchIndex,
        IOptions<WebhookOptions> webhookOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken) => {
            // Validate API key
            if(!httpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
                string.IsNullOrEmpty(apiKey) ||
                apiKey != webhookOptions.Value.ReindexApiKey) {
                logger.LogWarning("Unauthorized reindex webhook attempt");
                return Results.Unauthorized();
            }

            logger.LogInformation("Starting search index rebuild via webhook");
            var stopwatch = Stopwatch.StartNew();

            try {
                await searchIndex.RebuildIndexAsync(cancellationToken);
                stopwatch.Stop();

                logger.LogInformation("Search index rebuilt successfully via webhook in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return Results.Ok(new ReindexResponse(
                    Success: true,
                    Message: "Search index rebuilt successfully",
                    DurationMs: stopwatch.ElapsedMilliseconds
                ));
            } catch(Exception ex) {
                stopwatch.Stop();
                logger.LogError(ex, "Search index rebuild failed via webhook after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return Results.Problem(
                    title: "Reindex failed",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        });

    await app.RunAsync();
} catch(Exception ex) {
    Log.Fatal(ex, "Application terminated unexpectedly");
} finally {
    await Log.CloseAndFlushAsync();
}

internal record UpdateStatusRequest(SetStatus Status);
internal record AddSetRequest(string SetNumber);
internal record AddSetResponse(Guid Id, string SetNumber, string Name);
internal record ReindexResponse(bool Success, string Message, long DurationMs);
