using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db){ _db = db; }
        public IActionResult Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var totalOrders = _db.HoaDons.Count();
            var totalProducts = _db.SanPhams.Count();
            var totalCustomers = _db.TaiKhoans.Count();

            var revenueToday = _db.HoaDons
                .Where(h => h.NgayTao >= today && h.NgayTao < tomorrow && h.TrangThai == 3)
                .Sum(h => (decimal?)h.TongTien) ?? 0m;

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.RevenueToday = revenueToday;
            // Recent orders (top 10)
            var recent = _db.HoaDons
                .OrderByDescending(h => h.NgayTao)
                .Select(h => new { h.HoaDonId, h.TenKhachHang, h.TongTien, h.TrangThai, h.NgayTao })
                .Take(10)
                .ToList();
            ViewBag.RecentOrders = recent.Select(h => new {
                HoaDonId = h.HoaDonId,
                CustomerName = h.TenKhachHang,
                TongTienSauKhiGiam = h.TongTien,
                TrangThai = h.TrangThai,
                NgayTao = h.NgayTao
            }).ToList();

            // Orders today count
            ViewBag.TodayOrders = _db.HoaDons.Count(h => h.NgayTao >= today && h.NgayTao < tomorrow);
            ViewBag.TodayRevenue = revenueToday;

            // Chart data
            var currentYear = DateTime.Today.Year;
            var revenueSeries = new decimal[12];
            for (int m = 1; m <= 12; m++)
            {
                var start = new DateTime(currentYear, m, 1);
                var end = start.AddMonths(1);
                revenueSeries[m - 1] = _db.HoaDons
                    .Where(h => h.NgayTao >= start && h.NgayTao < end && h.TrangThai == 3)
                    .Sum(h => (decimal?)h.TongTien) ?? 0m;
            }
            ViewBag.RevenueSeries = revenueSeries;

            var statusCounts = new int[5];
            statusCounts[0] = _db.HoaDons.Count(h => h.TrangThai == 0);
            statusCounts[1] = _db.HoaDons.Count(h => h.TrangThai == 1);
            statusCounts[2] = _db.HoaDons.Count(h => h.TrangThai == 2);
            statusCounts[3] = _db.HoaDons.Count(h => h.TrangThai == 3);
            statusCounts[4] = _db.HoaDons.Count(h => h.TrangThai == 4);
            ViewBag.StatusCounts = statusCounts;

            return View();
        }
    }
}
