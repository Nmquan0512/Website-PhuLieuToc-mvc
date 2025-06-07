using Microsoft.AspNetCore.Mvc;

namespace PhuLieuToc.Controllers
{
	public class ProductController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
		public IActionResult Details()
		{
			return View();
		}
	}
}
