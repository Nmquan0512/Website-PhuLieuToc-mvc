using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HoaDonController : Controller
    {
        private readonly AppDbContext _db;
        public HoaDonController(AppDbContext db) { _db = db; }

        // Chỉ hiển thị hoá đơn trạng thái 3 (hoàn thành)
        public async Task<IActionResult> Index()
        {
            var data = await _db.HoaDons
                .Include(h => h.HoaDonChiTiets).ThenInclude(c => c.SanPhamChiTiet)
                .Where(h => h.TrangThai == 3)
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!Guid.TryParse(id, out var gid)) return NotFound();
            var hoadon = await _db.HoaDons
                .Include(h => h.HoaDonChiTiets).ThenInclude(c => c.SanPhamChiTiet)
                .Include(h => h.LichSuTrangThaiHoaDons)
                .Include(h => h.TaiKhoan)
                .FirstOrDefaultAsync(h => h.HoaDonId == gid);
            if (hoadon == null) return NotFound();
            return View(hoadon);
        }

        // Xuất PDF: dùng trang in (browser Print to PDF)
        public async Task<IActionResult> Export(string id)
        {
            if (!Guid.TryParse(id, out var gid)) return NotFound();
            var hoadon = await _db.HoaDons
                .Include(h => h.HoaDonChiTiets).ThenInclude(c => c.SanPhamChiTiet)
                .Include(h => h.LichSuTrangThaiHoaDons)
                .FirstOrDefaultAsync(h => h.HoaDonId == gid);
            if (hoadon == null) return NotFound();
            ViewBag.Print = true; // bật auto print trong view
            return View("Details", hoadon);
        }
    }
}


