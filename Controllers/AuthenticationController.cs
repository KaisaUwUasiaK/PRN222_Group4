using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services;
using Group4_ReadingComicWeb.Utils;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Controllers;

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

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AccountLocked()
    {
        return View();
    }

    /// <summary>
    /// Handles user login. Validates credentials, checks ban status,
    /// builds cookie claims, and redirects based on role:
    /// Admin → Admin Dashboard, Moderator → Moderation Dashboard, User → Home.
    /// </summary>
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
            .FirstOrDefaultAsync(u => u.Email == email);

        // Verify credentials — use BCrypt to compare hashed password
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View();
        }

        // Prevent banned accounts from logging in
        if (user.Status == AccountStatus.Banned)
        {
            return RedirectToAction("AccountLocked");
        }

        user.Status = AccountStatus.Online;
        await _context.SaveChangesAsync();

        // Build claims for the authentication cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };

        // Include avatar URL in claims so layouts can display it without a DB query
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            claims.Add(new Claim("AvatarUrl", user.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        // Redirect to role-specific landing page
        if (user.Role.RoleName == "Admin")
            return RedirectToAction("Dashboard", "Admin");

        if (user.Role.RoleName == "Moderator")
            return RedirectToAction("Dashboard", "Moderation");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var fullname = ValidationRules.NormalizeSpaces(model.Fullname);
        var email = model.Email.Trim();
        var password = model.Password;

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email is already in use.");
            return View(model);
        }

        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
        if (userRole == null)
        {
            ModelState.AddModelError(string.Empty, "Default role 'User' not found.");
            return View(model);
        }

        var user = new User
        {
            Username = fullname,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = userRole.RoleId,
            Status = AccountStatus.Offline
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Registration successful! Please log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = AccountStatus.Offline;
                await _context.SaveChangesAsync();
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Logged out successfully.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    /// <summary>
    /// Generates a password reset token and emails a reset link to the user.
    /// Always returns a success message regardless of whether the email exists
    /// to prevent user enumeration attacks.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Please enter your email.");
            return View();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
        if (user != null)
        {
            // Generate a secure one-time token valid for 15 minutes
            var token = Guid.NewGuid().ToString("N");
            user.ResetPasswordToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var link =
                $"{Request.Scheme}://{Request.Host}{Url.Action("ResetPassword", "Authentication", new { token })}";
            var subject = "Reset Your Password - ComicVerse";
            var body = $@"<p>You requested to reset your password.</p>
<p>Click the link below to reset your password:</p>
<p><a href=""{link}"">Reset Password</a></p>
<p>This link will expire in 15 minutes. If you did not request this, please ignore this email.</p>";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
            catch
            {
            }
        }

        TempData["Success"] = "If the email exists, please check your inbox (and spam folder) to reset your password.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Invalid link.";
            return RedirectToAction("ForgotPassword");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetPasswordToken == token &&
            u.ResetTokenExpiry.HasValue &&
            u.ResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            TempData["Error"] = "Invalid or expired link.";
            return RedirectToAction("ForgotPassword");
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string? token, ResetPasswordViewModel model)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Invalid link.";
            return RedirectToAction("ForgotPassword");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetPasswordToken == token &&
            u.ResetTokenExpiry.HasValue &&
            u.ResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            TempData["Error"] = "Invalid or expired link.";
            return RedirectToAction("ForgotPassword");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Token = token;
            return View(model);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        user.ResetPasswordToken = null;
        user.ResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Password reset successful. Please log in.";
        return RedirectToAction("Login");
    }
}
