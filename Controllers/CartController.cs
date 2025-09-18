using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Models.ViewModels;
using PhuLieuToc.Repository;
using System.Security.Claims;
using System.Text.Json;

namespace PhuLieuToc.Controllers
{
	public class CartController : Controller
	{
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var cart = GetCartFromSession();
                return View(cart);
            }

            var cartDb = await GetCartViewModel(userId.Value);
            return View(cartDb);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int sanPhamChiTietId, int soLuong = 1, string? selectedThuocTinh = null)
        {
            var userId = GetCurrentUserId();

            try
            {
                var sanPham = await _context.SanPhamChiTiets
                    .Include(s => s.SanPham)
                    .FirstOrDefaultAsync(s => s.SanPhamChiTietId == sanPhamChiTietId && s.TrangThai == 1);

                if (sanPham == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });

                if (sanPham.SoLuongTon < soLuong)
                    return Json(new { success = false, message = "Số lượng không đủ" });

                if (userId == null)
                {
                    var cart = GetCartFromSession();
                    var existing = cart.Items.FirstOrDefault(i => i.SanPhamChiTietId == sanPhamChiTietId && (
                        string.IsNullOrWhiteSpace(selectedThuocTinh) || string.Equals(i.ThuocTinh, selectedThuocTinh, StringComparison.OrdinalIgnoreCase)));
                    if (existing != null)
                    {
                        existing.SoLuong += soLuong;
                        if (!string.IsNullOrWhiteSpace(selectedThuocTinh)) existing.ThuocTinh = selectedThuocTinh;
                    }
                    else
                    {
                        var thuocTinh = string.IsNullOrWhiteSpace(selectedThuocTinh)
                            ? string.Join(", ", _context.SanPhamChiTietThuocTinhs
                                .Where(t => t.SanPhamChiTietId == sanPhamChiTietId)
                                .Include(t => t.GiaTriThuocTinh)
                                    .ThenInclude(g => g.ThuocTinh)
                                .Select(t => t.GiaTriThuocTinh.TenGiaTri))
                            : selectedThuocTinh;

                        cart.Items.Add(new CartItemViewModel
                        {
                            GioHangChiTietId = sanPhamChiTietId,
                            SanPhamChiTietId = sanPhamChiTietId,
                            TenSanPham = sanPham.SanPham.TenSanPham,
                            Anh = sanPham.Anh,
                            ThuongHieu = sanPham.SanPham.Brand?.TenThuongHieu,
                            DanhMuc = sanPham.SanPham.Category?.TenDanhMuc,
                            ThuocTinh = thuocTinh,
                            DonGia = sanPham.Gia,
                            SoLuong = soLuong,
                            SoLuongTon = sanPham.SoLuongTon
                        });
                    }
                    SaveCartToSession(cart);
                    // Store selected attributes for guests as well
                    if (!string.IsNullOrWhiteSpace(selectedThuocTinh))
                    {
                        var map = GetCartSelectedAttrMap();
                        map[sanPhamChiTietId] = selectedThuocTinh;
                        SaveCartSelectedAttrMap(map);
                    }
                    return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
                }
                else
                {
                    var gioHang = await _context.GioHangs
                        .FirstOrDefaultAsync(g => g.TaiKhoanId == userId);

                    if (gioHang == null)
                    {
                        gioHang = new GioHang { TaiKhoanId = userId.Value };
                        _context.GioHangs.Add(gioHang);
                        await _context.SaveChangesAsync();
                    }

                    // Find existing row that matches both product and selected attributes (if provided)
                    var sameProductItems = await _context.GioHangChiTiets
                        .Where(g => g.GioHangId == gioHang.GioHangId && g.SanPhamChiTietId == sanPhamChiTietId)
                        .ToListAsync();
                    GioHangChiTiet existingItem = null;
                    if (!string.IsNullOrWhiteSpace(selectedThuocTinh))
                    {
                        var rowMap = GetCartSelectedAttrMapByRow();
                        existingItem = sameProductItems.FirstOrDefault(it => rowMap.TryGetValue(it.GioHangChiTietId, out var picked)
                            && string.Equals((picked ?? "").Trim(), selectedThuocTinh.Trim(), StringComparison.OrdinalIgnoreCase));
                        // If no exact attribute match, do NOT merge → keep existingItem null to create new row
                    }
                    else
                    {
                        // No attribute text provided → safe to merge into any existing row for the product
                        existingItem = sameProductItems.FirstOrDefault();
                    }

                    int insertedRowId = 0;
                    if (existingItem != null)
                    {
                        existingItem.SoLuong += soLuong;
                        existingItem.ThanhTien = existingItem.SoLuong * existingItem.DonGia;
                    }
                    else
                    {
                        var newItem = new GioHangChiTiet
                        {
                            GioHangId = gioHang.GioHangId,
                            SanPhamChiTietId = sanPhamChiTietId,
                            SoLuong = soLuong,
                            DonGia = sanPham.Gia,
                            ThanhTien = sanPham.Gia * soLuong
                        };
                        _context.GioHangChiTiets.Add(newItem);
                        await _context.SaveChangesAsync();
                        insertedRowId = newItem.GioHangChiTietId;
                    }
                    if (existingItem != null)
                    {
                        await _context.SaveChangesAsync();
                    }
                    // persist chosen attributes too
                    if (!string.IsNullOrWhiteSpace(selectedThuocTinh))
                    {
                        var rowMap = GetCartSelectedAttrMapByRow();
                        var rowId = existingItem?.GioHangChiTietId > 0 ? existingItem.GioHangChiTietId : insertedRowId;
                        if (rowId > 0) { rowMap[rowId] = selectedThuocTinh; SaveCartSelectedAttrMapByRow(rowMap); }
                    }
                    return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int gioHangChiTietId, int soLuong)
        {
            var userId = GetCurrentUserId();

            try
            {
                var item = await _context.GioHangChiTiets
                    .Include(g => g.GioHang)
                    .FirstOrDefaultAsync(g => g.GioHangChiTietId == gioHangChiTietId && g.GioHang.TaiKhoanId == userId);

                if (item == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng" });

                if (soLuong <= 0)
                {
                    _context.GioHangChiTiets.Remove(item);
                }
                else
                {
                    item.SoLuong = soLuong;
                    item.ThanhTien = item.SoLuong * item.DonGia;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int gioHangChiTietId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var cart = GetCartFromSession();
                var item = cart.Items.FirstOrDefault(i => i.SanPhamChiTietId == gioHangChiTietId);
                if (item != null)
                {
                    cart.Items.Remove(item);
                    SaveCartToSession(cart);
                }
                return Json(new { success = true });
            }

            try
            {
                var item = await _context.GioHangChiTiets
                    .Include(g => g.GioHang)
                    .FirstOrDefaultAsync(g => g.GioHangChiTietId == gioHangChiTietId && g.GioHang.TaiKhoanId == userId);

                if (item != null)
                {
                    _context.GioHangChiTiets.Remove(item);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            var cart = userId == null ? GetCartFromSession() : await GetCartViewModel(userId.Value);
            if (!cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            var checkoutViewModel = new CheckoutViewModel
            {
                Cart = cart
            };

            return View(checkoutViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                model.Cart = userId == null ? GetCartFromSession() : await GetCartViewModel(userId.Value);
                return View(model);
            }

            try
            {
                var cart = userId == null ? GetCartFromSession() : await GetCartViewModel(userId.Value);
                if (!cart.Items.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(model.TenKhachHang) || string.IsNullOrWhiteSpace(model.SoDienThoai) || string.IsNullOrWhiteSpace(model.DiaChiGiaoHang))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ Họ tên, SĐT và Địa chỉ giao hàng.");
                    model.Cart = cart;
                    return View(model);
                }

                // Nếu khách chưa đăng nhập, validate email không trùng email tài khoản
                if (userId == null)
                {
                    if (string.IsNullOrWhiteSpace(model.EmailKhachHang))
                    {
                        ModelState.AddModelError("EmailKhachHang", "Vui lòng nhập email để nhận thông báo đơn hàng.");
                        model.Cart = cart; return View(model);
                    }

                    var isTaken = await _context.TaiKhoans.AnyAsync(t => t.Email == model.EmailKhachHang.Trim());
                    if (isTaken)
                    {
                        ModelState.AddModelError("", "Email đã tồn tại trong hệ thống. Vui lòng dùng email khác hoặc đăng nhập.");
                        model.Cart = cart;
                        return View(model);
                    }
                }

                var hoaDon = new HoaDon
                {
                    HoaDonId = Guid.NewGuid(),
                    TenKhachHang = model.TenKhachHang,
                    SoDienThoai = model.SoDienThoai,
                    DiaChiGiaoHang = model.DiaChiGiaoHang,
                    TongTien = cart.TotalAmount,
                    TrangThai = 0,
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TaiKhoanId = userId,
                    EmailKhachHang = userId == null ? (string.IsNullOrWhiteSpace(model.EmailKhachHang) ? null : model.EmailKhachHang.Trim()) : null
                };

                _context.HoaDons.Add(hoaDon);

                // Ghi lịch sử trạng thái tạo mới (bỏ qua nếu bảng chưa có migration)
                try
                {
                    _context.LichSuTrangThaiHoaDons.Add(new LichSuTrangThaiHoaDon
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = hoaDon.HoaDonId,
                        TrangThaiCu = -1,
                        TrangThaiMoi = 0,
                        ThoiGianThayDoi = DateTime.Now,
                        GhiChu = "Tạo đơn hàng"
                    });
                }
                catch { /* ignore when table doesn't exist yet */ }

                foreach (var item in cart.Items)
                {
                    var sanPham = await _context.SanPhamChiTiets
                        .Include(s => s.SanPham)
                            .ThenInclude(p => p.Brand)
                        .Include(s => s.SanPhamChiTietThuocTinhs)
                            .ThenInclude(t => t.GiaTriThuocTinh)
                                .ThenInclude(g => g.ThuocTinh)
                        .FirstOrDefaultAsync(s => s.SanPhamChiTietId == item.SanPhamChiTietId);

                    if (sanPham != null)
                    {
                        // Prefer the exact attributes selected by user at add-to-cart time
                        var thuocTinh = !string.IsNullOrWhiteSpace(item.ThuocTinh)
                            ? item.ThuocTinh
                            : string.Join(", ", sanPham.SanPhamChiTietThuocTinhs
                                .Select(t => $"{t.GiaTriThuocTinh.ThuocTinh.TenThuocTinh}: {t.GiaTriThuocTinh.TenGiaTri}"));
                        if (thuocTinh.Length > 95) thuocTinh = thuocTinh.Substring(0, 95);

                        var chiTiet = new HoaDonChiTiet
                        {
                            HoaDonChiTietId = Guid.NewGuid(),
                            HoaDonId = hoaDon.HoaDonId,
                            SanPhamChiTietId = item.SanPhamChiTietId,
                            HinhAnhLucMua = item.Anh,
                            TenSanPhamLucMua = item.TenSanPham,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia,
                            ThanhTien = item.ThanhTien,
                            LoaiThuocTinhLucMua = thuocTinh,
                            ThuongHieuLucMua = item.ThuongHieu
                        };

                        _context.HoaDonChiTiets.Add(chiTiet);
                        // Chỉ trừ tồn ngay đối với COD. Với VNPay, sẽ trừ sau khi thanh toán thành công
                        if (!string.Equals(model.PhuongThucThanhToan, "VNPay", StringComparison.OrdinalIgnoreCase))
                        {
                            sanPham.SoLuongTon -= item.SoLuong;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Chỉ xoá giỏ hàng nếu là COD. Với VNPay, giữ giỏ cho đến khi thanh toán thành công
                if (!string.Equals(model.PhuongThucThanhToan, "VNPay", StringComparison.OrdinalIgnoreCase))
                {
                    if (userId == null)
                    {
                        HttpContext.Session.Remove("CART");
                    }
                    else
                    {
                        var gioHang = await _context.GioHangs
                            .Include(g => g.GioHangChiTiets)
                            .FirstOrDefaultAsync(g => g.TaiKhoanId == userId);

                        if (gioHang != null)
                        {
                            _context.GioHangChiTiets.RemoveRange(gioHang.GioHangChiTiets);
                            _context.GioHangs.Remove(gioHang);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                // Email admin notification chỉ dành cho COD. VNPay sẽ gửi sau khi thanh toán thành công
                try
                {
                    if (!string.Equals(model.PhuongThucThanhToan, "VNPay", StringComparison.OrdinalIgnoreCase))
                    {
                        var cfg = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                        var adminEmail = cfg?["EmailSettings:AdminEmail"] ?? cfg?["EmailSettings:SenderEmail"];
                        if (!string.IsNullOrWhiteSpace(adminEmail))
                        {
                            var htmlTemplate = CreateOrderNotificationEmailTemplate(hoaDon);
                            _ = System.Threading.Tasks.Task.Run(async () =>
                            {
                                try { await SendEmailAsync(adminEmail, $"[Linh Trang - Phụ Liệu Tóc] 🛍️ Đơn hàng mới #{hoaDon.HoaDonId}", htmlTemplate); } catch { }
                            });
                        }
                    }
                }
                catch { /* best effort notification */ }

                // Email cảm ơn gửi cho khách chỉ dành cho COD
                try
                {
                    if (!string.Equals(model.PhuongThucThanhToan, "VNPay", StringComparison.OrdinalIgnoreCase))
                    {
                        string customerEmail = userId != null
                            ? (await _context.TaiKhoans.Where(t => t.TaiKhoanId == userId).Select(t => t.Email).FirstOrDefaultAsync())
                            : hoaDon.EmailKhachHang;
                        if (!string.IsNullOrWhiteSpace(customerEmail))
                        {
                            var items = await _context.HoaDonChiTiets.Where(x => x.HoaDonId == hoaDon.HoaDonId).ToListAsync();
                            var rows = string.Join("", items.Select(c => $"<tr><td style='padding:8px 12px'>{c.TenSanPhamLucMua}</td><td style='padding:8px 12px;text-align:center'>{c.SoLuong}</td><td style='text-align:right;padding:8px 12px'>{c.DonGia:n0} đ</td><td style='text-align:right;padding:8px 12px'>{c.ThanhTien:n0} đ</td></tr>"));
                            var html = $@"<div style='font-family:Segoe UI,Arial,sans-serif'>
                                <div style='background:#7a9470;color:#fff;padding:14px;border-radius:8px 8px 0 0'>
                                    <h3 style='margin:0'>Cảm ơn bạn đã đặt hàng</h3>
                                </div>
                                <div style='border:1px solid #e6efe6;border-top:none;padding:16px;border-radius:0 0 8px 8px'>
                                    <p>Xin chào <strong>{hoaDon.TenKhachHang}</strong>, chúng tôi đã nhận được đơn hàng <strong>#{hoaDon.HoaDonId}</strong>.</p>
                                    <p><strong>Thông tin giao hàng:</strong><br/>SĐT: {hoaDon.SoDienThoai}<br/>Địa chỉ: {hoaDon.DiaChiGiaoHang}</p>
                                    <table style='width:100%;border-collapse:collapse;background:#fafdf9;border:1px solid #e6efe6'>
                                        <thead><tr style='background:#eef5ea'><th style='text-align:left;padding:8px 12px'>Sản phẩm</th><th style='padding:8px 12px'>SL</th><th style='text-align:right;padding:8px 12px'>Đơn giá</th><th style='text-align:right;padding:8px 12px'>Thành tiền</th></tr></thead>
                                        <tbody>{rows}</tbody>
                                        <tfoot><tr><td colspan='3' style='padding:8px 12px;text-align:right'><strong>Tổng cộng:</strong></td><td style='padding:8px 12px;text-align:right'><strong>{hoaDon.TongTien:n0} đ</strong></td></tr></tfoot>
                                    </table>
                                    <p style='margin-top:12px'>Cảm ơn bạn đã tin tưởng Phụ Liệu Tóc.</p>
                                </div></div>";
                            _ = System.Threading.Tasks.Task.Run(async () => { try { await SendEmailAsync(customerEmail, $"Xác nhận đơn hàng #{hoaDon.HoaDonId}", html); } catch { } });
                        }
                    }
                }
                catch { }

                // Redirect based on payment method
                if (string.Equals(model.PhuongThucThanhToan, "VNPay", StringComparison.OrdinalIgnoreCase))
                {
                    var vnp = HttpContext.RequestServices.GetRequiredService<PhuLieuToc.Service.VNPayService>();
                    var payUrl = vnp.CreatePaymentUrl(hoaDon.HoaDonId, hoaDon.TongTien, $"Thanh toan don hang #{hoaDon.HoaDonId.ToString().Substring(0,8)}");
                    return Redirect(payUrl);
                }

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderSuccess", new { orderId = hoaDon.HoaDonId });
            }
            catch (Exception ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng: " + details);
                model.Cart = userId == null ? GetCartFromSession() : await GetCartViewModel(userId.Value);
                return View(model);
            }
        }

        public async Task<IActionResult> OrderSuccess(Guid orderId)
        {
            var order = await _context.HoaDons
                .Include(h => h.HoaDonChiTiets)
                .FirstOrDefaultAsync(h => h.HoaDonId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Index");
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

            return View(orderViewModel);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                var sessionCart = GetCartFromSession();
                return Json(new { success = true, count = sessionCart.TotalItems });
            }

            try
            {
                var gioHang = await _context.GioHangs
                    .Include(g => g.GioHangChiTiets)
                    .FirstOrDefaultAsync(g => g.TaiKhoanId == userId);

                var count = gioHang?.GioHangChiTiets.Sum(item => item.SoLuong) ?? 0;
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, count = 0, error = ex.Message });
            }
        }

        private async Task<CartViewModel> GetCartViewModel(int userId)
        {
            var gioHang = await _context.GioHangs
                .Include(g => g.GioHangChiTiets)
                    .ThenInclude(gc => gc.SanPhamChiTiet)
                        .ThenInclude(sp => sp.SanPham)
                            .ThenInclude(p => p.Brand)
                .Include(g => g.GioHangChiTiets)
                    .ThenInclude(gc => gc.SanPhamChiTiet)
                        .ThenInclude(sp => sp.SanPham)
                            .ThenInclude(p => p.Category)
                .Include(g => g.GioHangChiTiets)
                    .ThenInclude(gc => gc.SanPhamChiTiet)
                        .ThenInclude(sp => sp.SanPhamChiTietThuocTinhs)
                            .ThenInclude(t => t.GiaTriThuocTinh)
                .FirstOrDefaultAsync(g => g.TaiKhoanId == userId);

            var cartViewModel = new CartViewModel();

            if (gioHang != null)
            {
                var rowMap = GetCartSelectedAttrMapByRow();
                foreach (var item in gioHang.GioHangChiTiets)
                {
                    // Prefer user-selected attributes captured for this specific row
                    var thuocTinh = rowMap.TryGetValue(item.GioHangChiTietId, out var picked)
                        ? picked
                        : string.Join(", ", item.SanPhamChiTiet.SanPhamChiTietThuocTinhs
                            .Select(t => t.GiaTriThuocTinh.TenGiaTri));

                    cartViewModel.Items.Add(new CartItemViewModel
                    {
                        GioHangChiTietId = item.GioHangChiTietId,
                        SanPhamChiTietId = item.SanPhamChiTietId,
                        TenSanPham = item.SanPhamChiTiet.SanPham.TenSanPham,
                        Anh = item.SanPhamChiTiet.Anh,
                        ThuongHieu = item.SanPhamChiTiet.SanPham.Brand?.TenThuongHieu,
                        DanhMuc = item.SanPhamChiTiet.SanPham.Category?.TenDanhMuc,
                        ThuocTinh = thuocTinh,
                        DonGia = item.DonGia,
                        SoLuong = item.SoLuong,
                        SoLuongTon = item.SanPhamChiTiet.SoLuongTon
                    });
                }
            }

            return cartViewModel;
        }

        private Dictionary<int,string> GetCartSelectedAttrMap()
        {
            var json = HttpContext.Session.GetString("CART_ATTR_MAP");
            if (string.IsNullOrEmpty(json)) return new Dictionary<int, string>();
            try { return System.Text.Json.JsonSerializer.Deserialize<Dictionary<int,string>>(json) ?? new Dictionary<int,string>(); }
            catch { return new Dictionary<int, string>(); }
        }

        private void SaveCartSelectedAttrMap(Dictionary<int,string> map)
        {
            HttpContext.Session.SetString("CART_ATTR_MAP", System.Text.Json.JsonSerializer.Serialize(map));
        }

        private Dictionary<int,string> GetCartSelectedAttrMapByRow()
        {
            var json = HttpContext.Session.GetString("CART_ATTR_MAP_ROW");
            if (string.IsNullOrEmpty(json)) return new Dictionary<int, string>();
            try { return System.Text.Json.JsonSerializer.Deserialize<Dictionary<int,string>>(json) ?? new Dictionary<int,string>(); }
            catch { return new Dictionary<int, string>(); }
        }

        private void SaveCartSelectedAttrMapByRow(Dictionary<int,string> map)
        {
            HttpContext.Session.SetString("CART_ATTR_MAP_ROW", System.Text.Json.JsonSerializer.Serialize(map));
        }

        private CartViewModel GetCartFromSession()
        {
            var json = HttpContext.Session.GetString("CART");
            if (string.IsNullOrEmpty(json)) return new CartViewModel();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<CartViewModel>(json) ?? new CartViewModel();
            }
            catch
            {
                return new CartViewModel();
            }
        }

        private void SaveCartToSession(CartViewModel cart)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("CART", json);
        }

        private async Task<int> GetOrCreateGuestAccountId()
        {
            var guest = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == "guest");
            if (guest == null)
            {
                guest = new TaiKhoan
                {
                    TenDangNhap = "guest",
                    Email = "guest@local",
                    MatKhau = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    TrangThai = true,
                    VaiTro = "User"
                };
                _context.TaiKhoans.Add(guest);
                await _context.SaveChangesAsync();
            }
            return guest.TaiKhoanId;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
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
                var mail = new System.Net.Mail.MailMessage(new System.Net.Mail.MailAddress(user!, display), new System.Net.Mail.MailAddress(toEmail)) { Subject = subject, Body = body, IsBodyHtml = true };
                await client.SendMailAsync(mail);
            }
            catch { }
        }

        private string CreateOrderNotificationEmailTemplate(HoaDon hoaDon)
{
    var sb = new System.Text.StringBuilder();
    
    // Chi tiết sản phẩm
    var chiTietSanPham = new System.Text.StringBuilder();
    foreach (var c in _context.HoaDonChiTiets.Where(x => x.HoaDonId == hoaDon.HoaDonId))
    {
        chiTietSanPham.Append($@"
            <tr>
                <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; font-weight: 500; color: #374151;'>
                    {c.TenSanPhamLucMua}
                </td>
                <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: center; color: #6b7280;'>
                    x{c.SoLuong}
                </td>
                <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right; color: #6b7280;'>
                    {c.DonGia:n0} đ
                </td>
                <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 600; color: #374151;'>
                    {c.ThanhTien:n0} đ
                </td>
            </tr>");
        
        if (!string.IsNullOrWhiteSpace(c.LoaiThuocTinhLucMua))
        {
            chiTietSanPham.Append($@"
            <tr>
                <td colspan='4' style='padding: 0 12px 8px 12px; font-size: 13px; color: #6b7280; font-style: italic;'>
                    Thuộc tính: {c.LoaiThuocTinhLucMua}
                </td>
            </tr>");
        }
    }

    sb.Append($@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Đơn hàng mới #{hoaDon.HoaDonId}</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f9fafb; line-height: 1.6;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #6b835f 0%, #8fa876 100%); padding: 30px 20px; text-align: center;'>
            <h1 style='margin: 0; color: #ffffff; font-size: 28px; font-weight: bold; text-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                🛍️ Đơn Hàng Mới
            </h1>
            <p style='margin: 8px 0 0 0; color: #f0f9ff; font-size: 16px; opacity: 0.9;'>
                Linh Trang - Phụ Liệu Tóc
            </p>
        </div>

        <!-- Order Info -->
        <div style='padding: 30px 20px;'>
            <div style='background-color: #f8fffe; border: 2px solid #6b835f; border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
                <h2 style='margin: 0 0 15px 0; color: #6b835f; font-size: 20px; font-weight: bold;'>
                    📋 Thông Tin Đơn Hàng
                </h2>
                <div style='display: flex; flex-wrap: wrap; gap: 10px;'>
                    <div style='background-color: #ffffff; padding: 15px; border-radius: 6px; flex: 1; min-width: 200px; border-left: 4px solid #6b835f;'>
                        <p style='margin: 0; font-size: 14px; color: #6b7280;'>Mã đơn hàng</p>
                        <p style='margin: 5px 0 0 0; font-size: 18px; font-weight: bold; color: #6b835f;'>#{hoaDon.HoaDonId}</p>
                    </div>
                    <div style='background-color: #ffffff; padding: 15px; border-radius: 6px; flex: 1; min-width: 200px; border-left: 4px solid #8fa876;'>
                        <p style='margin: 0; font-size: 14px; color: #6b7280;'>Ngày đặt</p>
                        <p style='margin: 5px 0 0 0; font-size: 16px; font-weight: 600; color: #374151;'>{DateTime.Now:dd/MM/yyyy HH:mm}</p>
                    </div>
                </div>
            </div>

            <!-- Customer Info -->
            <div style='margin-bottom: 25px;'>
                <h3 style='margin: 0 0 15px 0; color: #374151; font-size: 18px; font-weight: bold; border-bottom: 2px solid #6b835f; padding-bottom: 8px;'>
                    👤 Thông Tin Khách Hàng
                </h3>
                <div style='background-color: #f9fafb; padding: 20px; border-radius: 8px; border: 1px solid #e5e7eb;'>
                    <div style='margin-bottom: 12px;'>
                        <span style='display: inline-block; width: 120px; font-weight: 600; color: #6b835f;'>Tên khách hàng:</span>
                        <span style='color: #374151; font-weight: 500;'>{hoaDon.TenKhachHang}</span>
                    </div>
                    <div style='margin-bottom: 12px;'>
                        <span style='display: inline-block; width: 120px; font-weight: 600; color: #6b835f;'>Số điện thoại:</span>
                        <span style='color: #374151; font-weight: 500;'>{hoaDon.SoDienThoai}</span>
                    </div>

                    <div style='margin-bottom: 0;'>
                        <span style='display: inline-block; width: 120px; font-weight: 600; color: #6b835f; vertical-align: top;'>Địa chỉ:</span>
                        <span style='color: #374151; font-weight: 500;'>{hoaDon.DiaChiGiaoHang}</span>
                    </div>
                </div>
            </div>

            <!-- Product Details -->
            <div style='margin-bottom: 25px;'>
                <h3 style='margin: 0 0 15px 0; color: #374151; font-size: 18px; font-weight: bold; border-bottom: 2px solid #6b835f; padding-bottom: 8px;'>
                    🛒 Chi Tiết Sản Phẩm
                </h3>
                <div style='background-color: #ffffff; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #6b835f;'>
                                <th style='padding: 15px 12px; text-align: left; color: #ffffff; font-weight: 600; font-size: 14px;'>Sản phẩm</th>
                                <th style='padding: 15px 12px; text-align: center; color: #ffffff; font-weight: 600; font-size: 14px;'>SL</th>
                                <th style='padding: 15px 12px; text-align: right; color: #ffffff; font-weight: 600; font-size: 14px;'>Đơn giá</th>
                                <th style='padding: 15px 12px; text-align: right; color: #ffffff; font-weight: 600; font-size: 14px;'>Thành tiền</th>
                            </tr>
                        </thead>
                        <tbody>
                            {chiTietSanPham}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Total -->
            <div style='background: linear-gradient(135deg, #6b835f 0%, #8fa876 100%); padding: 20px; border-radius: 8px; text-align: center; margin-bottom: 25px;'>
                <p style='margin: 0 0 5px 0; color: #f0f9ff; font-size: 16px; opacity: 0.9;'>Tổng thanh toán</p>
                <p style='margin: 0; color: #ffffff; font-size: 28px; font-weight: bold; text-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    {hoaDon.TongTien:n0} đ
                </p>
            </div>

            <!-- Action Required -->
            <div style='background-color: #fef3c7; border: 2px solid #f59e0b; border-radius: 8px; padding: 20px; text-align: center;'>
                <h4 style='margin: 0 0 10px 0; color: #92400e; font-size: 16px; font-weight: bold;'>
                    ⚠️ Cần Xử Lý Ngay
                </h4>
                <p style='margin: 0; color: #92400e; font-size: 14px; line-height: 1.5;'>
                    Đơn hàng mới cần được xác nhận và chuẩn bị giao hàng. Vui lòng đăng nhập hệ thống để xử lý.
                </p>
            </div>
        </div>

        <!-- Footer -->
        <div style='background-color: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb;'>
            <p style='margin: 0 0 10px 0; color: #6b7280; font-size: 14px;'>
                Email này được gửi tự động từ hệ thống
            </p>
            <p style='margin: 0; color: #6b835f; font-weight: 600; font-size: 16px;'>
                Linh Trang - Phụ Liệu Tóc 💇‍♀️
            </p>
        </div>
    </div>
</body>
</html>");

    return sb.ToString();
}
    }
}
