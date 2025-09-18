using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuLieuToc.Repository;
using System.Linq;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult GetChartData(string period = "month")
        {
            // Only 'month' is implemented for now to match the dashboard view
            var currentYear = DateTime.Today.Year;
            var labels = Enumerable.Range(1, 12).Select(m => $"T{m}").ToArray();
            var revenue = new decimal[12];
            for (int m = 1; m <= 12; m++)
            {
                var start = new DateTime(currentYear, m, 1);
                var end = start.AddMonths(1);
                revenue[m - 1] = _db.HoaDons
                    .Where(h => h.NgayTao >= start && h.NgayTao < end && h.TrangThai == 3)
                    .Select(h => (decimal?)h.TongTien)
                    .Sum() ?? 0m;
            }

            var statusLabels = new[] { "Chờ duyệt", "Đã duyệt", "Đang giao", "Đã giao", "Đã hủy" };
            var statusValues = new[]
            {
                _db.HoaDons.Count(h => h.TrangThai == 0),
                _db.HoaDons.Count(h => h.TrangThai == 1),
                _db.HoaDons.Count(h => h.TrangThai == 2),
                _db.HoaDons.Count(h => h.TrangThai == 3),
                _db.HoaDons.Count(h => h.TrangThai == 4)
            };

            return Json(new
            {
                success = true,
                revenueData = new object[] { new { labels }, new { values = revenue } },
                orderStatusData = new object[] { new { labels = statusLabels }, new { values = statusValues } }
            });
        }
    }
}


