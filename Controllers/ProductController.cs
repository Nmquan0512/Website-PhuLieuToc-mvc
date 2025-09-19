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

		public async Task<IActionResult> Index(int? categoryId, int? brandId, string? search, string? categorySlug = null, string? brandSlug = null)
		{
			// Resolve slugs to IDs if provided
			if (!string.IsNullOrWhiteSpace(categorySlug))
			{
				var cat = await _context.Categorys.FirstOrDefaultAsync(c => c.Slug == categorySlug && c.TrangThai == 1);
				if (cat != null) categoryId = cat.Id;
			}
			if (!string.IsNullOrWhiteSpace(brandSlug))
			{
				var br = await _context.Brands.FirstOrDefaultAsync(b => b.Slug == brandSlug && b.TrangThai == 1);
				if (br != null) brandId = br.Id;
			}

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

			// Sản phẩm liên quan (ưu tiên cùng danh mục; nếu không có thì cùng thương hiệu)
			var relatedProducts = await _context.SanPhams
				.Include(p => p.SanPhamChiTiets)
				.Where(p => p.CategoryId == product.SanPham.CategoryId
					&& p.SanPhamId != product.SanPhamId
					&& p.TrangThai == 1
					&& p.SanPhamChiTiets.Any(v => v.TrangThai == 1))
				.OrderByDescending(p => p.SanPhamId)
				.Take(8)
				.ToListAsync();

			if (relatedProducts.Count == 0)
			{
				relatedProducts = await _context.SanPhams
					.Include(p => p.SanPhamChiTiets)
					.Where(p => p.BrandId == product.SanPham.BrandId
						&& p.SanPhamId != product.SanPhamId
						&& p.TrangThai == 1
						&& p.SanPhamChiTiets.Any(v => v.TrangThai == 1))
					.OrderByDescending(p => p.SanPhamId)
					.Take(8)
					.ToListAsync();
			}
			ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

		[HttpGet]
		public async Task<IActionResult> DetailsBySlug(string slug, int? variantId)
		{
			if (string.IsNullOrWhiteSpace(slug)) return NotFound();
			var prod = await _context.SanPhams
				.Include(p => p.Category)
				.Include(p => p.Brand)
				.FirstOrDefaultAsync(p => p.Slug == slug && p.TrangThai == 1);
			if (prod == null) return NotFound();

			SanPhamChiTiet? variant = null;
			if (variantId.HasValue)
			{
				variant = await _context.SanPhamChiTiets
					.Include(s => s.SanPham)
						.ThenInclude(p => p.Category)
					.Include(s => s.SanPham)
						.ThenInclude(p => p.Brand)
					.Include(s => s.SanPhamChiTietThuocTinhs)
						.ThenInclude(t => t.GiaTriThuocTinh)
							.ThenInclude(g => g.ThuocTinh)
					.FirstOrDefaultAsync(s => s.SanPhamId == prod.SanPhamId && s.SanPhamChiTietId == variantId.Value && s.TrangThai == 1);
			}

			if (variant == null)
			{
				variant = await _context.SanPhamChiTiets
					.Include(s => s.SanPham)
						.ThenInclude(p => p.Category)
					.Include(s => s.SanPham)
						.ThenInclude(p => p.Brand)
					.Include(s => s.SanPhamChiTietThuocTinhs)
						.ThenInclude(t => t.GiaTriThuocTinh)
							.ThenInclude(g => g.ThuocTinh)
					.Where(s => s.SanPhamId == prod.SanPhamId && s.TrangThai == 1)
					.OrderBy(s => s.SanPhamChiTietId)
					.FirstOrDefaultAsync();
			}

			if (variant == null) return NotFound();

			var relatedVariants = await _context.SanPhamChiTiets
				.Include(s => s.SanPhamChiTietThuocTinhs)
					.ThenInclude(t => t.GiaTriThuocTinh)
						.ThenInclude(g => g.ThuocTinh)
				.Where(s => s.SanPhamId == prod.SanPhamId && s.TrangThai == 1)
				.ToListAsync();
			ViewBag.RelatedVariants = relatedVariants;

			var relatedProducts = await _context.SanPhams
				.Include(p => p.SanPhamChiTiets)
				.Where(p => p.CategoryId == prod.CategoryId && p.SanPhamId != prod.SanPhamId && p.TrangThai == 1 && p.SanPhamChiTiets.Any(v => v.TrangThai == 1))
				.OrderByDescending(p => p.SanPhamId)
				.Take(8)
				.ToListAsync();

			if (relatedProducts.Count == 0)
			{
				relatedProducts = await _context.SanPhams
					.Include(p => p.SanPhamChiTiets)
					.Where(p => p.BrandId == prod.BrandId && p.SanPhamId != prod.SanPhamId && p.TrangThai == 1 && p.SanPhamChiTiets.Any(v => v.TrangThai == 1))
					.OrderByDescending(p => p.SanPhamId)
					.Take(8)
					.ToListAsync();
			}
			ViewBag.RelatedProducts = relatedProducts;

			return View("Details", variant);
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
