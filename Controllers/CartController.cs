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
        public async Task<IActionResult> AddToCart(int sanPhamChiTietId, int soLuong = 1)
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
                    var existing = cart.Items.FirstOrDefault(i => i.SanPhamChiTietId == sanPhamChiTietId);
                    if (existing != null)
                    {
                        existing.SoLuong += soLuong;
                    }
                    else
                    {
                        var thuocTinh = string.Join(", ", _context.SanPhamChiTietThuocTinhs
                            .Where(t => t.SanPhamChiTietId == sanPhamChiTietId)
                            .Include(t => t.GiaTriThuocTinh)
                            .Select(t => t.GiaTriThuocTinh.TenGiaTri));

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

                    var existingItem = await _context.GioHangChiTiets
                        .FirstOrDefaultAsync(g => g.GioHangId == gioHang.GioHangId && g.SanPhamChiTietId == sanPhamChiTietId);

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
                    }

                    await _context.SaveChangesAsync();
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

                var hoaDon = new HoaDon
                {
                    HoaDonId = Guid.NewGuid(),
                    TenKhachHang = model.TenKhachHang,
                    SoDienThoai = model.SoDienThoai,
                    DiaChiGiaoHang = model.DiaChiGiaoHang,
                    TongTien = cart.TotalAmount,
                    TrangThai = 0,
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TaiKhoanId = userId
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
                        var thuocTinh = string.Join(", ", sanPham.SanPhamChiTietThuocTinhs
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
                        sanPham.SoLuongTon -= item.SoLuong;
                    }
                }

                await _context.SaveChangesAsync();

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
                foreach (var item in gioHang.GioHangChiTiets)
                {
                    var thuocTinh = string.Join(", ", item.SanPhamChiTiet.SanPhamChiTietThuocTinhs
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
    }
}
