using BrickDex.Core.Contracts;
using BrickDex.Web.Options;
using BrickDex.Web.Services;
using Microsoft.Extensions.Options;

namespace BrickDex.Web.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddRebrickableClient(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<RebrickableOptions>()
            .Bind(configuration.GetSection("Rebrickable"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<RebrickableOptions>, RebrickableOptionsValidator>();

        services.AddHttpClient<IRebrickableClient, RebrickableClient>((provider, client) => {
            var options = provider.GetRequiredService<IOptions<RebrickableOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }

    public static IServiceCollection AddBrickDexServices(this IServiceCollection services) {
        services.AddScoped<ILegoSetService, LegoSetService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
