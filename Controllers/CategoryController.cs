using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Controllers
{
	public class CategoryController : Controller
	{
		private readonly AppDbContext _context;

		public CategoryController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(string Slug = "" )
		{
			CategoryModel category = _context.Categorys.Where( c => c.Slug == Slug ).FirstOrDefault();
			if (category == null)
			{

				return RedirectToAction("Index");
			}
			var productByCategory = _context.SanPhamChiTiets
				.Include(x => x.SanPham)
				.Where(p => p.SanPham.CategoryId == category.Id);

			return View(await productByCategory.ToListAsync());
		}
	}
}
