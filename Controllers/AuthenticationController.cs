using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Controllers;

public class AuthenticationController : Controller
{
    private readonly IAuthService _authService;

    public AuthenticationController(IAuthService authService)
    {
        _authService = authService;
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
            return View();

        var (user, error) = await _authService.LoginAsync(email, password, rememberMe, HttpContext);

        if (error == "banned")
            return RedirectToAction("AccountLocked");

        if (user == null || error != null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Invalid email or password.");
            return View();
        }

        // Redirect to role-specific landing page
        return user.Role.RoleName switch
        {
            "Admin" => RedirectToAction("Dashboard", "Admin"),
            "Moderator" => RedirectToAction("Dashboard", "Moderation"),
            _ => RedirectToAction("Index", "Home")
        };
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
            return View(model);

        var error = await _authService.RegisterAsync(model);

        if (error != null)
        {
            // Map error message to the correct field
            if (error.Contains("Email"))
                ModelState.AddModelError(nameof(RegisterViewModel.Email), error);
            else
                ModelState.AddModelError(string.Empty, error);

            return View(model);
        }

        TempData["Success"] = "Registration successful! Please log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            await _authService.LogoutAsync(userId, HttpContext);
        }

        TempData["Success"] = "Logged out successfully.";
        return RedirectToAction("Index", "Home");
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

        // Build the base URL for the reset link (e.g. https://host/Authentication/ResetPassword)
        var resetBaseUrl = Url.Action("ResetPassword", "Authentication",
            null, Request.Scheme, Request.Host.ToString())!;

        await _authService.SendPasswordResetEmailAsync(email, resetBaseUrl);

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

        var user = await _authService.GetUserByResetTokenAsync(token);
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

        if (!ModelState.IsValid)
        {
            ViewBag.Token = token;
            return View(model);
        }

        var success = await _authService.ResetPasswordAsync(token, model.Password);
        if (!success)
        {
            TempData["Error"] = "Invalid or expired link.";
            return RedirectToAction("ForgotPassword");
        }

        TempData["Success"] = "Password reset successful. Please log in.";
        return RedirectToAction("Login");
    }
}
