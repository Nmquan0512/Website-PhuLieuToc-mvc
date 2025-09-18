using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _db;
        private readonly Service.VNPayService _vnp;

        public PaymentController(AppDbContext db, Service.VNPayService vnp)
        {
            _db = db; _vnp = vnp;
        }

        [HttpGet]
        public async Task<IActionResult> VNPayReturn()
        {
            // validate signature
            var valid = _vnp.ValidateReturn(Request.Query, out var rspCode, out var txnRef);
            // Find order by partial id (we used first 12 chars of Guid without dashes)
            var order = await _db.HoaDons
                .OrderByDescending(h => h.NgayTao)
                .FirstOrDefaultAsync(h => h.HoaDonId.ToString("N").StartsWith(txnRef, StringComparison.OrdinalIgnoreCase));

            if (!valid || order == null)
            {
                TempData["Error"] = "Thanh toán VNPay không hợp lệ.";
                return RedirectToAction("Index", "Cart");
            }

            if (rspCode == "00")
            {
                // success -> mark as paid/approved if applicable
                if (order.TrangThai == 0) order.TrangThai = 1; // auto approve
                order.NgayCapNhat = DateTime.Now;
                // now that VNPay succeeded: reduce stocks and clear cart
                try
                {
                    var items = await _db.HoaDonChiTiets.Where(c => c.HoaDonId == order.HoaDonId).ToListAsync();
                    foreach (var i in items)
                    {
                        var spct = await _db.SanPhamChiTiets.FirstOrDefaultAsync(s => s.SanPhamChiTietId == i.SanPhamChiTietId);
                        if (spct != null)
                        {
                            spct.SoLuongTon -= i.SoLuong;
                        }
                    }
                    await _db.SaveChangesAsync();
                }
                catch { }
                await _db.SaveChangesAsync();
                TempData["Success"] = "Thanh toán VNPay thành công.";
                return RedirectToAction("OrderSuccess", "Cart", new { orderId = order.HoaDonId });
            }
            else
            {
                // optional: remove pending order if failed
                try
                {
                    // Do not delete, but mark as cancelled
                    order.TrangThai = 4; // cancelled
                    order.NgayCapNhat = DateTime.Now;
                    await _db.SaveChangesAsync();
                }
                catch { }
                TempData["Error"] = "VNPay: giao dịch không thành công (" + rspCode + ").";
                return RedirectToAction("Index", "Cart");
            }
        }
    }
}


