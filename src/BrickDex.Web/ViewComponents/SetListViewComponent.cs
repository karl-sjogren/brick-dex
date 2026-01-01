using BrickDex.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BrickDex.Web.ViewComponents;

public class SetListViewComponent : ViewComponent {
    public IViewComponentResult Invoke(SetListViewModel model) {
        return View(model);
    }
}
