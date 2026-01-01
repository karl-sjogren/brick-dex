using System.Security.Claims;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.ServiceDefaults;
using BrickDex.Web.Data;
using BrickDex.Web.Extensions;
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
    builder.Services.AddBrickDexServices();

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
            CancellationToken cancellationToken) => {
                await importService.ImportAllAsync(cancellationToken);
                return Results.Ok(new { message = "Rebrickable catalog import completed successfully" });
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

    await app.RunAsync();
} catch(Exception ex) {
    Log.Fatal(ex, "Application terminated unexpectedly");
} finally {
    await Log.CloseAndFlushAsync();
}

internal record UpdateStatusRequest(SetStatus Status);
