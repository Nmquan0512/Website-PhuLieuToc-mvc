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

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        public string DiaChiDayDu { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string MoTa { get; set; }

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
