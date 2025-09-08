using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuLieuToc.Models
{
	public class LichSuTrangThaiHoaDon
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		public Guid HoaDonId { get; set; }  

		[ForeignKey("HoaDonId")]
		public HoaDon HoaDon { get; set; }   

		[Required]
		public int TrangThaiCu { get; set; }

		[Required]
		public int TrangThaiMoi { get; set; }

		[Required]
		public DateTime ThoiGianThayDoi { get; set; }

		public string? GhiChu { get; set; }
	}
}
