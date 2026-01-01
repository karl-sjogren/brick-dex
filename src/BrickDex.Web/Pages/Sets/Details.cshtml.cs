using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Sets;

[Authorize]
public class DetailsModel : PageModel {
    private readonly IUserSetService _userSetService;
    private readonly IUserService _userService;

    public DetailsModel(IUserSetService userSetService, IUserService userService) {
        _userSetService = userSetService;
        _userService = userService;
    }

    [BindProperty]
    public UserSet? UserSet { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Unauthorized();
        }

        UserSet = await _userSetService.GetUserSetAsync(user.Id, id, cancellationToken);
        if(UserSet == null) {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Unauthorized();
        }

        if(UserSet == null) {
            return NotFound();
        }

        await _userSetService.UpdateUserSetAsync(UserSet, cancellationToken);
        return RedirectToPage("/Sets/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Unauthorized();
        }

        await _userSetService.RemoveFromUserCollectionAsync(user.Id, id, cancellationToken);
        return RedirectToPage("/Sets/Index");
    }
}
