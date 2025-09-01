using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;
using PhuLieuToc.Models;

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
            // Lấy danh sách tất cả danh mục để làm danh mục cha
            var danhsachDanhCha = await _context.Categorys
                .Where(c => c.TrangThai == 1).Where(c => c.ParentCategoryId == null) // Chỉ lấy danh mục đang hoạt động
                .ToListAsync();
            
            // Tạo SelectList cho dropdown danh mục cha
            ViewBag.ParentCategory = new SelectList(danhsachDanhCha, "Id", "TenDanhMuc");

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

                    _context.Categorys.Add(category);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
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

                //Tao danh muc cha
                var DanhMucCha = new CategoryModel
                {
                    TenDanhMuc = model.Name,
                    MoTa = model.Description,
                    Slug = string.IsNullOrEmpty(model.Slug) ? CreateSlug(model.Name) : model.Slug,
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
                            Slug = string.IsNullOrEmpty(model.Slug) ? CreateSlug(model.Name) : model.Slug,
                            TrangThai = subcategory.Active ? 1 : 0,
                            ParentCategoryId = DanhMucCha.Id
                        };
                        _context.Add(DanhMucCon);
                       
                    }
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Danh mục đã được tạo thành công", categoryId = DanhMucCha.Id });

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

                // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
                var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
                if (hasProducts)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục có sản phẩm. Vui lòng di chuyển hoặc xóa sản phẩm trước." });
                }

                if (category.Children != null && category.Children.Any())
                    {
                var childIds = category.Children.Select(c => c.Id).ToList();
                var hasProductsInChildren = await _context.Products.AnyAsync(p => childIds.Contains(p.CategoryId));
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

    // ViewModel cho việc tạo danh mục với danh mục con
    public class CategoryCreateViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public List<SubcategoryViewModel> Subcategories { get; set; } = new List<SubcategoryViewModel>();
    }

    public class SubcategoryViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }
}
