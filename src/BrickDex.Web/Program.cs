using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using BrickDex.Core.Data;
using BrickDex.ServiceDefaults;
using BrickDex.Web.Endpoints;
using BrickDex.Web.Extensions;
using BrickDex.Web.Options;
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

        // Development-only endpoints
        app.MapDevelopmentEndpoints();
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
    app.MapUserSetEndpoints();
    app.MapWebhookEndpoints();

    await app.RunAsync();
} catch(Exception ex) {
    Log.Fatal(ex, "Application terminated unexpectedly");
} finally {
    await Log.CloseAndFlushAsync();
}
