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
				// Debug logging
				System.Diagnostics.Debug.WriteLine($"Login attempt: username={username}, password length={password?.Length}");
				ViewBag.Debug = $"Form submitted: username={username}, password length={password?.Length}";
				
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
				ViewBag.Error = "Mật khẩu không đúng";
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
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			TempData["Success"] = $"Chào mừng {user.TenDangNhap}! Đăng nhập thành công.";

			if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			{
				return LocalRedirect(returnUrl);
			}

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

			if (await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == username))
			{
				ViewBag.Error = "Tên đăng nhập đã tồn tại";
				return View();
			}
			if (await _context.TaiKhoans.AnyAsync(u => u.Email == email))
			{
				ViewBag.Error = "Email đã tồn tại";
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
	}
}
