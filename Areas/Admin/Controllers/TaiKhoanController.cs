using Microsoft.AspNetCore.Mvc;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class TaiKhoanController : Controller
	{
		private readonly AppDbContext _context;

		public TaiKhoanController(AppDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			var ListTaiKhoan = _context.TaiKhoans.ToList();
			return View(ListTaiKhoan);
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		public IActionResult Create(TaiKhoan model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var PassHash = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

			var ThemTaiKhoan = new TaiKhoan()
			{
				TenDangNhap = model.TenDangNhap,
				MatKhau = PassHash,
				Email = model.Email,
				SoDienThoai = model.SoDienThoai,
				VaiTro = model.VaiTro,
				TrangThai = model.TrangThai
			};
			_context.TaiKhoans.Add(ThemTaiKhoan);
			_context.SaveChanges();



			return RedirectToAction("Index");
		}



		[HttpGet]
		public IActionResult Edit(int id)
		{
			var EditTaiKhoan = _context.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == id);
			return View(EditTaiKhoan);
		}

		[HttpPost]
		public IActionResult Edit(int id, TaiKhoan model, string? newPassword)
		{
			var taiKhoan = _context.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == id);
			if (taiKhoan == null) return NotFound();

			taiKhoan.TenDangNhap = model.TenDangNhap;
			taiKhoan.Email = model.Email;
			taiKhoan.SoDienThoai = model.SoDienThoai;
			taiKhoan.VaiTro = model.VaiTro;
			taiKhoan.TrangThai = model.TrangThai;


			if (!string.IsNullOrWhiteSpace(newPassword))
			{
				taiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword);
			}

			_context.TaiKhoans.Update(taiKhoan);
			_context.SaveChanges();



			return RedirectToAction("Index");
		}
		[HttpPost]
		public IActionResult Delete(int id)
		{
			var DeleteTaiKhoan = _context.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == id);

			if (DeleteTaiKhoan == null)
			{
				return NotFound();
			}

			_context.TaiKhoans.Remove(DeleteTaiKhoan);
			_context.SaveChanges();



			return RedirectToAction("Index");
		}

	}
}
