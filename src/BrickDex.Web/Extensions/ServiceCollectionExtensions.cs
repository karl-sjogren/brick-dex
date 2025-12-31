using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Options;
using BrickDex.Core.Services;
using BrickDex.Web.Data;
using BrickDex.Web.Options;
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

    public static IServiceCollection AddBrickDexServices(this IServiceCollection services) {
        services.AddScoped<IBrickDexContext>(provider => provider.GetRequiredService<BrickDexContext>());
        services.AddScoped<ILegoSetService, LegoSetService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<ILegoThemeCache, LegoThemeCache>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IViewPreferenceService, ViewPreferenceService>();

        return services;
    }
}
