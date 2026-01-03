using BrickDex.Core.Contracts;
using BrickDex.Core.Data;
using BrickDex.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BrickDex.Web.Pages;

[Authorize]
public class StatisticsModel : PageModel {
    private readonly IBrickDexContext _context;
    private readonly IUserService _userService;

    public StatisticsModel(IBrickDexContext context, IUserService userService) {
        _context = context;
        _userService = userService;
    }

    // Basic stats
    public int TotalSets { get; set; }
    public int TotalParts { get; set; }
    public int TotalMinifigs { get; set; }
    public int WishlistCount { get; set; }

    // Pie chart data: Sets
    public int SetsBuilt { get; set; }
    public int SetsBuilding { get; set; }
    public int SetsInStorage { get; set; }

    // Pie chart data: Pieces
    public int PiecesBuilt { get; set; }
    public int PiecesBuilding { get; set; }
    public int PiecesInStorage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        var user = await _userService.GetCurrentUserAsync(User, cancellationToken);
        if(user == null) {
            return;
        }

        // Basic stats (same as Index page)
        TotalSets = await _context.UserSets
            .Where(us => us.UserId == user.Id && !us.IsWishlist)
            .CountAsync(cancellationToken);

        TotalParts = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist)
            .SumAsync(us => us.Set.NumParts * us.Quantity, cancellationToken);

        TotalMinifigs = await _context.UserSets
            .Where(us => us.UserId == user.Id && !us.IsWishlist)
            .Join(
                _context.RebrickableInventories,
                us => us.SetNumber,
                inv => inv.SetNum,
                (us, inv) => new { us.Quantity, inv.Id })
            .Join(
                _context.RebrickableInventoryMinifigs,
                x => x.Id,
                im => im.InventoryId,
                (x, im) => x.Quantity * im.Quantity)
            .SumAsync(cancellationToken);

        WishlistCount = await _context.UserSets
            .Where(us => us.UserId == user.Id && us.IsWishlist)
            .CountAsync(cancellationToken);

        // Pie chart: Sets by status
        SetsBuilt = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist && us.Set.NumParts > 0)
            .Where(us => us.Status == SetStatus.Built)
            .CountAsync(cancellationToken);

        SetsBuilding = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist && us.Set.NumParts > 0)
            .Where(us => us.Status == SetStatus.Building)
            .CountAsync(cancellationToken);

        SetsInStorage = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist && us.Set.NumParts > 0)
            .Where(us => us.Status == SetStatus.InStorage)
            .CountAsync(cancellationToken);

        // Pie chart: Pieces by status
        PiecesBuilt = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist && us.Set.NumParts > 0)
            .Where(us => us.Status == SetStatus.Built)
            .SumAsync(us => us.Set.NumParts * us.Quantity, cancellationToken);

        PiecesBuilding = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist && us.Set.NumParts > 0)
            .Where(us => us.Status == SetStatus.Building)
            .SumAsync(us => us.Set.NumParts * us.Quantity, cancellationToken);

        PiecesInStorage = await _context.UserSets
            .Include(us => us.Set)
            .Where(us => us.UserId == user.Id && !us.IsWishlist && us.Set.NumParts > 0)
            .Where(us => us.Status == SetStatus.InStorage)
            .SumAsync(us => us.Set.NumParts * us.Quantity, cancellationToken);
    }
}
