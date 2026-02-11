using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PRN222_Group4.Models;

namespace PRN222_Group4.Controllers;

public class AuthenticationController : Controller
{
    private readonly AppDbContext _context;

    public AuthenticationController(AppDbContext context)
    {
        _context = context;
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

        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
