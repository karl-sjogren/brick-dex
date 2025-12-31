using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace BrickDex.Web.Extensions;

public static class AuthenticationExtensions {
    public static IServiceCollection AddBrickDexAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) {
        var authBuilder = services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options => {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "BrickDex.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
        });

        // Only add Google if credentials are configured
        var googleConfig = configuration.GetSection("Authentication:Google");
        if(IsProviderConfigured(googleConfig)) {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options => {
                options.ClientId = googleConfig["ClientId"]!;
                options.ClientSecret = googleConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
            });
        }

        // Only add GitHub if credentials are configured
        var githubConfig = configuration.GetSection("Authentication:GitHub");
        if(IsProviderConfigured(githubConfig)) {
            authBuilder.AddGitHub(options => {
                options.ClientId = githubConfig["ClientId"]!;
                options.ClientSecret = githubConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-github";
                options.Scope.Add("user:email");
            });
        }

        return services;
    }

    private static bool IsProviderConfigured(IConfigurationSection config) {
        var clientId = config["ClientId"];
        var clientSecret = config["ClientSecret"];
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }
}
