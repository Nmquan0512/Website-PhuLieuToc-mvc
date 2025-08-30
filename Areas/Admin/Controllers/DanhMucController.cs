using Microsoft.AspNetCore.Mvc;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DanhMucController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
