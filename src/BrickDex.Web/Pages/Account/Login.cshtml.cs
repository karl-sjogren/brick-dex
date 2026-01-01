using AspNet.Security.OAuth.Apple;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Twitch;
using BrickDex.Core.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Account;

public class LoginModel : PageModel {
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly IInviteService _inviteService;

    public LoginModel(
        IAuthenticationSchemeProvider schemeProvider,
        IWebHostEnvironment environment,
        IInviteService inviteService) {
        _schemeProvider = schemeProvider;
        _environment = environment;
        _inviteService = inviteService;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Invite { get; set; }

    public string? ErrorMessage { get; set; }

    public bool GoogleEnabled { get; set; }

    public bool GitHubEnabled { get; set; }

    public bool FacebookEnabled { get; set; }

    public bool MicrosoftEnabled { get; set; }

    public bool TwitchEnabled { get; set; }

    public bool DiscordEnabled { get; set; }

    public bool AppleEnabled { get; set; }

    public bool IsDevelopment { get; set; }

    /// <summary>
    /// True if this is the first user (no invite needed).
    /// </summary>
    public bool IsFirstUser { get; set; }

    /// <summary>
    /// True if a valid invite code was provided.
    /// </summary>
    public bool HasValidInvite { get; set; }

    public async Task<IActionResult> OnGetAsync(string? error, CancellationToken cancellationToken) {
        // If already authenticated, redirect to home
        if(User.Identity?.IsAuthenticated == true) {
            return RedirectToPage("/Index");
        }

        ErrorMessage = error;
        IsDevelopment = _environment.IsDevelopment();
        await LoadEnabledProvidersAsync();

        // Check if registration is allowed
        IsFirstUser = !await _inviteService.AnyUsersExistAsync(cancellationToken);
        HasValidInvite = !string.IsNullOrEmpty(Invite) &&
            await _inviteService.GetValidInviteAsync(Invite, cancellationToken) != null;

        return Page();
    }

    public IActionResult OnPostGoogle() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        // Store invite code in auth properties
        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostGitHub() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        // Store invite code in auth properties
        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, "GitHub");
    }

    public IActionResult OnPostFacebook() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostMicrosoft() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostTwitch() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, TwitchAuthenticationDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostDiscord() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, DiscordAuthenticationDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostApple() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        if(!string.IsNullOrEmpty(Invite)) {
            properties.Items["invite"] = Invite;
        }

        return Challenge(properties, AppleAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task LoadEnabledProvidersAsync() {
        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var schemeNames = schemes.Select(s => s.Name).ToHashSet();

        GoogleEnabled = schemeNames.Contains(GoogleDefaults.AuthenticationScheme);
        GitHubEnabled = schemeNames.Contains("GitHub");
        FacebookEnabled = schemeNames.Contains(FacebookDefaults.AuthenticationScheme);
        MicrosoftEnabled = schemeNames.Contains(MicrosoftAccountDefaults.AuthenticationScheme);
        TwitchEnabled = schemeNames.Contains(TwitchAuthenticationDefaults.AuthenticationScheme);
        DiscordEnabled = schemeNames.Contains(DiscordAuthenticationDefaults.AuthenticationScheme);
        AppleEnabled = schemeNames.Contains(AppleAuthenticationDefaults.AuthenticationScheme);
    }
}
