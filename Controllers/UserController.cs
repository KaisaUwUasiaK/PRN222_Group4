using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Utils;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public UserController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    private async Task RefreshUserSignInAsync(User user)
    {
        // Reload user with role to build full claims
        var fullUser = await _context.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.UserId == user.UserId);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, fullUser.UserId.ToString()),
            new Claim(ClaimTypes.Name, fullUser.Username),
            new Claim(ClaimTypes.Email, fullUser.Email),
            new Claim(ClaimTypes.Role, fullUser.Role.RoleName)
        };

        if (!string.IsNullOrEmpty(fullUser.AvatarUrl))
        {
            claims.Add(new Claim("AvatarUrl", fullUser.AvatarUrl));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Authentication");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return RedirectToAction("Login", "Authentication");
        }

        var vm = new ProfileUpdateViewModel
        {
            User = user,
            Username = user.Username,
            Bio = user.Bio
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileUpdateViewModel model)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Authentication");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Profile");
        }

        // Ensure view can render even if invalid
        model.User = user;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = ValidationRules.NormalizeSpaces(model.Username);
        var bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();

        // Check if username is already taken by another user
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.UserId != userId);
        // if (existingUser != null)
        // {
        //     ModelState.AddModelError("Username", "Username is already taken.");
        //     return View(model);
        // }

        // Handle password change if provided (validated in view model)
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            user.PasswordHash = model.NewPassword;
        }

        user.Username = username;
        user.Bio = bio;

        await _context.SaveChangesAsync();

        // Re-issue auth cookie so header (claims) reflect latest username/avatar
        await RefreshUserSignInAsync(user);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Authentication");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Profile");
        }

        // If password fields are empty, skip password change
        if (string.IsNullOrWhiteSpace(newPassword) && string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // Validate password fields
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ModelState.AddModelError("NewPassword", "New password is required.");
            return View("Profile", user);
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
            return View("Profile", user);
        }

        if (newPassword.Length < 6)
        {
            ModelState.AddModelError("NewPassword", "Password must be at least 6 characters.");
            return View("Profile", user);
        }

        // Verify current password (if provided)
        if (!string.IsNullOrWhiteSpace(currentPassword) && user.PasswordHash != currentPassword)
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return View("Profile", user);
        }

        // Update password
        user.PasswordHash = newPassword;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Json(new { success = false, message = "User not authenticated." });
        }

        if (avatarFile.Length == 0)
        {
            return Json(new { success = false, message = "No file uploaded." });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return Json(new { success = false, message = "Invalid file type. Only JPG, PNG, and GIF are allowed." });
        }

        // Validate file size (max 5MB)
        if (avatarFile.Length > 5 * 1024 * 1024)
        {
            return Json(new { success = false, message = "File size exceeds 5MB limit." });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        // Create uploads directory if it doesn't exist
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldAvatarPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldAvatarPath))
            {
                System.IO.File.Delete(oldAvatarPath);
            }
        }

        // Generate unique filename
        var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatarFile.CopyToAsync(stream);
        }

        // Update user avatar URL
        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        await _context.SaveChangesAsync();

        // Re-issue auth cookie so header avatar updates immediately
        await RefreshUserSignInAsync(user);

        return Json(new { success = true, avatarUrl = user.AvatarUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string confirmText)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login", "Authentication");
        }

        // Simple confirmation check - user must type "DELETE" to confirm
        if (confirmText != "DELETE")
        {
            TempData["Error"] = "Please type 'DELETE' to confirm account deletion.";
            return RedirectToAction("Profile");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Profile");
        }

        // Delete avatar file if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var avatarPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/'));
            if (System.IO.File.Exists(avatarPath))
            {
                System.IO.File.Delete(avatarPath);
            }
        }

        // Delete user from database
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Sign out user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["Success"] = "Your account has been deleted.";
        return RedirectToAction("Login", "Authentication");
    }
}
