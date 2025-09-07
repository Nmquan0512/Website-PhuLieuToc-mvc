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
        public IActionResult Index()
        {
            var ListProduct = _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .ThenInclude(s => s.Brand)
                .ToList();
            return View(ListProduct);
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
