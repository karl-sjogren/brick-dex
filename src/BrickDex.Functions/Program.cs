using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Services;
using BrickDex.Web.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

if(builder.Environment.IsProduction()) {
    builder.Configuration.AddAzureKeyVault(
        new Uri("https://brickdex-keyvault.vault.azure.net/"),
        new DefaultAzureCredential(),
        new AzureKeyVaultConfigurationOptions {
            ReloadInterval = TimeSpan.FromMinutes(5)
        }
    );
}

builder.ConfigureFunctionsWebApplication();

// Add database context (SQL Server via Aspire)
builder.AddSqlServerDbContext<BrickDexContext>("brickdex");

// Register services
builder.Services.AddScoped<IBrickDexContext>(provider => provider.GetRequiredService<BrickDexContext>());
builder.Services.AddHttpClient<IRebrickableCatalogImportService, RebrickableCatalogImportService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
