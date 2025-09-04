using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuLieuToc.Models
{
    public class CategoryModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenDanhMuc { get; set; }

        [Required]
        [MaxLength(100)]
        public string Slug { get; set; }

        [MaxLength(500)]
        public string MoTa { get; set; }

        public int TrangThai { get; set; }

        // Hỗ trợ phân cấp danh mục
        public int? ParentCategoryId { get; set; }
        public CategoryModel ParentCategory { get; set; }
        public ICollection<CategoryModel> Children { get; set; } = new List<CategoryModel>();

        // Navigation cho sản phẩm (nếu bạn dùng danh mục để tham chiếu cho sản phẩm)
        public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();



    }
}
