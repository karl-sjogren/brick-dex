using BrickDex.Core.Contracts;
using BrickDex.Core.Services.Rebrickable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Sets;

public class SearchModel : PageModel {
    private readonly IRebrickableClient _rebrickableClient;
    private readonly ILegoSetService _legoSetService;
    private readonly ILogger<SearchModel> _logger;

    private const int _pageSize = 20;

    public SearchModel(IRebrickableClient rebrickableClient, ILegoSetService legoSetService, ILogger<SearchModel> logger) {
        _rebrickableClient = rebrickableClient;
        _legoSetService = legoSetService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<RebrickableSet> SearchResults { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / _pageSize) : 0;

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        if(CurrentPage < 1) {
            CurrentPage = 1;
        }

        if(!string.IsNullOrWhiteSpace(Query)) {
            var result = await _rebrickableClient.SearchSetsAsync(Query, CurrentPage, _pageSize, cancellationToken);
            SearchResults = result.Results;
            TotalCount = result.Count;
        }
    }

    public async Task<IActionResult> OnPostAddToCollectionAsync(string setNumber, string? query, int page, CancellationToken cancellationToken) {
        return await AddSetAsync(setNumber, query, page, isWishlist: false, cancellationToken);
    }

    public async Task<IActionResult> OnPostAddToWishlistAsync(string setNumber, string? query, int page, CancellationToken cancellationToken) {
        return await AddSetAsync(setNumber, query, page, isWishlist: true, cancellationToken);
    }

    private async Task<IActionResult> AddSetAsync(string setNumber, string? query, int page, bool isWishlist, CancellationToken cancellationToken) {
        Query = query;
        CurrentPage = page < 1 ? 1 : page;

        try {
            await _legoSetService.AddFromRebrickableAsync(setNumber, isWishlist, cancellationToken);
            Message = $"Set {setNumber} added to {(isWishlist ? "wishlist" : "collection")}!";
        } catch(InvalidOperationException ex) {
            _logger.LogWarning(ex, "Failed to add set {SetNumber}", setNumber);
            IsError = true;
            Message = ex.Message;
        }

        // Re-run the search to show results again
        if(!string.IsNullOrWhiteSpace(Query)) {
            var result = await _rebrickableClient.SearchSetsAsync(Query, CurrentPage, _pageSize, cancellationToken);
            SearchResults = result.Results;
            TotalCount = result.Count;
        }

        return Page();
    }
}
