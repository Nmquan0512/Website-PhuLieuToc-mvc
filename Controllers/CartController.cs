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
            if (userId == null) return RedirectToAction("Index", "Login");

            var cart = await GetCartViewModel(userId.Value);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int sanPhamChiTietId, int soLuong = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var sanPham = await _context.SanPhamChiTiets
                    .Include(s => s.SanPham)
                    .FirstOrDefaultAsync(s => s.SanPhamChiTietId == sanPhamChiTietId && s.TrangThai == 1);

                if (sanPham == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });

                if (sanPham.SoLuongTon < soLuong)
                    return Json(new { success = false, message = "Số lượng không đủ" });

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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int gioHangChiTietId, int soLuong)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

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
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

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
            if (userId == null) return RedirectToAction("Index", "Login");

            var cart = await GetCartViewModel(userId.Value);
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
            if (userId == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                model.Cart = await GetCartViewModel(userId.Value);
                return View(model);
            }

            try
            {
                var cart = await GetCartViewModel(userId.Value);
                if (!cart.Items.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Index");
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
                    TaiKhoanId = userId.Value
                };

                _context.HoaDons.Add(hoaDon);

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

                var gioHang = await _context.GioHangs
                    .Include(g => g.GioHangChiTiets)
                    .FirstOrDefaultAsync(g => g.TaiKhoanId == userId);

                if (gioHang != null)
                {
                    _context.GioHangChiTiets.RemoveRange(gioHang.GioHangChiTiets);
                    _context.GioHangs.Remove(gioHang);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("OrderSuccess", new { orderId = hoaDon.HoaDonId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng: " + ex.Message);
                model.Cart = await GetCartViewModel(userId.Value);
                return View(model);
            }
        }

        public async Task<IActionResult> OrderSuccess(Guid orderId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var order = await _context.HoaDons
                .Include(h => h.HoaDonChiTiets)
                .FirstOrDefaultAsync(h => h.HoaDonId == orderId && h.TaiKhoanId == userId);

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
            if (userId == null) return Json(new { success = false, count = 0 });

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
	}
}
