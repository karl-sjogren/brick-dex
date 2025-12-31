using System.Security.Claims;
using BrickDex.Core.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Account;

public class CallbackModel : PageModel {
    private readonly IUserService _userService;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(IUserService userService, ILogger<CallbackModel> logger) {
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl, CancellationToken cancellationToken) {
        var authenticateResult = await HttpContext.AuthenticateAsync();

        if(!authenticateResult.Succeeded) {
            _logger.LogWarning("Authentication failed: {Failure}", authenticateResult.Failure?.Message);
            return RedirectToPage("/Account/Login", new { error = "Authentication failed. Please try again." });
        }

        var externalPrincipal = authenticateResult.Principal!;
        var provider = authenticateResult.Properties?.Items[".AuthScheme"]
            ?? externalPrincipal.Identity?.AuthenticationType
            ?? "Unknown";

        var providerKey = externalPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = externalPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var name = externalPrincipal.FindFirst(ClaimTypes.Name)?.Value;
        var picture = externalPrincipal.FindFirst("urn:google:picture")?.Value
            ?? externalPrincipal.FindFirst("urn:github:avatar_url")?.Value;

        if(string.IsNullOrEmpty(providerKey) || string.IsNullOrEmpty(email)) {
            _logger.LogWarning("Could not retrieve user information from {Provider}", provider);
            return RedirectToPage("/Account/Login", new { error = "Could not retrieve user information from the provider." });
        }

        var user = await _userService.GetOrCreateFromExternalLoginAsync(
            provider, providerKey, email, name, picture, cancellationToken);

        if(user == null) {
            _logger.LogError("Could not create user account for {Email}", email);
            return RedirectToPage("/Account/Login", new { error = "Could not create user account. Please try again." });
        }

        // Create application claims
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email)
        };

        if(!string.IsNullOrEmpty(user.AvatarUrl)) {
            claims.Add(new Claim("avatar_url", user.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _logger.LogInformation("User {Email} signed in via {Provider}", email, provider);

        return LocalRedirect(returnUrl ?? "/");
    }
}
