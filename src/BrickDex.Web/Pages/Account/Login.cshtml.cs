using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Account;

public class LoginModel : PageModel {
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IWebHostEnvironment _environment;

    public LoginModel(IAuthenticationSchemeProvider schemeProvider, IWebHostEnvironment environment) {
        _schemeProvider = schemeProvider;
        _environment = environment;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public bool GoogleEnabled { get; set; }

    public bool GitHubEnabled { get; set; }

    public bool IsDevelopment { get; set; }

    public async Task<IActionResult> OnGetAsync(string? error) {
        // If already authenticated, redirect to home
        if(User.Identity?.IsAuthenticated == true) {
            return RedirectToPage("/Index");
        }

        ErrorMessage = error;
        IsDevelopment = _environment.IsDevelopment();
        await LoadEnabledProvidersAsync();
        return Page();
    }

    public IActionResult OnPostGoogle() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostGitHub() {
        var redirectUrl = Url.Page("/Account/Callback", new { ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "GitHub");
    }

    private async Task LoadEnabledProvidersAsync() {
        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var schemeNames = schemes.Select(s => s.Name).ToHashSet();

        GoogleEnabled = schemeNames.Contains(GoogleDefaults.AuthenticationScheme);
        GitHubEnabled = schemeNames.Contains("GitHub");
    }
}
