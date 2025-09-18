using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DonHangController : Controller
    {
        private readonly AppDbContext _db;
        public DonHangController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var data = await _db.HoaDons
                .Include(h => h.HoaDonChiTiets).ThenInclude(c => c.SanPhamChiTiet)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            if (!Guid.TryParse(id, out var gid)) return NotFound();
            var h = await _db.HoaDons
                .Include(x=>x.HoaDonChiTiets)
                .FirstOrDefaultAsync(x=>x.HoaDonId==gid);
            if (h == null) return NotFound();
            if (h.TrangThai == 0 || h.TrangThai == 1)
            {
                var old = h.TrangThai; h.TrangThai = 4; h.NgayCapNhat = DateTime.Now;
                _db.LichSuTrangThaiHoaDons.Add(new Models.LichSuTrangThaiHoaDon { HoaDonId = h.HoaDonId, TrangThaiCu = old, TrangThaiMoi = 4, ThoiGianThayDoi = DateTime.Now, GhiChu = "Huỷ đơn" });
                // hoàn lại số lượng tồn
                foreach (var c in h.HoaDonChiTiets)
                {
                    var spct = await _db.SanPhamChiTiets.FirstOrDefaultAsync(s=>s.SanPhamChiTietId==c.SanPhamChiTietId);
                    if (spct != null){ spct.SoLuongTon += c.SoLuong; }
                }
                await _db.SaveChangesAsync();

                // notify customer via email (background)
                try
                {
                    var cfg = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                    var email = h.TaiKhoanId != null
                        ? await _db.TaiKhoans.Where(t=>t.TaiKhoanId==h.TaiKhoanId).Select(t=>t.Email).FirstOrDefaultAsync()
                        : h.EmailKhachHang;
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var subject = $"Đơn hàng #{h.HoaDonId.ToString().Substring(0,8).ToUpper()} đã bị huỷ";
                        var body = BuildOrderEmailHtml(h,
                            header: "Đơn hàng đã bị huỷ",
                            sub: $"Chúng tôi rất tiếc. Đơn hàng của bạn đã được huỷ vào {DateTime.Now:dd/MM/yyyy HH:mm}.");
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                var host = cfg?["EmailSettings:SmtpServer"]; var port = int.TryParse(cfg?["EmailSettings:SmtpPort"], out var p) ? p : 587;
                                var user = cfg?["EmailSettings:SenderEmail"]; var pass = cfg?["EmailSettings:SenderPassword"]; var display = cfg?["EmailSettings:SenderName"] ?? "Phụ Liệu Tóc";
                                using var client = new System.Net.Mail.SmtpClient(host, port) { EnableSsl = true, Credentials = new System.Net.NetworkCredential(user, pass) };
                                var mail = new System.Net.Mail.MailMessage(new System.Net.Mail.MailAddress(user!, display), new System.Net.Mail.MailAddress(email)) { Subject = subject, Body = body, IsBodyHtml = true };
                                await client.SendMailAsync(mail);
                            }
                            catch { }
                        });
                    }
                }
                catch { }

                TempData["Success"] = "Đã huỷ đơn hàng.";
            }
            else TempData["Error"] = "Không thể huỷ đơn ở trạng thái hiện tại.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, int status)
        {
            if (!Guid.TryParse(id, out var gid)) return NotFound();
            var h = await _db.HoaDons
                .Include(x => x.HoaDonChiTiets)
                .FirstOrDefaultAsync(x => x.HoaDonId == gid);
            if (h == null) return NotFound();
            var old = h.TrangThai;
            if (status < 0 || status > 4) { TempData["Error"] = "Trạng thái không hợp lệ."; return RedirectToAction(nameof(Index)); }
            // business rule: chỉ cho phép đi tới 1->2->3 hoặc hủy về 4
            h.TrangThai = status;
            h.NgayCapNhat = DateTime.Now;
            _db.LichSuTrangThaiHoaDons.Add(new Models.LichSuTrangThaiHoaDon { HoaDonId = h.HoaDonId, TrangThaiCu = old, TrangThaiMoi = h.TrangThai, ThoiGianThayDoi = DateTime.Now });
            await _db.SaveChangesAsync();

            // send email notify for status change (background)
            try
            {
                var cfg = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var email = h.TaiKhoanId != null
                    ? await _db.TaiKhoans.Where(t=>t.TaiKhoanId==h.TaiKhoanId).Select(t=>t.Email).FirstOrDefaultAsync()
                    : h.EmailKhachHang;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    string statusText = h.TrangThai == 1 ? "Đã duyệt" : h.TrangThai == 2 ? "Đang giao" : h.TrangThai == 3 ? "Đã giao" : h.TrangThai == 4 ? "Đã huỷ" : "Chờ duyệt";
                    var subject = $"Cập nhật trạng thái đơn hàng #{h.HoaDonId.ToString().Substring(0,8).ToUpper()}";
                    var body = BuildOrderEmailHtml(h,
                        header: $"Trạng thái: {statusText}",
                        sub: $"Đơn hàng của bạn đã được cập nhật vào {DateTime.Now:dd/MM/yyyy HH:mm}.");
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            var host = cfg?["EmailSettings:SmtpServer"]; var port = int.TryParse(cfg?["EmailSettings:SmtpPort"], out var p) ? p : 587;
                            var user = cfg?["EmailSettings:SenderEmail"]; var pass = cfg?["EmailSettings:SenderPassword"]; var display = cfg?["EmailSettings:SenderName"] ?? "Phụ Liệu Tóc";
                            using var client = new System.Net.Mail.SmtpClient(host, port) { EnableSsl = true, Credentials = new System.Net.NetworkCredential(user, pass) };
                            var mail = new System.Net.Mail.MailMessage(new System.Net.Mail.MailAddress(user!, display), new System.Net.Mail.MailAddress(email)) { Subject = subject, Body = body, IsBodyHtml = true };
                            await client.SendMailAsync(mail);
                        }
                        catch { }
                    });
                }
            }
            catch { }

            TempData["Success"] = $"Đã cập nhật trạng thái lúc {DateTime.Now:HH:mm:ss}.";
            return RedirectToAction(nameof(Index));
        }

        private string BuildOrderEmailHtml(PhuLieuToc.Models.HoaDon order, string header, string sub)
        {
            var rows = string.Join("",
                (order.HoaDonChiTiets ?? new List<PhuLieuToc.Models.HoaDonChiTiet>()).Select(c =>
                    $"<tr><td style='padding:10px 12px'>{System.Net.WebUtility.HtmlEncode(c.TenSanPhamLucMua)}</td>" +
                    $"<td style='padding:10px 12px;text-align:center'>{c.SoLuong}</td>" +
                    $"<td style='padding:10px 12px;text-align:right'>{c.DonGia:n0} đ</td>" +
                    $"<td style='padding:10px 12px;text-align:right'>{c.ThanhTien:n0} đ</td></tr>"));

            var idShort = order.HoaDonId.ToString().Substring(0, 8).ToUpper();
            var html = $@"<div style='font-family:Segoe UI,Arial,sans-serif;background:#f6faf5;padding:18px'>
  <div style='max-width:680px;margin:0 auto;background:#ffffff;border:1px solid #e2efe0;border-radius:12px;overflow:hidden;box-shadow:0 6px 24px rgba(122,148,112,0.12)'>
    <div style='background:linear-gradient(135deg,#7a9470,#99b18f);color:#fff;padding:18px 22px'>
      <h2 style='margin:0;font-weight:700'>Linh Trang Store</h2>
      <div style='opacity:.9'>Đơn hàng #{idShort}</div>
    </div>
    <div style='padding:20px'>
      <h3 style='color:#5d7355;margin:0 0 6px 0'>{System.Net.WebUtility.HtmlEncode(header)}</h3>
      <p style='margin:0 0 14px 0;color:#4a5d42'>{System.Net.WebUtility.HtmlEncode(sub)}</p>
      <div style='background:#f9fcf7;border:1px solid #e6efe6;border-radius:10px;padding:14px;margin-bottom:14px'>
        <strong style='color:#4a5d42'>Thông tin khách hàng</strong>
        <div style='margin-top:6px;color:#37493a'>
          Họ tên: <strong>{System.Net.WebUtility.HtmlEncode(order.TenKhachHang)}</strong><br/>
          SĐT: {System.Net.WebUtility.HtmlEncode(order.SoDienThoai)}<br/>
          Email: {System.Net.WebUtility.HtmlEncode(order.TaiKhoanId != null ? order.TaiKhoan?.Email ?? "" : order.EmailKhachHang ?? "") }<br/>
          Địa chỉ: {System.Net.WebUtility.HtmlEncode(order.DiaChiGiaoHang)}
        </div>
      </div>
      <table style='width:100%;border-collapse:collapse;background:#ffffff;border:1px solid #e6efe6'>
        <thead>
          <tr style='background:#eef6ea;color:#2f3f2a'>
            <th style='text-align:left;padding:10px 12px'>Sản phẩm</th>
            <th style='text-align:center;padding:10px 12px;width:70px'>SL</th>
            <th style='text-align:right;padding:10px 12px;width:120px'>Đơn giá</th>
            <th style='text-align:right;padding:10px 12px;width:140px'>Thành tiền</th>
          </tr>
        </thead>
        <tbody>{rows}</tbody>
        <tfoot>
          <tr>
            <td colspan='3' style='text-align:right;padding:10px 12px;border-top:1px solid #e6efe6'><strong>Tổng cộng:</strong></td>
            <td style='text-align:right;padding:10px 12px;border-top:1px solid #e6efe6'><strong style='color:#2f4a31'>{order.TongTien:n0} đ</strong></td>
          </tr>
        </tfoot>
      </table>
      <p style='color:#55715a;margin-top:14px'>Nếu bạn cần hỗ trợ, vui lòng phản hồi email này hoặc liên hệ hotline.</p>
    </div>
    <div style='background:#f1f6ef;color:#4a5d42;padding:12px 20px;text-align:center'>
      © {DateTime.Now.Year} Linh Trang Store
    </div>
  </div>
</div>";
            return html;
        }

        // Backward-compat for old forms posting to NextStatus (computes next state then forwards)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NextStatus(string id)
        {
            if (!Guid.TryParse(id, out var gid)) return NotFound();
            var h = await _db.HoaDons.FindAsync(gid);
            if (h == null) return NotFound();
            var next = h.TrangThai == 0 ? 1 : (h.TrangThai == 1 ? 2 : (h.TrangThai == 2 ? 3 : h.TrangThai));
            if (next == h.TrangThai)
            {
                TempData["Error"] = "Trạng thái cuối cùng rồi.";
                return RedirectToAction(nameof(Index));
            }
            return await UpdateStatus(id, next);
        }
    }
}


