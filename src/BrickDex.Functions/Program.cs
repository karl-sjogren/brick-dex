using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Services;
using BrickDex.Functions.Options;
using BrickDex.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

// Add configuration files
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

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

// Configure Web app webhook client
builder.Services.AddOptions<WebAppOptions>()
    .Bind(builder.Configuration.GetSection(WebAppOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddHttpClient<IReindexWebhookClient, ReindexWebhookClient>((provider, client) => {
    var options = provider.GetRequiredService<IOptions<WebAppOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
