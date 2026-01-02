using BrickDex.Core.Contracts;
using BrickDex.Core.Services.Rebrickable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RebrickableSet = BrickDex.Core.Models.Rebrickable.RebrickableSet;
using RebrickableTheme = BrickDex.Core.Models.Rebrickable.RebrickableTheme;

namespace BrickDex.Web.Pages.Sets;

[Authorize]
public class SearchModel : PageModel {
    private readonly IUserSetService _userSetService;
    private readonly ILegoThemeCache _themeCache;

    private const int _pageSize = 20;

    public SearchModel(
        IUserSetService userSetService,
        ILegoThemeCache themeCache) {
        _userSetService = userSetService;
        _themeCache = themeCache;
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

        var result = await _userSetService.SearchSetsAsync(filters, cancellationToken);
        SearchResults = result.Items.ToList();
        TotalCount = result.TotalCount;
    }

    private async Task LoadThemesAsync(CancellationToken cancellationToken) {
        Themes = await _themeCache.GetThemesAsync(cancellationToken);
    }
}
