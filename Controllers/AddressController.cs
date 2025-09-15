using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;
using System.Security.Claims;
using System.Text.Json;

namespace PhuLieuToc.Controllers
{
    public class ProvinceData
    {
        public string name { get; set; }
        public string slug { get; set; }
        public string type { get; set; }
        public string name_with_type { get; set; }
        public string code { get; set; }
    }

    public class WardData
    {
        public string name { get; set; }
        public string slug { get; set; }
        public string type { get; set; }
        public string name_with_type { get; set; }
        public string code { get; set; }
        public string parent_code { get; set; }
    }

    [Authorize]
    public class AddressController : Controller
    {
        private readonly AppDbContext _context;

        public AddressController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var addresses = _context.DiaChiGiaoHangs
                .Where(a => a.TaiKhoanId == userId && a.TrangThai == 1)
                .OrderByDescending(a => a.LaMacDinh)
                .ThenBy(a => a.Id)
                .ToList();

            return View(addresses);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null) 
            {
                TempData["Error"] = "Bạn cần đăng nhập để thêm địa chỉ";
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiaChiGiaoHang model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            // Debug input data
            Console.WriteLine($"=== ADDRESS CREATE DEBUG ===");
            Console.WriteLine($"Received data: HoTen={model.HoTen}, SoDienThoai={model.SoDienThoai}, DiaChiCuThe={model.DiaChiCuThe}, TinhThanh={model.TinhThanh}, XaPhuong={model.XaPhuong}, LaMacDinh={model.LaMacDinh}");
            Console.WriteLine($"UserId: {userId}");
            
            // Debug form data
            Console.WriteLine("Form data:");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"  {key} = {Request.Form[key]}");
            }

            // Debug ModelState
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            // Remove TaiKhoan validation error
            ModelState.Remove("TaiKhoan");
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                var errorMessage = "Có lỗi validation: " + string.Join(", ", errors);
                Console.WriteLine(errorMessage);
                TempData["Error"] = errorMessage;
                return View(model);
            }

            try
            {
                Console.WriteLine("Starting to save address...");
                
                // Test database connection
                var testCount = await _context.DiaChiGiaoHangs.CountAsync();
                Console.WriteLine($"Current address count: {testCount}");
                
                // Fix LaMacDinh - check if checkbox was checked
                var laMacDinhValues = Request.Form["LaMacDinh"].ToString();
                var isDefault = laMacDinhValues.Contains("true");
                Console.WriteLine($"LaMacDinh form value: {laMacDinhValues}, isDefault: {isDefault}");
                
                // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
                if (isDefault)
                {
                    Console.WriteLine("Setting as default address...");
                    var existingAddresses = await _context.DiaChiGiaoHangs
                        .Where(a => a.TaiKhoanId == userId && a.TrangThai == 1)
                        .ToListAsync();
                    
                    foreach (var addr in existingAddresses)
                    {
                        addr.LaMacDinh = false;
                    }
                }

                model.TaiKhoanId = userId.Value;
                model.TrangThai = 1; // Đảm bảo trạng thái là active
                model.LaMacDinh = isDefault; // Set the correct value
                
                Console.WriteLine($"Saving address: {model.HoTen}, {model.DiaChiDayDu}, LaMacDinh: {model.LaMacDinh}");
                
                _context.DiaChiGiaoHangs.Add(model);
                await _context.SaveChangesAsync();

                Console.WriteLine("Address saved successfully!");
                TempData["Success"] = "Thêm địa chỉ thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving address: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var address = await _context.DiaChiGiaoHangs
                .FirstOrDefaultAsync(a => a.Id == id && a.TaiKhoanId == userId && a.TrangThai == 1);

            if (address == null) return NotFound();

            return View(address);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DiaChiGiaoHang model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var address = await _context.DiaChiGiaoHangs
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.TaiKhoanId == userId && a.TrangThai == 1);

            if (address == null) return NotFound();

            // Debug ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Có lỗi validation: " + string.Join(", ", errors);
                return View(model);
            }

            try
            {
                // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
                if (model.LaMacDinh)
                {
                    var existingAddresses = await _context.DiaChiGiaoHangs
                        .Where(a => a.TaiKhoanId == userId && a.TrangThai == 1 && a.Id != model.Id)
                        .ToListAsync();
                    
                    foreach (var addr in existingAddresses)
                    {
                        addr.LaMacDinh = false;
                    }
                }

                address.HoTen = model.HoTen;
                address.SoDienThoai = model.SoDienThoai;
                address.DiaChiCuThe = model.DiaChiCuThe;
                address.TinhThanh = model.TinhThanh;
                address.XaPhuong = model.XaPhuong;
                address.GhiChu = model.GhiChu;
                address.LaMacDinh = model.LaMacDinh;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật địa chỉ thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            var address = await _context.DiaChiGiaoHangs
                .FirstOrDefaultAsync(a => a.Id == id && a.TaiKhoanId == userId && a.TrangThai == 1);

            if (address == null) return Json(new { success = false, message = "Không tìm thấy địa chỉ" });

            address.TrangThai = -1; // Xóa mềm
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa địa chỉ thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            var address = await _context.DiaChiGiaoHangs
                .FirstOrDefaultAsync(a => a.Id == id && a.TaiKhoanId == userId && a.TrangThai == 1);

            if (address == null) return Json(new { success = false, message = "Không tìm thấy địa chỉ" });

            // Bỏ mặc định của tất cả địa chỉ khác
            var existingAddresses = await _context.DiaChiGiaoHangs
                .Where(a => a.TaiKhoanId == userId && a.TrangThai == 1)
                .ToListAsync();
            
            foreach (var addr in existingAddresses)
            {
                addr.LaMacDinh = false;
            }

            // Đặt địa chỉ này làm mặc định
            address.LaMacDinh = true;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đặt địa chỉ mặc định thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "json", "province.json");
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                var provinces = JsonSerializer.Deserialize<Dictionary<string, ProvinceData>>(jsonContent);
                
                var result = provinces.Select(p => new { 
                    code = p.Key, 
                    name = p.Value.name,
                    name_with_type = p.Value.name_with_type
                }).OrderBy(p => p.name).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWards(string provinceCode)
        {
            try
            {
                var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "json", "ward.json");
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                var wards = JsonSerializer.Deserialize<Dictionary<string, WardData>>(jsonContent);
                
                var result = wards
                    .Where(w => w.Value.parent_code == provinceCode)
                    .Select(w => new { 
                        code = w.Key, 
                        name = w.Value.name,
                        name_with_type = w.Value.name_with_type
                    })
                    .OrderBy(w => w.name)
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false });

            try
            {
                var addresses = await _context.DiaChiGiaoHangs
                    .Where(a => a.TaiKhoanId == userId && a.TrangThai == 1)
                    .OrderByDescending(a => a.LaMacDinh)
                    .ThenBy(a => a.Id)
                    .Select(a => new {
                        id = a.Id,
                        hoTen = a.HoTen,
                        soDienThoai = a.SoDienThoai,
                        diaChiDayDu = a.DiaChiDayDu,
                        ghiChu = a.GhiChu,
                        laMacDinh = a.LaMacDinh
                    })
                    .ToListAsync();

                return Json(new { success = true, addresses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAddress(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false });

            try
            {
                var address = await _context.DiaChiGiaoHangs
                    .FirstOrDefaultAsync(a => a.Id == id && a.TaiKhoanId == userId && a.TrangThai == 1);

                if (address == null) return Json(new { success = false });

                return Json(new { 
                    success = true, 
                    address = new {
                        id = address.Id,
                        hoTen = address.HoTen,
                        soDienThoai = address.SoDienThoai,
                        diaChiDayDu = address.DiaChiDayDu,
                        ghiChu = address.GhiChu
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        }
    }
}
