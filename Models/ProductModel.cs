using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PhuLieuToc.Models
{
	public class ProductModel
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string TenSanPham { get; set; }


		public string Slug { get; set; } 

	
		public string MoTa { get; set; }

		public string Anh { get; set; }
		public decimal GiaBan { get; set; }

		public int CategoryId { get; set; }
		public CategoryModel Category { get; set; }

		public int BrandId { get; set; }
		public BrandModel Brand { get; set; }
	}
}
