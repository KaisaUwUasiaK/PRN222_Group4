using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public UserController(IUserService userService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return RedirectToAction("Login", "Authentication");

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return RedirectToAction("Login", "Authentication");

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
            return RedirectToAction("Login", "Authentication");

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Profile");
        }

        // Ensure view can render even if invalid
        model.User = user;

        if (!ModelState.IsValid)
            return View(model);

        var error = await _userService.UpdateProfileAsync(userId, model.Username, model.Bio);
        if (error != null)
        {
            ModelState.AddModelError("Username", error);
            return View(model);
        }

        // Re-issue auth cookie so header (claims) reflect latest username/avatar
        await _userService.RefreshUserSignInAsync(user, HttpContext);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return RedirectToAction("Login", "Authentication");

        var error = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
        if (error != null)
        {
            // Map error to the correct field
            if (error.Contains("Current password"))
                ModelState.AddModelError(nameof(model.CurrentPassword), error);
            else
                ModelState.AddModelError(string.Empty, error);

            return View(model);
        }

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Json(new { success = false, message = "User not authenticated." });

        var (success, errorMessage, avatarUrl) = await _userService.UploadAvatarAsync(
            userId, avatarFile, _environment.WebRootPath);

        if (!success)
            return Json(new { success = false, message = errorMessage });

        // Re-issue auth cookie so header avatar updates immediately
        var user = await _userService.GetUserByIdAsync(userId);
        if (user != null)
            await _userService.RefreshUserSignInAsync(user, HttpContext);

        return Json(new { success = true, avatarUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string confirmText)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return RedirectToAction("Login", "Authentication");

        // Simple confirmation check - user must type "DELETE" to confirm
        if (confirmText != "DELETE")
        {
            TempData["Error"] = "Please type 'DELETE' to confirm account deletion.";
            return RedirectToAction("Profile");
        }

        var deleted = await _userService.DeleteAccountAsync(userId, _environment.WebRootPath);
        if (!deleted)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction("Profile");
        }

        // Sign out user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["Success"] = "Your account has been deleted.";
        return RedirectToAction("Login", "Authentication");
    }
}
