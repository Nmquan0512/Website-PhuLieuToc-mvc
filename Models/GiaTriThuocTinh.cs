using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class GiaTriThuocTinh
    {
        [Key]
        public int GiaTriThuocTinhId { get; set; }

        [Required(ErrorMessage = "Tên giá trị là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên giá trị không được vượt quá 100 ký tự")]
        public string TenGiaTri { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }

        [Required]
        [ForeignKey("ThuocTinh")]
        public int ThuocTinhId { get; set; }
        public ThuocTinh ThuocTinh { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Trạng thái phải là 0 hoặc 1")]
        public int TrangThai { get; set; } = 1;

        public ICollection<SanPhamChiTietThuocTinh> SanPhamChiTietThuocTinhs { get; set; } = new List<SanPhamChiTietThuocTinh>();
    }
}
