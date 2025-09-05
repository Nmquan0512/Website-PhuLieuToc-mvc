using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class HoaDonChiTiet
    {
        [Key]
        public Guid HoaDonChiTietId { get; set; }

        [StringLength(500)]
        public string? HinhAnhLucMua { get; set; }

        [Required]
        [StringLength(200)]
        public string TenSanPhamLucMua { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        [StringLength(100)]
        public string? LoaiThuocTinhLucMua { get; set; }

        [StringLength(200)]
        public string? GiaTriThuocTinhLucMua { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ThanhTien { get; set; }

        [StringLength(100)]
        public string? ThuongHieuLucMua { get; set; }

        [Required]
        [ForeignKey("HoaDon")]
        public Guid HoaDonId { get; set; }
        public HoaDon HoaDon { get; set; }

        [Required]
        [ForeignKey("SanPham")]
        public int SanPhamId { get; set; }
        public SanPham SanPham { get; set; }
    }
}
