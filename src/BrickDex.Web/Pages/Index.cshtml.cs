using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Web.Pages;

public class IndexModel : PageModel {
    private readonly IBrickDexContext _context;
    private readonly IUserService _userService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(
        IBrickDexContext context,
        IUserService userService,
        IAuthenticationSchemeProvider schemeProvider,
        IWebHostEnvironment environment) {
        _context = context;
        _userService = userService;
        _schemeProvider = schemeProvider;
        _environment = environment;
    }

    public bool IsAuthenticated { get; set; }
    public int TotalSets { get; set; }
    public int TotalParts { get; set; }
    public int WishlistCount { get; set; }
    public IReadOnlyList<UserSet> RecentSets { get; set; } = [];
    public bool GoogleEnabled { get; set; }
    public bool GitHubEnabled { get; set; }
    public bool IsDevelopment { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        IsAuthenticated = User.Identity?.IsAuthenticated == true;

        if(!IsAuthenticated) {
            IsDevelopment = _environment.IsDevelopment();
            await LoadEnabledProvidersAsync();
            return;
        }

        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return;
        }

        TotalSets = await _context.UserSets
            .Where(us => us.UserId == user.Id && !us.IsWishlist)
            .CountAsync(cancellationToken);

        TotalParts = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist)
            .SumAsync(us => us.Set.NumParts * us.Quantity, cancellationToken);

        WishlistCount = await _context.UserSets
            .Where(us => us.UserId == user.Id && us.IsWishlist)
            .CountAsync(cancellationToken);

        RecentSets = await _context.UserSets
            .Include(us => us.Set)
                .ThenInclude(s => s.Theme)
            .Where(us => us.UserId == user.Id && !us.IsWishlist)
            .OrderByDescending(us => us.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);
    }

    public IActionResult OnPostGoogle() {
        var redirectUrl = Url.Page("/Account/Callback");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostGitHub() {
        var redirectUrl = Url.Page("/Account/Callback");
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
