using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    [Table("DiaChiGiaoHangs")]
    public class DiaChiGiaoHang
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(50, ErrorMessage = "Họ tên tối đa 50 ký tự")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Địa chỉ cụ thể là bắt buộc")]
        [StringLength(200, ErrorMessage = "Địa chỉ cụ thể tối đa 200 ký tự")]
        public string DiaChiCuThe { get; set; }

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tỉnh/Thành phố tối đa 50 ký tự")]
        public string TinhThanh { get; set; }

        [Required(ErrorMessage = "Xã/Phường là bắt buộc")]
        [StringLength(50, ErrorMessage = "Xã/Phường tối đa 50 ký tự")]
        public string XaPhuong { get; set; }

        // Địa chỉ đầy đủ được tạo từ DiaChiCuThe + XaPhuong + TinhThanh
        public string DiaChiDayDu => $"{DiaChiCuThe}, {XaPhuong}, {TinhThanh}";

        [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự")]
        public string GhiChu { get; set; }

        // dùng int thì bạn có thể để 0 = chưa dùng, 1 = đang dùng, -1 = xóa mềm
        [Required]
        public int TrangThai { get; set; } = 1;

        [Required]
        public bool LaMacDinh { get; set; } = false;

        [Required]
        [ForeignKey("TaiKhoan")]
        public int TaiKhoanId { get; set; }

        public TaiKhoan TaiKhoan { get; set; }
    }
}
