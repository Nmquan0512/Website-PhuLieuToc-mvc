using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    [Table("TaiKhoans")] // tên bảng trong database
    public class TaiKhoan
    {
        [Key] 
        public int TaiKhoanId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Tên đăng nhập phải từ 5 đến 50 ký tự")]
        public string TenDangNhap { get; set; }

		[Required]
		[StringLength(255)]
		public string MatKhau { get; set; } 

		[Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15)]
        public string SoDienThoai { get; set; }

        [Required]
        public bool TrangThai { get; set; } = true; 

        [StringLength(20)]
        public string VaiTro { get; set; } 

        [DataType(DataType.DateTime)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? NgayCapNhat { get; set; }

        public ICollection<DiaChiGiaoHang> DiaChiGiaoHangs { get; set; } = new List<DiaChiGiaoHang>();

        public ICollection<HoaDon>? HoaDons { get; set; } = new List<HoaDon>();

        public ICollection<GioHang>? GioHangs { get; set; } = new List<GioHang>();

        // Password reset OTP snapshot
        [StringLength(10)]
        public string? ResetOtp { get; set; }

        public DateTime? ResetOtpExpiryUtc { get; set; }

        public int? ResetOtpAttempts { get; set; }
    }
}
