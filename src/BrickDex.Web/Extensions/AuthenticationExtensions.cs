using AspNet.Security.OAuth.Apple;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Twitch;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.FileProviders;

namespace BrickDex.Web.Extensions;

public static class AuthenticationExtensions {
    public const string ExternalScheme = "BrickDex.External";

    public static IServiceCollection AddBrickDexAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) {
        var authBuilder = services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = ExternalScheme;
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
        })
        .AddCookie(ExternalScheme, options => {
            options.Cookie.Name = "BrickDex.External";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });

        // Google
        var googleConfig = configuration.GetSection("Authentication:Google");
        if(IsProviderConfigured(googleConfig)) {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options => {
                options.ClientId = googleConfig["ClientId"]!;
                options.ClientSecret = googleConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
            });
        }

        // GitHub
        var githubConfig = configuration.GetSection("Authentication:GitHub");
        if(IsProviderConfigured(githubConfig)) {
            authBuilder.AddGitHub(options => {
                options.ClientId = githubConfig["ClientId"]!;
                options.ClientSecret = githubConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-github";
                options.Scope.Add("user:email");
            });
        }

        // Facebook
        var facebookConfig = configuration.GetSection("Authentication:Facebook");
        if(IsProviderConfigured(facebookConfig)) {
            authBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, options => {
                options.AppId = facebookConfig["ClientId"]!;
                options.AppSecret = facebookConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-facebook";
                options.Fields.Add("email");
                options.Fields.Add("name");
                options.Fields.Add("picture");
            });
        }

        // Microsoft
        var microsoftConfig = configuration.GetSection("Authentication:Microsoft");
        if(IsProviderConfigured(microsoftConfig)) {
            authBuilder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options => {
                options.ClientId = microsoftConfig["ClientId"]!;
                options.ClientSecret = microsoftConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-microsoft";
            });
        }

        // Twitch
        var twitchConfig = configuration.GetSection("Authentication:Twitch");
        if(IsProviderConfigured(twitchConfig)) {
            authBuilder.AddTwitch(TwitchAuthenticationDefaults.AuthenticationScheme, options => {
                options.ClientId = twitchConfig["ClientId"]!;
                options.ClientSecret = twitchConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-twitch";
                options.Scope.Add("user:read:email");
            });
        }

        // Discord
        var discordConfig = configuration.GetSection("Authentication:Discord");
        if(IsProviderConfigured(discordConfig)) {
            authBuilder.AddDiscord(DiscordAuthenticationDefaults.AuthenticationScheme, options => {
                options.ClientId = discordConfig["ClientId"]!;
                options.ClientSecret = discordConfig["ClientSecret"]!;
                options.CallbackPath = "/signin-discord";
                options.Scope.Add("email");
            });
        }

        // Apple
        var appleConfig = configuration.GetSection("Authentication:Apple");
        if(IsAppleConfigured(appleConfig)) {
            authBuilder.AddApple(AppleAuthenticationDefaults.AuthenticationScheme, options => {
                options.ClientId = appleConfig["ClientId"]!;
                options.TeamId = appleConfig["TeamId"]!;
                options.KeyId = appleConfig["KeyId"]!;
                options.CallbackPath = "/signin-apple";
                // Apple requires a private key file or inline key
                var keyPath = appleConfig["PrivateKeyPath"];
                if(!string.IsNullOrWhiteSpace(keyPath)) {
#pragma warning disable IO0006 // Use IFileSystem.Path - not practical in static startup config
                    options.UsePrivateKey(keyId => {
                        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                        var filePath = isDevelopment ? keyPath : $"/secrets/{keyId}.p8";
                        var directory = Path.GetDirectoryName(filePath) ?? ".";
                        var fileName = Path.GetFileName(filePath);
                        var provider = new PhysicalFileProvider(directory);
                        return provider.GetFileInfo(fileName);
                    });
#pragma warning restore IO0006
                }
            });
        }

        return services;
    }

    private static bool IsProviderConfigured(IConfigurationSection config) {
        var clientId = config["ClientId"];
        var clientSecret = config["ClientSecret"];
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    private static bool IsAppleConfigured(IConfigurationSection config) {
        var clientId = config["ClientId"];
        var teamId = config["TeamId"];
        var keyId = config["KeyId"];
        return !string.IsNullOrWhiteSpace(clientId) &&
               !string.IsNullOrWhiteSpace(teamId) &&
               !string.IsNullOrWhiteSpace(keyId);
    }
}
