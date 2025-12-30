using BrickDex.Core.Contracts;
using BrickDex.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BrickDex.Web.Pages.Sets;

public class DetailsModel : PageModel {
    private readonly ILegoSetService _legoSetService;

    public DetailsModel(ILegoSetService legoSetService) {
        _legoSetService = legoSetService;
    }

    [BindProperty]
    public LegoSet? Set { get; set; }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken) {
        Set = await _legoSetService.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) {
        if(Set == null) {
            return NotFound();
        }

        await _legoSetService.UpdateAsync(Set, cancellationToken);
        return RedirectToPage("/Sets/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken) {
        await _legoSetService.DeleteAsync(id, cancellationToken);
        return RedirectToPage("/Sets/Index");
    }
}
