using System.ComponentModel.DataAnnotations;

namespace PhuLieuToc.Models
{
    public class ThuocTinh
    {
        [Key]
        public int ThuocTinhId { get; set; }

        [Required(ErrorMessage = "Tên thuộc tính là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên thuộc tính không được vượt quá 100 ký tự")]
        public string TenThuocTinh { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Trạng thái phải là 0 hoặc 1")]
        public int TrangThai { get; set; } = 1;

        public ICollection<GiaTriThuocTinh> GiaTriThuocTinhs { get; set; } = new List<GiaTriThuocTinh>();
    }
}
