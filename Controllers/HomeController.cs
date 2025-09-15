using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;
using System.Diagnostics;

namespace PhuLieuToc.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;


        private readonly ILogger<HomeController> _logger;


        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            // Lấy sản phẩm cha (SanPham) và kèm 1 biến thể đầu tiên để hiển thị ảnh/giá
            var productsRaw = await _context.SanPhams
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.SanPhamChiTiets)
                .Where(p => p.TrangThai == 1)
                .OrderByDescending(p => p.SanPhamId)
                .Take(8)
                .ToListAsync();

            var featuredProducts = productsRaw.Select(p => new
            {
                SanPham = p,
                FirstVariant = (
                    p.SanPhamChiTiets
                        .Where(v => v.TrangThai == 1 && !string.IsNullOrEmpty(v.Anh))
                        .OrderBy(v => v.SanPhamChiTietId)
                        .FirstOrDefault()
                ) ?? p.SanPhamChiTiets
                        .Where(v => v.TrangThai == 1)
                        .OrderBy(v => v.SanPhamChiTietId)
                        .FirstOrDefault()
            }).ToList();

            var categories = await _context.Categorys
                .Where(c => c.TrangThai == 1)
                .Take(6)
                .ToListAsync();

            var brands = await _context.Brands
                .Where(b => b.TrangThai == 1)
                .Take(6)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            ViewBag.ContactsEnabled = true;

            return View(featuredProducts);
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categorys.Where(c => c.TrangThai == 1).ToListAsync();
            return View(categories);
        }

        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
