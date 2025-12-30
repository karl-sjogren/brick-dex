using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using BrickDex.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Web.Pages.Sets;

public class WishlistModel : PageModel {
    private readonly BrickDexContext _context;
    private readonly ILegoSetService _legoSetService;
    private readonly TimeProvider _timeProvider;

    public WishlistModel(BrickDexContext context, ILegoSetService legoSetService, TimeProvider timeProvider) {
        _context = context;
        _legoSetService = legoSetService;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<LegoSet> Sets { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        Sets = await _context.LegoSets
            .Where(s => s.IsWishlist)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostMoveToCollectionAsync(Guid id, CancellationToken cancellationToken) {
        var set = await _context.LegoSets.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if(set != null) {
            set.IsWishlist = false;
            set.UpdatedAt = _timeProvider.GetUtcNow();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken) {
        await _legoSetService.DeleteAsync(id, cancellationToken);
        return RedirectToPage();
    }
}
