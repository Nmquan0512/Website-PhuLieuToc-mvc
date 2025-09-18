using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuLieuToc.Models;
using PhuLieuToc.Repository;
using System.Security.Claims;
using System.Net;
using System.Net.Mail;

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
		public IActionResult Register()
		{
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
		public async Task<IActionResult> Index(string username, string password, string? returnUrl, bool remember = false)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
				{
					ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
					return View();
				}

			var normalized = username.Trim();
			var looksEmail = normalized.Contains("@");
			// Truy vấn tối ưu: tránh OR để dùng index seek và dùng AsNoTracking
			var userShallow = looksEmail
				? await _context.TaiKhoans.AsNoTracking()
					.Where(u => u.Email == normalized && u.TrangThai)
					.Select(u => new { u.TaiKhoanId, u.TenDangNhap, u.Email, u.VaiTro, u.MatKhau })
					.FirstOrDefaultAsync()
				: await _context.TaiKhoans.AsNoTracking()
					.Where(u => u.TenDangNhap == normalized && u.TrangThai)
					.Select(u => new { u.TaiKhoanId, u.TenDangNhap, u.Email, u.VaiTro, u.MatKhau })
					.FirstOrDefaultAsync();
			System.Diagnostics.Debug.WriteLine($"User found: {userShallow != null}");

			if (userShallow == null)
			{
				ViewBag.Error = "Tài khoản không tồn tại hoặc đã bị khóa";
				return View();
			}
			
			var storedPassword = userShallow.MatKhau ?? string.Empty;
			var isBcryptMatch = false;
			try { isBcryptMatch = !string.IsNullOrEmpty(storedPassword) && BCrypt.Net.BCrypt.Verify(password, storedPassword); }
			catch { /* ignore malformed hash */ }

			var isLegacyPlainTextMatch = !isBcryptMatch && storedPassword == password;

			if (!isBcryptMatch && !isLegacyPlainTextMatch)
			{
				ViewBag.Error = "Mật khẩu không chính xác";
				return View();
			}

			// Nếu mật khẩu đang ở dạng plain text, nâng cấp lên BCrypt sau khi xác thực thành công
			if (isLegacyPlainTextMatch)
			{
				try
				{
					var tracked = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.TaiKhoanId == userShallow.TaiKhoanId);
					if (tracked != null)
					{
						tracked.MatKhau = BCrypt.Net.BCrypt.HashPassword(password);
						await _context.SaveChangesAsync();
					}
				}
				catch { /* best-effort upgrade */ }
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, userShallow.TaiKhoanId.ToString()),
				new Claim(ClaimTypes.Name, userShallow.TenDangNhap),
				new Claim(ClaimTypes.Email, userShallow.Email ?? string.Empty),
				new Claim(ClaimTypes.Role, string.IsNullOrEmpty(userShallow.VaiTro) ? "User" : userShallow.VaiTro)
			};
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var principal = new ClaimsPrincipal(identity);
			
			System.Diagnostics.Debug.WriteLine("Signing in user...");
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
			{
				IsPersistent = remember,
				ExpiresUtc = remember ? DateTimeOffset.UtcNow.AddDays(14) : (DateTimeOffset?)null
			});
			System.Diagnostics.Debug.WriteLine("User signed in successfully");

			TempData["Success"] = $"Chào mừng {userShallow.TenDangNhap}! Đăng nhập thành công.";

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
				System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
				#if DEBUG
				ViewBag.Error = $"Lỗi đăng nhập: {ex.Message}";
				#else
				ViewBag.Error = "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại.";
				#endif
				return View();
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(string username, string email, string password, string? phone)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
				{
					ViewBag.Error = "Tên đăng nhập tối thiểu 3 ký tự"; return View();
				}
				if (string.IsNullOrWhiteSpace(email)) { ViewBag.Error = "Vui lòng nhập email"; return View(); }
				if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
				{ ViewBag.Error = "Mật khẩu tối thiểu 6 ký tự"; return View(); }

				var existed = await _context.TaiKhoans.AnyAsync(u => u.TenDangNhap == username || u.Email == email);
				if (existed)
				{
					ViewBag.Error = "Tên đăng nhập hoặc email đã tồn tại"; return View();
				}

				var user = new TaiKhoan
				{
					TenDangNhap = username,
					Email = email,
					SoDienThoai = string.IsNullOrWhiteSpace(phone) ? null : phone,
					MatKhau = BCrypt.Net.BCrypt.HashPassword(password),
					TrangThai = true,
					VaiTro = "User"
				};
				_context.TaiKhoans.Add(user);
				await _context.SaveChangesAsync();

				// Auto sign-in after successful registration
				var claims = new List<Claim>
				{
					new Claim(ClaimTypes.NameIdentifier, user.TaiKhoanId.ToString()),
					new Claim(ClaimTypes.Name, user.TenDangNhap),
					new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
					new Claim(ClaimTypes.Role, string.IsNullOrEmpty(user.VaiTro) ? "User" : user.VaiTro)
				};
				await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
				TempData["Success"] = $"Chào mừng {user.TenDangNhap}! Tài khoản đã được tạo.";
				return RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				ViewBag.Error = $"Có lỗi xảy ra: {ex.Message}"; return View();
			}
		}

		[HttpGet]
		public IActionResult ForgotPassword()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(string email)
		{
			if (string.IsNullOrWhiteSpace(email)) { ViewBag.Error = "Vui lòng nhập email"; return View(); }
			var user = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Email == email && x.TrangThai);
			if (user == null) { ViewBag.Error = "Email không tồn tại"; return View(); }
			var otp = new Random().Next(100000, 999999).ToString();
			user.ResetOtp = otp;
			user.ResetOtpExpiryUtc = DateTime.UtcNow.AddMinutes(10);
			user.ResetOtpAttempts = 0;
			await _context.SaveChangesAsync();
			await SendEmailAsync(email, "Mã OTP khôi phục mật khẩu", $"Mã OTP của bạn là: {otp}. Hết hạn sau 10 phút.");
			TempData["Success"] = "Đã gửi mã OTP tới email của bạn.";
			return RedirectToAction("ResetPassword", new { email });
		}

		[HttpGet]
		public IActionResult ResetPassword(string email)
		{
			ViewData["Email"] = email; return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(string email, string otp, string newPassword)
		{
			var user = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Email == email && x.TrangThai);
			if (user == null) { ViewBag.Error = "Email không tồn tại"; ViewData["Email"] = email; return View(); }
			if (string.IsNullOrWhiteSpace(otp) || user.ResetOtp != otp || user.ResetOtpExpiryUtc < DateTime.UtcNow)
			{ ViewBag.Error = "Mã OTP không hợp lệ hoặc đã hết hạn"; ViewData["Email"] = email; return View(); }
			if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
			{ ViewBag.Error = "Mật khẩu mới tối thiểu 6 ký tự"; ViewData["Email"] = email; return View(); }
			user.MatKhau = BCrypt.Net.BCrypt.HashPassword(newPassword);
			user.ResetOtp = null; user.ResetOtpExpiryUtc = null; user.ResetOtpAttempts = null;
			await _context.SaveChangesAsync();
			TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập";
			return RedirectToAction("Index");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult GoogleLogin(string? returnUrl)
		{
			var redirectUrl = Url.Action("GoogleCallback", new { returnUrl });
			var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
			return Challenge(properties, GoogleDefaults.AuthenticationScheme);
		}

		[HttpGet]
		public async Task<IActionResult> GoogleCallback(string? returnUrl)
		{
			// After external challenge, user info will be in HttpContext.User when using GoogleDefaults
			var extAuth = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
			if (!extAuth.Succeeded || extAuth.Principal == null)
			{
				TempData["Error"] = "Đăng nhập Google thất bại"; return RedirectToAction("Index");
			}
			var email = extAuth.Principal.FindFirst(ClaimTypes.Email)?.Value;
			var name = extAuth.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email;
			if (string.IsNullOrEmpty(email)) { TempData["Error"] = "Không lấy được email từ Google"; return RedirectToAction("Index"); }
			var user = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null)
			{
				user = new TaiKhoan { TenDangNhap = email, Email = email, MatKhau = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), TrangThai = true, VaiTro = "User" };
				_context.TaiKhoans.Add(user); await _context.SaveChangesAsync();
			}
			var claims = new List<Claim> {
				new Claim(ClaimTypes.NameIdentifier, user.TaiKhoanId.ToString()),
				new Claim(ClaimTypes.Name, name ?? user.TenDangNhap),
				new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
				new Claim(ClaimTypes.Role, string.IsNullOrEmpty(user.VaiTro) ? "User" : user.VaiTro)
			};
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
			return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
		}

		private async Task SendEmailAsync(string toEmail, string subject, string body)
		{
			try
			{
				var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
				var host = config?["EmailSettings:SmtpServer"];
				var port = int.TryParse(config?["EmailSettings:SmtpPort"], out var p) ? p : 587;
				var user = config?["EmailSettings:SenderEmail"];
				var pass = config?["EmailSettings:SenderPassword"];
				var display = config?["EmailSettings:SenderName"] ?? "Phụ Liệu Tóc";
				using var client = new SmtpClient(host, port) { EnableSsl = true, Credentials = new NetworkCredential(user, pass) };
				var mail = new MailMessage(new MailAddress(user!, display), new MailAddress(toEmail)) { Subject = subject, Body = body, IsBodyHtml = false };
				await client.SendMailAsync(mail);
			}
			catch { /* swallow to avoid leaking secrets in UI */ }
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
