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
            var featuredProducts = await _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                    .ThenInclude(s => s.Brand)
                .Include(x => x.SanPham)
                    .ThenInclude(s => s.Category)
                .Include(x => x.SanPhamChiTietThuocTinhs)
                    .ThenInclude(t => t.GiaTriThuocTinh)
                .Where(x => x.TrangThai == 1 && x.SanPham.TrangThai == 1)
                .OrderByDescending(x => x.SanPham.SanPhamId)
                .Take(8)
                .ToListAsync();

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

            return View(featuredProducts);
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
