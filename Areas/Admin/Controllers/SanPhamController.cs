using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Models.ViewModels;
using PhuLieuToc.Repository;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace PhuLieuToc.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SanPhamController : Controller
    {
        private readonly AppDbContext _context;

        public SanPhamController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.SanPhams
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.SanPhamChiTiets.OrderBy(ct => ct.SanPhamChiTietId))
                .ToListAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ProductCreateEditViewModel
            {
                Categories = await _context.Categorys.ToListAsync(),
                Brands = await _context.Brands.ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categorys.ToListAsync();
                model.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            if (await _context.SanPhams.AnyAsync(p => p.Slug == model.Slug))
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại");
                model.Categories = await _context.Categorys.ToListAsync();
                model.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            var product = new SanPham
            {
                TenSanPham = model.TenSanPham,
                Slug = model.Slug,
                MoTa = model.MoTa,
                TrangThai = model.TrangThai,
                CategoryId = model.CategoryId,
                BrandId = model.BrandId
            };
            _context.SanPhams.Add(product);
            await _context.SaveChangesAsync();

            if (model.Variants != null)
            {
                foreach (var v in model.Variants)
                {
                    var detail = new SanPhamChiTiet
                    {
                        SanPhamId = product.SanPhamId,
                        Gia = v.Gia,
                        SoLuongTon = v.SoLuongTon,
                        Anh = null,
                        TrangThai = v.TrangThai
                    };
                    _context.SanPhamChiTiets.Add(detail);
                    await _context.SaveChangesAsync();

                    // Save image if uploaded
                    if (v.AnhFile != null && v.AnhFile.Length > 0)
                    {
                        var fileName = $"spct_{detail.SanPhamChiTietId}_{Path.GetFileName(v.AnhFile.FileName)}";
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "variants");
                        Directory.CreateDirectory(uploadDir);
                        var filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await v.AnhFile.CopyToAsync(stream);
                        }
                        detail.Anh = $"/uploads/variants/{fileName}";
                        _context.Update(detail);
                        await _context.SaveChangesAsync();
                    }

                    if (v.GiaTriThuocTinhIds != null)
                    {
                        foreach (var gId in v.GiaTriThuocTinhIds.Distinct())
                        {
                            _context.SanPhamChiTietThuocTinhs.Add(new SanPhamChiTietThuocTinh
                            {
                                SanPhamChiTietId = detail.SanPhamChiTietId,
                                GiaTriThuocTinhId = gId
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.SanPhams
                .Include(p => p.SanPhamChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTietThuocTinhs)
                .FirstOrDefaultAsync(p => p.SanPhamId == id);
            if (product == null) return NotFound();

            var vm = new ProductCreateEditViewModel
            {
                SanPhamId = product.SanPhamId,
                TenSanPham = product.TenSanPham,
                Slug = product.Slug,
                MoTa = product.MoTa,
                TrangThai = product.TrangThai,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                Categories = await _context.Categorys.ToListAsync(),
                Brands = await _context.Brands.ToListAsync(),
                Variants = product.SanPhamChiTiets.Select(ct => new VariantInputViewModel
                {
                    Gia = ct.Gia,
                    SoLuongTon = ct.SoLuongTon,
                    Anh = ct.Anh,
                    TrangThai = ct.TrangThai,
                    GiaTriThuocTinhIds = ct.SanPhamChiTietThuocTinhs.Select(x => x.GiaTriThuocTinhId).ToList()
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateEditViewModel model)
        {
            var product = await _context.SanPhams
                .Include(p => p.SanPhamChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTietThuocTinhs)
                .FirstOrDefaultAsync(p => p.SanPhamId == id);
            if (product == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categorys.ToListAsync();
                model.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            if (await _context.SanPhams.AnyAsync(p => p.Slug == model.Slug && p.SanPhamId != id))
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại");
                model.Categories = await _context.Categorys.ToListAsync();
                model.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            product.TenSanPham = model.TenSanPham;
            product.Slug = model.Slug;
            product.MoTa = model.MoTa;
            product.TrangThai = model.TrangThai;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;

            // Replace variants: simple approach for now
            var oldDetails = product.SanPhamChiTiets.ToList();
            foreach (var ct in oldDetails)
            {
                var oldLinks = _context.SanPhamChiTietThuocTinhs.Where(x => x.SanPhamChiTietId == ct.SanPhamChiTietId);
                _context.SanPhamChiTietThuocTinhs.RemoveRange(oldLinks);
                // KHÔNG xóa file ảnh cũ tại đây để có thể tái sử dụng đường dẫn ảnh khi người dùng không upload ảnh mới
            }
            _context.SanPhamChiTiets.RemoveRange(oldDetails);
            await _context.SaveChangesAsync();

            if (model.Variants != null)
            {
                foreach (var v in model.Variants)
                {
                    var detail = new SanPhamChiTiet
                    {
                        SanPhamId = product.SanPhamId,
                        Gia = v.Gia,
                        SoLuongTon = v.SoLuongTon,
                        Anh = null,
                        TrangThai = v.TrangThai
                    };
                    _context.SanPhamChiTiets.Add(detail);
                    await _context.SaveChangesAsync();

                    if (v.AnhFile != null && v.AnhFile.Length > 0)
                    {
                        var fileName = $"spct_{detail.SanPhamChiTietId}_{Path.GetFileName(v.AnhFile.FileName)}";
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "variants");
                        Directory.CreateDirectory(uploadDir);
                        var filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await v.AnhFile.CopyToAsync(stream);
                        }
                        detail.Anh = $"/uploads/variants/{fileName}";
                        _context.Update(detail);
                        await _context.SaveChangesAsync();
                    }
                    else if (!string.IsNullOrEmpty(v.Anh))
                    {
                        // Giữ lại ảnh cũ
                        detail.Anh = v.Anh;
                        _context.Update(detail);
                        await _context.SaveChangesAsync();
                    }

                    if (v.GiaTriThuocTinhIds != null)
                    {
                        foreach (var gId in v.GiaTriThuocTinhIds.Distinct())
                        {
                            _context.SanPhamChiTietThuocTinhs.Add(new SanPhamChiTietThuocTinh
                            {
                                SanPhamChiTietId = detail.SanPhamChiTietId,
                                GiaTriThuocTinhId = gId
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // JSON endpoints to power dynamic UI
        [HttpGet]
        public async Task<IActionResult> GetThuocTinhs()
        {
            var data = await _context.ThuocTinhs
                .Where(t => t.TrangThai == 1)
                .Select(t => new { id = t.ThuocTinhId, name = t.TenThuocTinh })
                .ToListAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetGiaTriByThuocTinh(int thuocTinhId)
        {
            var data = await _context.GiaTriThuocTinhs
                .Where(v => v.ThuocTinhId == thuocTinhId && v.TrangThai == 1)
                .Select(v => new { id = v.GiaTriThuocTinhId, name = v.TenGiaTri })
                .ToListAsync();
            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> GetGiaTriByIds([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0) return Json(new List<object>());
            var data = await _context.GiaTriThuocTinhs
                .Include(v => v.ThuocTinh)
                .Where(v => ids.Contains(v.GiaTriThuocTinhId))
                .Select(v => new
                {
                    id = v.GiaTriThuocTinhId,
                    name = v.TenGiaTri,
                    thuocTinhId = v.ThuocTinhId,
                    thuocTinhName = v.ThuocTinh.TenThuocTinh
                })
                .ToListAsync();
            return Json(data);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.SanPhams
                .Include(p => p.SanPhamChiTiets)
                .FirstOrDefaultAsync(p => p.SanPhamId == id);
            if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

            // remove variant links and files
            var detailIds = product.SanPhamChiTiets.Select(d => d.SanPhamChiTietId).ToList();
            var links = _context.SanPhamChiTietThuocTinhs.Where(x => detailIds.Contains(x.SanPhamChiTietId));
            _context.SanPhamChiTietThuocTinhs.RemoveRange(links);
            foreach (var ct in product.SanPhamChiTiets)
            {
                if (!string.IsNullOrEmpty(ct.Anh) && ct.Anh.StartsWith("/"))
                {
                    var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", ct.Anh.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
                }
            }
            _context.SanPhamChiTiets.RemoveRange(product.SanPhamChiTiets);
            _context.SanPhams.Remove(product);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}


