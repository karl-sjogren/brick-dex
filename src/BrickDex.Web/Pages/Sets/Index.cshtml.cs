using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Sets;

public class IndexModel : PageModel {
    private readonly ILegoSetService _legoSetService;

    public IndexModel(ILegoSetService legoSetService) {
        _legoSetService = legoSetService;
    }

    public IReadOnlyList<LegoSet> Sets { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) {
        Sets = await _legoSetService.GetAllAsync(includeWishlist: false, cancellationToken);
    }
}
