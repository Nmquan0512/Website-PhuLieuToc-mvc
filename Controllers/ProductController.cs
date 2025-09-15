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
			var query = _context.SanPhams
				.Include(p => p.Category)
				.Include(p => p.Brand)
				.Include(p => p.SanPhamChiTiets)
				.Where(p => p.TrangThai == 1);

            if (categoryId.HasValue)
            {
                var categoryIds = await GetCategoryAndChildrenIds(categoryId.Value);
                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }

			if (brandId.HasValue)
			{
				query = query.Where(p => p.BrandId == brandId.Value);
			}

			if (!string.IsNullOrEmpty(search))
			{
				query = query.Where(p => p.TenSanPham.Contains(search));
			}

            var products = await query.ToListAsync();

            ViewBag.Categories = await _context.Categorys.Where(c => c.TrangThai == 1).ToListAsync();
            ViewBag.Brands = await _context.Brands.Where(b => b.TrangThai == 1).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SearchTerm = search;

            if (categoryId.HasValue)
            {
                var selectedCategory = await _context.Categorys.FirstOrDefaultAsync(c => c.Id == categoryId.Value);
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

        [HttpGet]
        public async Task<IActionResult> GetVariants(int productId)
        {
            var variants = await _context.SanPhamChiTiets
                .Include(v => v.SanPhamChiTietThuocTinhs)
                    .ThenInclude(t => t.GiaTriThuocTinh)
                        .ThenInclude(g => g.ThuocTinh)
                .Where(v => v.SanPhamId == productId && v.TrangThai == 1)
                .Select(v => new {
                    id = v.SanPhamChiTietId,
                    gia = v.Gia,
                    soLuongTon = v.SoLuongTon,
                    anh = v.Anh,
                    thuocTinh = v.SanPhamChiTietThuocTinhs.Select(t => new { loai = t.GiaTriThuocTinh.ThuocTinh.TenThuocTinh, giaTri = t.GiaTriThuocTinh.TenGiaTri })
                })
                .ToListAsync();

            var prices = variants.Select(v => v.gia).ToList();
            var min = prices.Count > 0 ? prices.Min() : 0;
            var max = prices.Count > 0 ? prices.Max() : 0;

            return Json(new { success = true, variants, minPrice = min, maxPrice = max });
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
