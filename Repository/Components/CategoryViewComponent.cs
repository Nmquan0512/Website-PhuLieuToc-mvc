using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace PhuLieuToc.Repository.Components
{
    public class CategoryViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        public CategoryViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy ra các danh mục cha kèm theo danh mục con (nếu có)
            var categories = await _context.Categorys
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.Children)
                .ToListAsync();

            return View(categories);
        }
    }
}
