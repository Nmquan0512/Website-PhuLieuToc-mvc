using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
    public class SanPham
    {
        [Key]
        public int SanPhamId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string TenSanPham { get; set; }

        [Required(ErrorMessage = "Slug là bắt buộc")]
        [StringLength(200, ErrorMessage = "Slug không được vượt quá 200 ký tự")]
        public string Slug { get; set; }

        [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? MoTa { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Trạng thái phải là 0 hoặc 1")]
        public int TrangThai { get; set; } = 1; // 1: active, 0: inactive
        
        [Required]
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public CategoryModel Category { get; set; }

        [Required]
        [ForeignKey("Brand")]
        public int BrandId { get; set; }
        public BrandModel Brand { get; set; }

        public ICollection<SanPhamChiTiet> SanPhamChiTiets { get; set; } = new List<SanPhamChiTiet>();
    }
}
