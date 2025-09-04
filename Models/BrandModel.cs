using System.ComponentModel.DataAnnotations;

namespace PhuLieuToc.Models
{
	public class BrandModel
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(100)]
		public string TenThuongHieu { get; set; }

		[Required]
		[MaxLength(100)]
		public string Slug { get; set; }

		[MaxLength(500)]
		public string MoTa { get; set; }

		public int TrangThai { get; set; } 

		public ICollection<SanPham> SanPhams { get; set; }
	}
}
