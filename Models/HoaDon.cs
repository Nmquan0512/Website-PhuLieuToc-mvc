using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class HoaDon
    {
        [Key]
        public Guid HoaDonId { get; set; }

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự")]
        public string TenKhachHang { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        [StringLength(500, ErrorMessage = "Địa chỉ giao hàng không được vượt quá 500 ký tự")]
        public string DiaChiGiaoHang { get; set; }

        [Required(ErrorMessage = "Tổng tiền là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Required(ErrorMessage = "Tổng tiền sau giảm giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền sau giảm giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTienSauGiamGia { get; set; }

        [StringLength(200)]
        public string? ThongTinVoucher { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
        public string TrangThai { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? NgayCapNhat { get; set; }

        [StringLength(50)]
        public string? PhuongThucThanhToan { get; set; }

        [Required]
        [ForeignKey("TaiKhoan")]
        public int TaiKhoanId { get; set; }
        public TaiKhoan TaiKhoan { get; set; }

        public ICollection<HoaDonChiTiet> HoaDonChiTiets { get; set; } = new List<HoaDonChiTiet>();
    }
}
