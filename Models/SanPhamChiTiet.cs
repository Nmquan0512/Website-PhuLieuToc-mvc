using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace PhuLieuToc.Models
{
	public class SanPhamChiTiet
	{
		[Key]
		public int SanPhamChiTietId { get; set; }

		[StringLength(500)]
		public string? Anh { get; set; }

		[Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
		[Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Gia { get; set; }

		[Required(ErrorMessage = "Số lượng tồn là bắt buộc")]
		[Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
		public int SoLuongTon { get; set; }

		[Required]
		[Range(0, 1, ErrorMessage = "Trạng thái phải là 0 hoặc 1")]
		public int TrangThai { get; set; } = 1; 

		[Required]
		[ForeignKey("SanPham")]
		public int SanPhamId { get; set; }
		public SanPham SanPham { get; set; }

        public ICollection<SanPhamChiTietThuocTinh> SanPhamChiTietThuocTinhs { get; set; } = new List<SanPhamChiTietThuocTinh>();

		public ICollection<HoaDonChiTiet> HoaDonChiTiets { get; set; } = new List<HoaDonChiTiet>();

		public ICollection<GioHangChiTiet> GioHangChiTiets { get; set; } = new List<GioHangChiTiet>();
    }
}
