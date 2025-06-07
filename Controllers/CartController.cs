using Microsoft.AspNetCore.Mvc;

namespace PhuLieuToc.Controllers
{
	public class CartController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
		public IActionResult Checkout()
		{
			return View("~/Views/CheckOut/Index.cshtml");
		}
	}
}
