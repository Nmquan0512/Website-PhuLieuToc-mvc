using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Controllers
{
	public class BrandController : Controller
	{
		private readonly AppDbContext _context;

		public BrandController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(string Slug = "")
		{
			BrandModel brandModel = _context.Brands.Where(b => b.Slug == Slug).FirstOrDefault();
			if (brandModel == null)
			{
				return RedirectToAction("Index");
			}
			var BrandTheoProduct = _context.Products.Where(p => p.BrandId == brandModel.Id);

			return View( await BrandTheoProduct.ToListAsync());
		}
	}
}
