using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class SanPhamChiTietThuocTinh
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("SanPhamChiTiet")]
        public int SanPhamChiTietId { get; set; }
        public SanPhamChiTiet SanPhamChiTiet { get; set; }

        [Required]
        [ForeignKey("GiaTriThuocTinh")]
        public int GiaTriThuocTinhId { get; set; }
        public GiaTriThuocTinh GiaTriThuocTinh { get; set; }
    }
}
