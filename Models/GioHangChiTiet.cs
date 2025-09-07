using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class GioHangChiTiet
    {
        [Key]
        public int GioHangChiTietId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
        public decimal DonGia { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Thành tiền phải lớn hơn hoặc bằng 0")]
        public decimal ThanhTien { get; set; } 

        [Required]
        [ForeignKey(nameof(GioHang))]
        public int GioHangId { get; set; }
        public GioHang GioHang { get; set; }

        [Required]
        [ForeignKey(nameof(SanPhamChiTiet))]
        public int SanPhamChiTietId { get; set; }
        public SanPhamChiTiet SanPhamChiTiet { get; set; }
    }
}
