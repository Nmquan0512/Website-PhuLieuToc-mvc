using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Controllers
{
	public class ProductController : Controller
	{
		private readonly AppDbContext _context;

		public ProductController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(int? categoryId, int? brandId, string? search)
		{
			var query = _context.SanPhamChiTiets
				.Include(s => s.SanPham)
					.ThenInclude(p => p.Category)
				.Include(s => s.SanPham)
					.ThenInclude(p => p.Brand)
				.Include(s => s.SanPhamChiTietThuocTinhs)
					.ThenInclude(t => t.GiaTriThuocTinh)
				.Where(s => s.TrangThai == 1 && s.SanPham.TrangThai == 1);

            if (categoryId.HasValue)
            {
                // Lấy danh mục và tất cả danh mục con
                var categoryIds = await GetCategoryAndChildrenIds(categoryId.Value);
                query = query.Where(s => categoryIds.Contains(s.SanPham.CategoryId));
            }

			if (brandId.HasValue)
			{
				query = query.Where(s => s.SanPham.BrandId == brandId.Value);
			}

			if (!string.IsNullOrEmpty(search))
			{
				query = query.Where(s => s.SanPham.TenSanPham.Contains(search));
			}

            var products = await query.ToListAsync();

            ViewBag.Categories = await _context.Categorys.Where(c => c.TrangThai == 1).ToListAsync();
            ViewBag.Brands = await _context.Brands.Where(b => b.TrangThai == 1).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SearchTerm = search;

            // Lấy tên danh mục đã chọn
            if (categoryId.HasValue)
            {
                var selectedCategory = await _context.Categorys
                    .FirstOrDefaultAsync(c => c.Id == categoryId.Value);
                ViewBag.SelectedCategoryName = selectedCategory?.TenDanhMuc;
            }

            return View(products);
		}

		public async Task<IActionResult> Details(int id)
		{
			var product = await _context.SanPhamChiTiets
				.Include(s => s.SanPham)
					.ThenInclude(p => p.Category)
				.Include(s => s.SanPham)
					.ThenInclude(p => p.Brand)
				.Include(s => s.SanPhamChiTietThuocTinhs)
					.ThenInclude(t => t.GiaTriThuocTinh)
						.ThenInclude(g => g.ThuocTinh)
				.FirstOrDefaultAsync(s => s.SanPhamChiTietId == id && s.TrangThai == 1);

			if (product == null)
			{
				return NotFound();
			}

			// Lấy các biến thể khác của cùng sản phẩm
			var relatedVariants = await _context.SanPhamChiTiets
				.Include(s => s.SanPhamChiTietThuocTinhs)
					.ThenInclude(t => t.GiaTriThuocTinh)
						.ThenInclude(g => g.ThuocTinh)
				.Where(s => s.SanPhamId == product.SanPhamId && s.TrangThai == 1)
				.ToListAsync();

			ViewBag.RelatedVariants = relatedVariants;

            return View(product);
        }

        private async Task<List<int>> GetCategoryAndChildrenIds(int categoryId)
        {
            var categoryIds = new List<int> { categoryId };
            
            // Lấy tất cả danh mục con
            var children = await _context.Categorys
                .Where(c => c.ParentCategoryId == categoryId && c.TrangThai == 1)
                .ToListAsync();
            
            foreach (var child in children)
            {
                categoryIds.Add(child.Id);
                // Đệ quy lấy danh mục con của danh mục con
                var grandChildren = await GetCategoryAndChildrenIds(child.Id);
                categoryIds.AddRange(grandChildren);
            }
            
            return categoryIds.Distinct().ToList();
        }
    }
}
