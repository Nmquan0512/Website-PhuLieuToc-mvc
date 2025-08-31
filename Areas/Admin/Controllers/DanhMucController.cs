using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Repository;

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

        //[HttpPost]
        //public IActionResult Create()
        //{
            
        //}

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
