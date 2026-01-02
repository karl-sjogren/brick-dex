using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Options;
using BrickDex.Core.Services;
using BrickDex.Lucene.Extensions;
using BrickDex.Web.Options;
using BrickDex.Web.Services;
using Microsoft.Extensions.Options;

namespace BrickDex.Web.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddRebrickableClient(this IServiceCollection services, IConfiguration configuration) {
        services.AddSingleton<IValidateOptions<RebrickableOptions>, RebrickableOptionsValidator>();

        services.AddOptions<RebrickableOptions>()
            .Bind(configuration.GetSection("Rebrickable"))
            .ValidateOnStart();

        services.AddHttpClient<IRebrickableClient, RebrickableClient>((provider, client) => {
            var options = provider.GetRequiredService<IOptions<RebrickableOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }

    public static IServiceCollection AddBrickDexServices(this IServiceCollection services, IConfiguration configuration) {
        services.AddScoped<IBrickDexContext>(provider => provider.GetRequiredService<BrickDexContext>());
        services.AddScoped<IUserSetService, UserSetService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddSingleton<ILegoThemeCache, LegoThemeCache>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IViewPreferenceService, ViewPreferenceService>();
        services.AddHttpClient<IRebrickableCatalogImportService, RebrickableCatalogImportService>();
        services.AddHostedService<MigrationHostedService>();

        // Add Lucene search services
        services.AddLuceneSearch(configuration);

        return services;
    }
}
