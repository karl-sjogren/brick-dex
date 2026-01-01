using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Account;

[Authorize]
public class InvitesModel : PageModel {
    private readonly IInviteService _inviteService;
    private readonly IUserService _userService;
    private readonly TimeProvider _timeProvider;

    public InvitesModel(
        IInviteService inviteService,
        IUserService userService,
        TimeProvider timeProvider) {
        _inviteService = inviteService;
        _userService = userService;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<Invite> Invites { get; private set; } = [];
    public string? NewInviteUrl { get; private set; }
    public string? SuccessMessage { get; set; }
    public DateTimeOffset Now { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        string? created,
        CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Forbid();
        }

        Now = _timeProvider.GetUtcNow();
        Invites = await _inviteService.GetInvitesByUserAsync(user.Id, cancellationToken);

        if(!string.IsNullOrEmpty(created)) {
            NewInviteUrl = Url.Page(
                "/Account/Login",
                null,
                new { invite = created },
                Request.Scheme);
            SuccessMessage = "Invite created! Share this link:";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return Forbid();
        }

        var invite = await _inviteService.CreateInviteAsync(user.Id, cancellationToken);

        return RedirectToPage(new { created = invite.Code });
    }
}
