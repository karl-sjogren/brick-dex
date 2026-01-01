using BrickDex.Core.Contracts;
using BrickDex.Core.Services.Rebrickable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Sets;

[Authorize]
public class SearchModel : PageModel {
    private readonly IRebrickableClient _rebrickableClient;
    private readonly ILegoSetService _legoSetService;
    private readonly IUserService _userService;
    private readonly ILegoThemeCache _themeCache;
    private readonly ILogger<SearchModel> _logger;

    private const int _pageSize = 20;

    public SearchModel(
        IRebrickableClient rebrickableClient,
        ILegoSetService legoSetService,
        IUserService userService,
        ILegoThemeCache themeCache,
        ILogger<SearchModel> logger) {
        _rebrickableClient = rebrickableClient;
        _legoSetService = legoSetService;
        _userService = userService;
        _themeCache = themeCache;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? MinYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MinParts { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxParts { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ThemeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Ordering { get; set; }

    public IReadOnlyList<RebrickableSet> SearchResults { get; set; } = [];
    public IReadOnlyList<RebrickableTheme> Themes { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / _pageSize) : 0;

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public bool HasActiveFilters =>
        MinYear.HasValue || MaxYear.HasValue ||
        MinParts.HasValue || MaxParts.HasValue ||
        ThemeId.HasValue || !string.IsNullOrWhiteSpace(Ordering);

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        if(CurrentPage < 1) {
            CurrentPage = 1;
        }

        await LoadThemesAsync(cancellationToken);

        if(!string.IsNullOrWhiteSpace(Query) || HasActiveFilters) {
            await ExecuteSearchAsync(cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAddToCollectionAsync(
        string setNumber,
        string? query,
        int page,
        int? minYear,
        int? maxYear,
        int? minParts,
        int? maxParts,
        int? themeId,
        string? ordering,
        CancellationToken cancellationToken) {
        return await AddSetAsync(setNumber, query, page, minYear, maxYear, minParts, maxParts, themeId, ordering, isWishlist: false, cancellationToken);
    }

    public async Task<IActionResult> OnPostAddToWishlistAsync(
        string setNumber,
        string? query,
        int page,
        int? minYear,
        int? maxYear,
        int? minParts,
        int? maxParts,
        int? themeId,
        string? ordering,
        CancellationToken cancellationToken) {
        return await AddSetAsync(setNumber, query, page, minYear, maxYear, minParts, maxParts, themeId, ordering, isWishlist: true, cancellationToken);
    }

    private async Task<IActionResult> AddSetAsync(
        string setNumber,
        string? query,
        int page,
        int? minYear,
        int? maxYear,
        int? minParts,
        int? maxParts,
        int? themeId,
        string? ordering,
        bool isWishlist,
        CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Unauthorized();
        }

        // Restore filter state
        Query = query;
        CurrentPage = page < 1 ? 1 : page;
        MinYear = minYear;
        MaxYear = maxYear;
        MinParts = minParts;
        MaxParts = maxParts;
        ThemeId = themeId;
        Ordering = ordering;

        try {
            await _legoSetService.AddToUserCollectionAsync(user.Id, setNumber, isWishlist, cancellationToken);
            Message = $"Set {setNumber} added to {(isWishlist ? "wishlist" : "collection")}!";
        } catch(InvalidOperationException ex) {
            _logger.LogWarning(ex, "Failed to add set {SetNumber}", setNumber);
            IsError = true;
            Message = ex.Message;
        }

        await LoadThemesAsync(cancellationToken);

        // Re-run the search to show results again
        if(!string.IsNullOrWhiteSpace(Query) || HasActiveFilters) {
            await ExecuteSearchAsync(cancellationToken);
        }

        return Page();
    }

    private async Task ExecuteSearchAsync(CancellationToken cancellationToken) {
        var filters = new SetSearchFilters {
            Query = Query,
            Page = CurrentPage,
            PageSize = _pageSize,
            MinYear = MinYear,
            MaxYear = MaxYear,
            MinParts = MinParts,
            MaxParts = MaxParts,
            ThemeId = ThemeId,
            Ordering = Ordering
        };

        var result = await _rebrickableClient.SearchSetsAsync(filters, cancellationToken);
        SearchResults = result.Results;
        TotalCount = result.Count;
    }

    private async Task LoadThemesAsync(CancellationToken cancellationToken) {
        Themes = await _themeCache.GetThemesAsync(cancellationToken);
    }
}
