using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Sets;

[Authorize]
public class IndexModel : PageModel {
    private readonly IUserSetService _userSetService;
    private readonly IUserService _userService;
    private readonly IViewPreferenceService _viewPreferenceService;

    private const int _pageSize = 20;

    public IndexModel(IUserSetService userSetService, IUserService userService, IViewPreferenceService viewPreferenceService) {
        _userSetService = userSetService;
        _userService = userService;
        _viewPreferenceService = viewPreferenceService;
    }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "name";

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? View { get; set; }

    public SetListViewModel? SetList { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) {
        View = _viewPreferenceService.GetViewPreference(Request, View, "list");

        if(!string.IsNullOrEmpty(Request.Query["view"])) {
            _viewPreferenceService.SetViewPreference(Response, View);
        }

        if(CurrentPage < 1) {
            CurrentPage = 1;
        }

        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Forbid();
        }

        var filters = new UserSetFilters {
            Page = CurrentPage,
            PageSize = _pageSize,
            Query = Query,
            SortBy = SortBy,
            SortDescending = SortDesc
        };

        var result = await _userSetService.GetUserSetsAsync(user.Id, filters, cancellationToken);

        SetList = new SetListViewModel {
            Sets = result.Items.Select(UserSetViewModel.FromUserSet).ToList(),
            PageName = "/Sets/Index",
            View = View,
            Query = Query,
            SortBy = SortBy,
            SortDesc = SortDesc,
            CurrentPage = CurrentPage,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            ShowActions = false
        };

        return Page();
    }
}
