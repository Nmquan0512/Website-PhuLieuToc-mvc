using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ThuongHieuController : Controller
    {

        private readonly AppDbContext _context;

          public ThuongHieuController(AppDbContext context)
        {
            _context = context;
        }
        
        public IActionResult Index()
        {
            var brands = _context.Brands.ToList();
            return View(brands);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(BrandModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ThuongHieu = new BrandModel()
            {
                TenThuongHieu = model.TenThuongHieu,
                Slug = string.IsNullOrWhiteSpace(model.Slug) ? CreateSlug(model.TenThuongHieu) : CreateSlug(model.Slug),
                MoTa = model.MoTa,
                TrangThai = model.TrangThai
            };

            _context.Brands.Add(ThuongHieu);
            _context.SaveChanges();


            TempData["SuccessMessage"] = "Thêm thương hiệu thành công";
            return RedirectToAction("Index");

        }


		[HttpGet]
		public IActionResult Edit( int id)
		{
            var brands =  _context.Brands.FirstOrDefault( x => x.Id == id);
			return View(brands);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(int id , BrandModel model)
		{
			var brands = _context.Brands.FirstOrDefault(x => x.Id == id);
            if (brands == null)
			{
				return NotFound();
			}

            brands.TenThuongHieu = model.TenThuongHieu;
            brands.Slug = string.IsNullOrWhiteSpace(model.Slug) ? CreateSlug(model.TenThuongHieu) : model.Slug;
            brands.MoTa = model.MoTa;
            brands.TrangThai = model.TrangThai;
	

            await _context.SaveChangesAsync();

			return RedirectToAction("Index");
		}
		[HttpPost, ActionName("Delete")]
        public IActionResult Delete(int id)
        {
            var DeleteBrands = _context.Brands.FirstOrDefault(x => x.Id == id);

            if (DeleteBrands == null)
            {
                return NotFound();
            }

            _context.Brands.Remove(DeleteBrands);
            _context.SaveChanges();


			TempData["SuccessMessage"] = "Xoá thương hiệu thành công";
			return RedirectToAction("Index");
		}

		private string CreateSlug(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            return text.ToLower()
                .Replace("đ", "d")
                .Replace("Đ", "d")
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "")
                .Replace(";", "")
                .Replace(":", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("{", "")
                .Replace("}", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("|", "")
                .Replace("`", "")
                .Replace("~", "")
                .Replace("@", "")
                .Replace("#", "")
                .Replace("$", "")
                .Replace("%", "")
                .Replace("^", "")
                .Replace("&", "")
                .Replace("*", "")
                .Replace("+", "")
                .Replace("=", "")
                .Replace("_", "-");
        }
    }

    }

