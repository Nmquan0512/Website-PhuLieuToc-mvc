using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Models.ViewModels;
using PhuLieuToc.Repository;
using System.Security.Claims;

namespace PhuLieuToc.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            try
            {
                var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var host = config?["EmailSettings:SmtpServer"];
                var port = int.TryParse(config?["EmailSettings:SmtpPort"], out var p) ? p : 587;
                var user = config?["EmailSettings:SenderEmail"];
                var pass = config?["EmailSettings:SenderPassword"];
                var display = config?["EmailSettings:SenderName"] ?? "Phụ Liệu Tóc";
                using var client = new System.Net.Mail.SmtpClient(host, port) { EnableSsl = true, Credentials = new System.Net.NetworkCredential(user, pass) };
                var mail = new System.Net.Mail.MailMessage(new System.Net.Mail.MailAddress(user!, display), new System.Net.Mail.MailAddress(toEmail)) { Subject = subject, Body = body, IsBodyHtml = isHtml };
                await client.SendMailAsync(mail);
            }
            catch { }
        }

        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(u => u.TaiKhoanId == userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng";
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(TaiKhoan model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            try
            {
                var user = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.TaiKhoanId == userId);

                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin người dùng";
                    return RedirectToAction("Profile");
                }

                // Kiểm tra email trùng lặp
                if (await _context.TaiKhoans.AnyAsync(u => u.Email == model.Email && u.TaiKhoanId != userId))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View("Profile", model);
                }

                // Kiểm tra SĐT trùng lặp
                if (!string.IsNullOrEmpty(model.SoDienThoai) && 
                    await _context.TaiKhoans.AnyAsync(u => u.SoDienThoai == model.SoDienThoai && u.TaiKhoanId != userId))
                {
                    ModelState.AddModelError("SoDienThoai", "Số điện thoại đã tồn tại");
                    return View("Profile", model);
                }

                // Cập nhật thông tin
                user.Email = model.Email;
                user.SoDienThoai = model.SoDienThoai;
                user.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View("Profile", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và xác nhận mật khẩu không khớp";
                return RedirectToAction("Profile");
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự";
                return RedirectToAction("Profile");
            }

            try
            {
                var user = await _context.TaiKhoans
                    .FirstOrDefaultAsync(u => u.TaiKhoanId == userId);

                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin người dùng";
                    return RedirectToAction("Profile");
                }

                // Kiểm tra mật khẩu hiện tại
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.MatKhau))
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng";
                    return RedirectToAction("Profile");
                }

                // Cập nhật mật khẩu mới
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        public async Task<IActionResult> Orders()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var orders = await _context.HoaDons
                .Include(h => h.HoaDonChiTiets)
                .Include(h => h.TaiKhoan)
                .Where(h => h.TaiKhoanId == userId)
                .OrderByDescending(h => h.NgayTao)
                .ToListAsync();

            var orderViewModels = orders.Select(order => new OrderStatusViewModel
            {
                HoaDonId = order.HoaDonId,
                TenKhachHang = order.TenKhachHang,
                SoDienThoai = order.SoDienThoai,
                DiaChiGiaoHang = order.DiaChiGiaoHang,
                TongTien = order.TongTien,
                TrangThai = order.TrangThai,
                PhuongThucThanhToan = order.PhuongThucThanhToan,
                NgayTao = order.NgayTao,
                ChiTietHoaDon = order.HoaDonChiTiets.ToList()
            }).ToList();

            return View(orderViewModels);
        }

        public async Task<IActionResult> OrderDetails(Guid orderId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var order = await _context.HoaDons
                .Include(h => h.HoaDonChiTiets)
                .FirstOrDefaultAsync(h => h.HoaDonId == orderId && h.TaiKhoanId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Orders");
            }

            var orderViewModel = new OrderStatusViewModel
            {
                HoaDonId = order.HoaDonId,
                TenKhachHang = order.TenKhachHang,
                SoDienThoai = order.SoDienThoai,
                DiaChiGiaoHang = order.DiaChiGiaoHang,
                Email = order.TaiKhoanId != null ? (await _context.TaiKhoans.Where(t=>t.TaiKhoanId==order.TaiKhoanId).Select(t=>t.Email).FirstOrDefaultAsync()) : order.EmailKhachHang,
                TongTien = order.TongTien,
                TrangThai = order.TrangThai,
                PhuongThucThanhToan = order.PhuongThucThanhToan,
                NgayTao = order.NgayTao,
                ChiTietHoaDon = order.HoaDonChiTiets.ToList()
            };

            // Build history dictionary for client timeline (same as admin)
            try
            {
                var histories = await _context.LichSuTrangThaiHoaDons
                    .Where(l => l.HoaDonId == order.HoaDonId)
                    .OrderBy(l => l.ThoiGianThayDoi)
                    .ToListAsync();
                var dict = histories
                    .GroupBy(x => x.TrangThaiMoi)
                    .ToDictionary(g => g.Key, g => g.Max(x => x.ThoiGianThayDoi));
                ViewBag.LichSu = dict;
            }
            catch { ViewBag.LichSu = new Dictionary<int, DateTime>(); }

            return View(orderViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var order = await _context.HoaDons
                    .FirstOrDefaultAsync(h => h.HoaDonId == orderId && h.TaiKhoanId == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ cho phép hủy đơn hàng khi trạng thái là Chờ duyệt (0)
                if (order.TrangThai != 0)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng ở trạng thái này" });
                }

                var oldStatus = order.TrangThai;
                order.TrangThai = 4; // Đã hủy
                order.NgayCapNhat = DateTime.Now;

                // Ghi lịch sử trạng thái (an toàn nếu chưa có bảng)
                try
                {
                    _context.LichSuTrangThaiHoaDons.Add(new LichSuTrangThaiHoaDon
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = order.HoaDonId,
                        TrangThaiCu = oldStatus,
                        TrangThaiMoi = 4,
                        ThoiGianThayDoi = DateTime.Now,
                        GhiChu = "Người dùng hủy đơn"
                    });
                }
                catch { }

                // Hoàn lại số lượng tồn kho
                var orderDetails = await _context.HoaDonChiTiets
                    .Where(hd => hd.HoaDonId == orderId)
                    .ToListAsync();

                foreach (var detail in orderDetails)
                {
                    var product = await _context.SanPhamChiTiets
                        .FirstOrDefaultAsync(sp => sp.SanPhamChiTietId == detail.SanPhamChiTietId);
                    
                    if (product != null)
                    {
                        product.SoLuongTon += detail.SoLuong;
                    }
                }

                await _context.SaveChangesAsync();

                // Email admin notification
                try
                {
                    var cfg = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                    var adminEmail = cfg?["EmailSettings:AdminEmail"] ?? cfg?["EmailSettings:SenderEmail"];
                    string customerEmail = (await _context.TaiKhoans.Where(t=>t.TaiKhoanId==userId).Select(t=>t.Email).FirstOrDefaultAsync()) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(adminEmail))
                    {
                        var html = $@"<div style='font-family:Segoe UI,Arial,sans-serif'>
                            <div style='background:#7a9470;color:#fff;padding:12px;border-radius:8px 8px 0 0'>Thông báo hủy đơn</div>
                            <div style='border:1px solid #e6efe6;border-top:none;padding:16px;border-radius:0 0 8px 8px'>
                                <p>Khách hàng <strong>{order.TenKhachHang}</strong> đã hủy đơn <strong>#{order.HoaDonId}</strong>.</p>
                                <p>Email: {(string.IsNullOrWhiteSpace(customerEmail) ? order.EmailKhachHang : customerEmail)}</p>
                                <p>Tổng tiền: <strong>{order.TongTien:n0} đ</strong></p>
                            </div></div>";
                        await SendEmailAsync(adminEmail, $"[PhuLieuToc] Huỷ đơn #{order.HoaDonId}", html, true);
                    }
                }
                catch { }

                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false });

            try
            {
                var totalOrders = await _context.HoaDons
                    .Where(h => h.TaiKhoanId == userId)
                    .CountAsync();

                var totalSpent = await _context.HoaDons
                    .Where(h => h.TaiKhoanId == userId && h.TrangThai != 4) // Không tính đơn hàng đã hủy
                    .SumAsync(h => h.TongTien);

                return Json(new { success = true, totalOrders, totalSpent });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDefaultAddress()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false });

            try
            {
                var address = await _context.DiaChiGiaoHangs
                    .FirstOrDefaultAsync(a => a.TaiKhoanId == userId && a.LaMacDinh && a.TrangThai == 1);

                if (address != null)
                {
                    return Json(new { 
                        success = true, 
                        address = new {
                            id = address.Id,
                            hoTen = address.HoTen,
                            soDienThoai = address.SoDienThoai,
                            diaChiDayDu = address.DiaChiDayDu,
                            ghiChu = address.GhiChu
                        }
                    });
                }

                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        }
    }
}
