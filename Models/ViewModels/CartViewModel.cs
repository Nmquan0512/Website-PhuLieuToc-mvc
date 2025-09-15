using PhuLieuToc.Models;

namespace PhuLieuToc.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount => Items.Sum(x => x.ThanhTien);
        public int TotalItems => Items.Sum(x => x.SoLuong);
    }

    public class CartItemViewModel
    {
        public int GioHangChiTietId { get; set; }
        public int SanPhamChiTietId { get; set; }
        public string TenSanPham { get; set; }
        public string? Anh { get; set; }
        public string? ThuongHieu { get; set; }
        public string? DanhMuc { get; set; }
        public string? ThuocTinh { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
        public int SoLuongTon { get; set; }
    }

    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new CartViewModel();
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public string GhiChu { get; set; }
        public string PhuongThucThanhToan { get; set; } = "COD"; // COD hoặc VNPay
    }

    public class OrderStatusViewModel
    {
        public Guid HoaDonId { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public decimal TongTien { get; set; }
        public int TrangThai { get; set; }
        public string TrangThaiText => GetTrangThaiText(TrangThai);
        public string PhuongThucThanhToan { get; set; }
        public DateTime NgayTao { get; set; }
        public List<HoaDonChiTiet> ChiTietHoaDon { get; set; } = new List<HoaDonChiTiet>();

        private string GetTrangThaiText(int trangThai)
        {
            return trangThai switch
            {
                0 => "Chờ duyệt",
                1 => "Đã duyệt",
                2 => "Đang giao",
                3 => "Đã giao",
                4 => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}
