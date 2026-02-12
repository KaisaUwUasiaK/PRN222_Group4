using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PRN222_Group4.Models;
using PRN222_Group4.Services;

namespace PRN222_Group4.Controllers;

public class AuthenticationController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;

    public AuthenticationController(AppDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View();
        }

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role.RoleName);

        var roleName = user.Role.RoleName;

        if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Home");
        }
        else if (string.Equals(roleName, "Moderator", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Home");
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullname, string email, string password, string confirmPassword)
    {
        if (!ModelState.IsValid)
        {
            return View();
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
            return View();
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "Email đã được sử dụng.");
            return View();
        }

        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
        if (userRole == null)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy role mặc định 'User'.");
            return View();
        }

        var user = new User
        {
            Username = fullname,
            Email = email,
            PasswordHash = password,
            RoleId = userRole.RoleId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "Đã đăng xuất.";
        return RedirectToAction("Login");
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
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập email.");
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
        if (user != null)
        {
            var token = Guid.NewGuid().ToString("N");
            user.ResetPasswordToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var link = $"{Request.Scheme}://{Request.Host}{Url.Action("ResetPassword", "Authentication", new { token })}";
            var subject = "Đặt lại mật khẩu - ComicVerse";
            var body = $@"<p>Bạn đã yêu cầu đặt lại mật khẩu.</p>
<p>Bấm vào link sau để đặt lại mật khẩu:</p>
<p><a href=""{link}"">Đặt lại mật khẩu</a></p>
<p>Link hết hạn sau 15 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";
            try
            {
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
            catch
            {
                // Don't reveal failure; user still sees same success message
            }
        }

        TempData["Success"] = "Nếu email tồn tại, vui lòng kiểm tra hộp thư (và thư mục spam) để đặt lại mật khẩu.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Link không hợp lệ.";
            return RedirectToAction("ForgotPassword");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetPasswordToken == token && u.ResetTokenExpiry.HasValue && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            TempData["Error"] = "Link không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("ForgotPassword");
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string? token, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Link không hợp lệ.";
            return RedirectToAction("ForgotPassword");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetPasswordToken == token && u.ResetTokenExpiry.HasValue && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            TempData["Error"] = "Link không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("ForgotPassword");
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
            ViewBag.Token = token;
            return View();
        }

        user.PasswordHash = password;
        user.ResetPasswordToken = null;
        user.ResetTokenExpiry = null;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }
}
