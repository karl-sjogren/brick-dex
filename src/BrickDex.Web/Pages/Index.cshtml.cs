using BrickDex.Core.Models;
using BrickDex.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Web.Pages;

public class IndexModel : PageModel {
    private readonly BrickDexContext _context;

    public IndexModel(BrickDexContext context) {
        _context = context;
    }

    public int TotalSets { get; set; }
    public int TotalParts { get; set; }
    public int WishlistCount { get; set; }
    public IReadOnlyList<LegoSet> RecentSets { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        TotalSets = await _context.LegoSets
            .Where(s => !s.IsWishlist)
            .CountAsync(cancellationToken);

        TotalParts = await _context.LegoSets
            .Where(s => !s.IsWishlist)
            .SumAsync(s => s.NumParts * s.Quantity, cancellationToken);

        WishlistCount = await _context.LegoSets
            .Where(s => s.IsWishlist)
            .CountAsync(cancellationToken);

        RecentSets = await _context.LegoSets
            .Where(s => !s.IsWishlist)
            .OrderByDescending(s => s.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);
    }
}
