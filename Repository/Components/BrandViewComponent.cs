using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace PhuLieuToc.Repository.Components
{
	public class BrandViewComponent : ViewComponent
	{
		private readonly AppDbContext _context;

		public BrandViewComponent(AppDbContext context)
		{
			_context = context;
		}

        public async Task<IViewComponentResult> InvokeAsync() => View(await _context.Brands.Where(b => b.TrangThai == 1).ToListAsync());


	}
}
