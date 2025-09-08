using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;
using PhuLieuToc.Models;
using PhuLieuToc.Models.ViewModels;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DanhMucController : Controller
    {
        private readonly AppDbContext _context;

        public DanhMucController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            // Lấy danh sách danh mục cha (không có ParentCategoryId) để hiển thị trong bảng
            var danhSachDanhMuc = await _context.Categorys
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.Children)
                .ToListAsync();
                
            return View(danhSachDanhMuc);
        }

        public async Task<IActionResult> Create()
        {
            var danhsachDanhMuc = await _context.Categorys.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryModel category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Tạo slug nếu chưa có
                    if (string.IsNullOrEmpty(category.Slug))
                    {
                        category.Slug = CreateSlug(category.TenDanhMuc);
                    }

                    // Kiểm tra trùng slug
                    if (await _context.Categorys.AnyAsync(c => c.Slug == category.Slug))
                    {
                        ModelState.AddModelError("Slug", "Slug đã tồn tại. Vui lòng nhập slug khác.");
                        return View(category);
                    }

                    _context.Categorys.Add(category);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Lưu thất bại: " + (ex.InnerException?.Message ?? ex.Message));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu danh mục: " + ex.Message);
                }
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWithSubcategories([FromBody] CategoryCreateViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Name))
                {
                    return BadRequest(new { success = false, message = "Tên Danh Mục không được để trống !" });
                }

                var parentSlug = string.IsNullOrEmpty(model.Slug) ? CreateSlug(model.Name) : model.Slug;
                // Kiểm tra trùng slug cha
                if (await _context.Categorys.AnyAsync(c => c.Slug == parentSlug))
                {
                    return BadRequest(new { success = false, message = $"Slug '{parentSlug}' đã tồn tại. Vui lòng chọn slug khác." });
                }

                // Kiểm tra trùng giữa các slug con và với slug cha
                var childSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (model.Subcategories != null)
                {
                    foreach (var sub in model.Subcategories)
                    {
                        var s = string.IsNullOrEmpty(sub.Slug) ? CreateSlug(sub.Name) : sub.Slug;
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            return BadRequest(new { success = false, message = "Slug danh mục con không hợp lệ." });
                        }
                        if (s.Equals(parentSlug, StringComparison.OrdinalIgnoreCase))
                        {
                            return BadRequest(new { success = false, message = $"Slug con '{s}' trùng với slug cha." });
                        }
                        if (!childSlugs.Add(s))
                        {
                            return BadRequest(new { success = false, message = $"Có slug danh mục con bị trùng: '{s}'." });
                        }
                    }

                    // Kiểm tra trùng slug con với DB
                    var existsChild = await _context.Categorys
                        .AnyAsync(c => childSlugs.Contains(c.Slug));
                    if (existsChild)
                    {
                        return BadRequest(new { success = false, message = "Có slug danh mục con đã tồn tại trong hệ thống." });
                    }
                }

                //Tao danh muc cha
                var DanhMucCha = new CategoryModel
                {
                    TenDanhMuc = model.Name,
                    MoTa = model.Description,
                    Slug = parentSlug,
                    TrangThai = model.Active ? 1 : 0,
                    ParentCategoryId = null
                };
                _context.Add(DanhMucCha);
                await _context.SaveChangesAsync();

                //Tao danh muc con 
                if (model.Subcategories != null && model.Subcategories.Any())
                {
                    foreach(var subcategory in model.Subcategories)
                    {
                        var DanhMucCon = new CategoryModel
                        {
                            TenDanhMuc = subcategory.Name,
                            MoTa = subcategory.Description,
                            Slug = string.IsNullOrEmpty(subcategory.Slug) ? CreateSlug(subcategory.Name) : subcategory.Slug,
                            TrangThai = subcategory.Active ? 1 : 0,
                            ParentCategoryId = DanhMucCha.Id
                        };
                        _context.Add(DanhMucCon);
                       
                    }
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Danh mục đã được tạo thành công", categoryId = DanhMucCha.Id });

            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { success = false, message = "Lưu thất bại: " + (ex.InnerException?.Message ?? ex.Message) });
            }
            catch (Exception ex) 
            {
                return BadRequest(new { success = false, message = "Có lỗi xảy ra " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categorys
                    .Include(c => c.Children)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Kiểm tra xem có sản phẩm nào thuộc danh mục này không (qua SanPham -> SanPhamChiTiet)
                var hasProducts = await _context.SanPhamChiTiets
                    .Include(ct => ct.SanPham)
                    .AnyAsync(ct => ct.SanPham.CategoryId == id);
                if (hasProducts)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục có sản phẩm. Vui lòng di chuyển hoặc xóa sản phẩm trước." });
                }

                if (category.Children != null && category.Children.Any())
                    {
                var childIds = category.Children.Select(c => c.Id).ToList();
                var hasProductsInChildren = await _context.SanPhamChiTiets
                    .Include(ct => ct.SanPham)
                    .AnyAsync(ct => childIds.Contains(ct.SanPham.CategoryId));
                if (hasProductsInChildren)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục có con chứa sản phẩm" });
                }
                     }
              if (category.Children != null && category.Children.Any())
                {
                    _context.Categorys.RemoveRange(category.Children);
                }

                _context.Categorys.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Danh mục đã được xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {

            var duLieuEdit = await _context.Categorys.Include(c => c.Children)
                .FirstOrDefaultAsync(x => x.Id == id);

            return View(duLieuEdit);
        }

        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] CategoryCreateViewModel model , int id)
        {
            var ListChaCon = await _context.Categorys.Include(c => c.Children)
                                .FirstOrDefaultAsync(x => x.Id == id);
            if (ListChaCon == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy danh mục" });
            }

                ListChaCon.TenDanhMuc = model.Name;
                ListChaCon.MoTa = model.Description;
                ListChaCon.Slug = string.IsNullOrEmpty(model.Slug) ? CreateSlug(model.Name) : model.Slug;
                ListChaCon.TrangThai = model.Active ? 1 : 0;

            
            _context.Update(ListChaCon);
            await _context.SaveChangesAsync();

            // Xóa danh mục con cũ
            if (ListChaCon.Children != null)
            {
                _context.Categorys.RemoveRange(ListChaCon.Children);
            }

            foreach (var sub in model.Subcategories)
            {
                var SuaDanhSachCon = new CategoryModel
                {
                    TenDanhMuc = sub.Name,
                    MoTa = sub.Description,
                    Slug = string.IsNullOrEmpty(sub.Slug) ? CreateSlug(sub.Name) : sub.Slug,
                    TrangThai = sub.Active ? 1 : 0,
                    ParentCategoryId = ListChaCon.Id
                };
            _context.Add(SuaDanhSachCon);
            


            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Danh mục đã được sửa thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> Subcategories(int id)
        {
            var DanhMucCon = await _context.Categorys.Include(c => c.Children)
                .FirstOrDefaultAsync(x => x.Id == id);

            return View(DanhMucCon);
        }

        [HttpGet]
        public async Task<IActionResult> EditSubcategory(int id)
        {
            var DanhMucCon = await _context.Categorys.Include(c => c.Children)
                .FirstOrDefaultAsync(x => x.Id == id);

            return View(DanhMucCon);
        }


          [HttpPut]
          public async Task<IActionResult> EditSubcategory(int id, [FromBody] SubcategoryViewModel model)
          {
              var subcategory = await _context.Categorys.FirstOrDefaultAsync(x => x.Id == id);
              if (subcategory == null)
                  return NotFound(new { success = false, message = "Không tìm thấy danh mục con" });

              subcategory.TenDanhMuc = model.Name;
              subcategory.MoTa = model.Description;
              subcategory.Slug = CreateSlug(model.Name);
              subcategory.TrangThai = model.Active ? 1 : 0;

              await _context.SaveChangesAsync();
              return Json(new { success = true, message = "Danh mục con đã được sửa thành công" });
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
