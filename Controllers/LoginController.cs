using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;
using System.Security.Claims;

namespace PhuLieuToc.Controllers
{
	public class LoginController : Controller
	{
		private readonly AppDbContext _context;

		public LoginController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public IActionResult Index(string? returnUrl)
		{
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> TestDb()
		{
			try
			{
				var userCount = await _context.TaiKhoans.CountAsync();
				return Content($"Database connected! User count: {userCount}");
			}
			catch (Exception ex)
			{
				return Content($"Database error: {ex.Message}");
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Index(string username, string password, string? returnUrl)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
				{
					ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
					return View();
				}

			var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.TenDangNhap == username && u.TrangThai);
			System.Diagnostics.Debug.WriteLine($"User found: {user != null}");
			
			if (user == null)
			{
				ViewBag.Error = "Tên đăng nhập không tồn tại hoặc tài khoản bị khóa";
				return View();
			}
			
			if (!BCrypt.Net.BCrypt.Verify(password, user.MatKhau))
			{
				ViewBag.Error = "Mật khẩu hoặc tài khoản không đúng";
				return View();
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.TaiKhoanId.ToString()),
				new Claim(ClaimTypes.Name, user.TenDangNhap),
				new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
				new Claim(ClaimTypes.Role, string.IsNullOrEmpty(user.VaiTro) ? "User" : user.VaiTro)
			};
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);
			
			System.Diagnostics.Debug.WriteLine("Signing in user...");
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
			System.Diagnostics.Debug.WriteLine("User signed in successfully");

			TempData["Success"] = $"Chào mừng {user.TenDangNhap}! Đăng nhập thành công.";

			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				System.Diagnostics.Debug.WriteLine($"Redirecting to returnUrl: {returnUrl}");
				return LocalRedirect(returnUrl);
			}

			System.Diagnostics.Debug.WriteLine("Redirecting to Home/Index");
			return RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
				ViewBag.Error = "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại.";
				return View();
			}
		}

		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(string username, string email, string password, string? phone)
		{
			if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
			{
				ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
				return View();
			}

			// Validate username length
			if (username.Length < 3)
			{
				ViewBag.Error = "Tên đăng nhập phải có ít nhất 3 ký tự";
				return View();
			}

			// Validate password length
			if (password.Length < 6)
			{
				ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự";
				return View();
			}

			// Validate email format
			if (!email.Contains("@") || !email.Contains("."))
			{
				ViewBag.Error = "Email không đúng định dạng";
				return View();
			}

			// Check for duplicate username
			if (await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == username))
			{
				ViewBag.Error = "Tên đăng nhập đã tồn tại";
				return View();
			}

			// Check for duplicate email
			if (await _context.TaiKhoans.AnyAsync(u => u.Email == email))
			{
				ViewBag.Error = "Email đã tồn tại";
				return View();
			}

			// Check for duplicate phone if provided
			if (!string.IsNullOrWhiteSpace(phone) && await _context.TaiKhoans.AnyAsync(u => u.SoDienThoai == phone))
			{
				ViewBag.Error = "Số điện thoại đã tồn tại";
				return View();
			}

			var user = new TaiKhoan
			{
				TenDangNhap = username,
				Email = email,
				MatKhau = BCrypt.Net.BCrypt.HashPassword(password),
				TrangThai = true,
				VaiTro = "User",
				SoDienThoai = phone ?? string.Empty
			};
			_context.TaiKhoans.Add(user);
			await _context.SaveChangesAsync();

			TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập";
			return RedirectToAction("Index");
		}

		[Authorize]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}

		[HttpGet]
		public IActionResult AccessDenied() => View();

		[HttpGet]
		public async Task<IActionResult> ResetAdminPassword()
		{
			try
			{
				var admin = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.TenDangNhap == "admin");
				if (admin != null)
				{
					// Reset password to "admin123"
					admin.MatKhau = BCrypt.Net.BCrypt.HashPassword("admin123");
					await _context.SaveChangesAsync();
					return Content("Mật khẩu admin đã được reset thành 'admin123'");
				}
				return Content("Không tìm thấy tài khoản admin");
			}
			catch (Exception ex)
			{
				return Content($"Lỗi: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> CreateAdmin()
		{
			try
			{
				// Check if admin already exists
				var existingAdmin = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.TenDangNhap == "admin");
				if (existingAdmin != null)
				{
					return Content("Tài khoản admin đã tồn tại");
				}

				// Create new admin account
				var admin = new TaiKhoan
				{
					TenDangNhap = "admin",
					Email = "admin@phulieutoc.com",
					MatKhau = BCrypt.Net.BCrypt.HashPassword("admin123"),
					TrangThai = true,
					VaiTro = "Admin",
					SoDienThoai = "0123456789"
				};

				_context.TaiKhoans.Add(admin);
				await _context.SaveChangesAsync();

				return Content("Tài khoản admin đã được tạo thành công! Username: admin, Password: admin123");
			}
			catch (Exception ex)
			{
				return Content($"Lỗi: {ex.Message}");
			}
		}


	}
}
