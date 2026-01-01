using System.Security.Claims;
using BrickDex.Core.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Account;

public class DevLoginModel : PageModel {
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DevLoginModel> _logger;

    private const string _devUserEmail = "dev@brickdex.local";
    private const string _devUserName = "Development User";
    private const string _devProvider = "Development";
    private const string _devProviderKey = "dev-user-001";

    private const string _inviteRequiredMessage =
        "You need an invite to create an account on BrickDex, " +
        "ask a friend with access if they can invite you.";

    public DevLoginModel(
        IUserService userService,
        IWebHostEnvironment environment,
        ILogger<DevLoginModel> logger) {
        _userService = userService;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(
        string? returnUrl,
        string? invite,
        CancellationToken cancellationToken) {
        if(!_environment.IsDevelopment()) {
            _logger.LogWarning("DevLogin attempted in non-development environment");
            return NotFound();
        }

        var (user, requiresInvite) = await _userService.GetOrCreateFromExternalLoginAsync(
            _devProvider,
            _devProviderKey,
            _devUserEmail,
            _devUserName,
            avatarUrl: null,
            invite,
            cancellationToken);

        if(requiresInvite) {
            _logger.LogWarning("Dev user registration blocked: invite required");
            return RedirectToPage("/Account/Login", new { error = _inviteRequiredMessage });
        }

        if(user == null) {
            _logger.LogError("Could not create development user");
            return RedirectToPage("/Account/Login", new {
                error = "Could not create development user."
            });
        }

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        _logger.LogInformation("Development user signed in");

        return LocalRedirect(returnUrl ?? "/");
    }
}
