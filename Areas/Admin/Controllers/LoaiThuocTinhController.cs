using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Models.ViewModels;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Areas.Admin.Controllers
{
	[Area("Admin")]  
	[Authorize(Roles = "Admin")]
	public class LoaiThuocTinhController : Controller
	{
		private readonly AppDbContext _context;

		public LoaiThuocTinhController(AppDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			var ListThuocTinh = _context.ThuocTinhs.Include(tt => tt.GiaTriThuocTinhs).ToList();

			return View(ListThuocTinh);
		}

		[HttpGet]
		public IActionResult Create()
		{ 
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromBody]CreateThuocTinhSPViewModel model)
		{
			
			var ThemThuocTinh = new ThuocTinh
			{
				TenThuocTinh = model.TenThuocTinh,
				MoTa = model.MoTa,
				TrangThai = model.TrangThai
			};

			foreach ( var giatri in model.GiaTriThuocTinhs)
			{
				var newGiaTri = new GiaTriThuocTinh
				{
					TenGiaTri = giatri,
					TrangThai = 1 
				};
				ThemThuocTinh.GiaTriThuocTinhs.Add(newGiaTri);
			}
			 		_context.ThuocTinhs.Add(ThemThuocTinh);


			 await _context.SaveChangesAsync();

			return Ok(new { success = true });
		}

		[HttpGet]
		public IActionResult Edit(int id)
		{
			var ListThuocTinh = _context.ThuocTinhs.Include(tt => tt.GiaTriThuocTinhs).FirstOrDefault(x => x.ThuocTinhId == id);
			return View(ListThuocTinh);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [FromBody] CreateThuocTinhSPViewModel model)
		{
			var ListThuocTinh = _context.ThuocTinhs.Include(tt => tt.GiaTriThuocTinhs).FirstOrDefault(x => x.ThuocTinhId == id);

			ListThuocTinh.TenThuocTinh = model.TenThuocTinh;
			ListThuocTinh.MoTa = model.MoTa;
			ListThuocTinh.TrangThai = model.TrangThai;

			if(ListThuocTinh.GiaTriThuocTinhs != null)
			{
				_context.GiaTriThuocTinhs.RemoveRange(ListThuocTinh.GiaTriThuocTinhs);
			}

			foreach (var giatri in model.GiaTriThuocTinhs)
			{
				var newGiaTri = new GiaTriThuocTinh
				{
					TenGiaTri = giatri,
					TrangThai = 1
				};
				ListThuocTinh.GiaTriThuocTinhs.Add(newGiaTri);
			}

			_context.ThuocTinhs.Update(ListThuocTinh);
		   await _context.SaveChangesAsync();

			return Ok(new { success = true });
		}

		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			var thuocTinh = await _context.ThuocTinhs.Include(tt => tt.GiaTriThuocTinhs)
				.FirstOrDefaultAsync(x => x.ThuocTinhId == id);
			if (thuocTinh == null)
			{
				return NotFound(new { success = false, message = "Không tìm thấy thuộc tính" });
			}

			_context.GiaTriThuocTinhs.RemoveRange(thuocTinh.GiaTriThuocTinhs);
			_context.ThuocTinhs.Remove(thuocTinh);
			await _context.SaveChangesAsync();
			return Ok(new { success = true });
		}

	}
}
