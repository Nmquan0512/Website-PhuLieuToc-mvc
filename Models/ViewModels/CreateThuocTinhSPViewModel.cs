namespace PhuLieuToc.Models.ViewModels
{
	public class CreateThuocTinhSPViewModel
	{

		public string TenThuocTinh { get; set; }

		public string? MoTa { get; set; }

		public int TrangThai { get; set; } = 1;

		public List<string> GiaTriThuocTinhs { get; set; } = new List<string>();
	}

	public class GiaTriThuocTinhViewModel
	{
		public int GiaTriThuocTinhId { get; set; }
		public string TenGiaTri { get; set; }

		public int TrangThai { get; set; }
	}
}
