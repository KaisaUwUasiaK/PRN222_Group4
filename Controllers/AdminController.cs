using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IHubContext<UserStatusHub> _hubContext;

    public AdminController(IAdminService adminService, IHubContext<UserStatusHub> hubContext)
    {
        _adminService = adminService;
        _hubContext = hubContext;
    }

    // GET: /Admin/Dashboard
    public IActionResult Dashboard()
    {
        return View();
    }

    // GET: /Admin/Users — list all Moderators only
    public async Task<IActionResult> Users()
    {
        var moderators = await _adminService.GetAllModeratorsAsync();
        if (moderators == null)
        {
            TempData["Error"] = "Moderator role not found in database.";
            return View(new List<Group4_ReadingComicWeb.Models.User>());
        }

        ViewBag.CreateModViewModel = new CreateModViewModel();
        return View(moderators);
    }

    /// <summary>
    /// Creates a new Moderator account.
    /// Validates uniqueness of username and email before inserting.
    /// On validation failure, re-renders the Users view with the current moderator list.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMod(CreateModViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var mods = await _adminService.GetAllModeratorsAsync();
            ViewBag.CreateModViewModel = model;
            return View("Users", mods);
        }

        var error = await _adminService.CreateModeratorAsync(model);
        if (error != null)
        {
            // Error format: "field:FieldName:Message" or "role:Message"
            var parts = error.Split(':', 3);
            if (parts[0] == "username")
                ModelState.AddModelError(nameof(model.Username), parts[2]);
            else if (parts[0] == "email")
                ModelState.AddModelError(nameof(model.Email), parts[2]);
            else
                TempData["Error"] = parts.Length > 1 ? parts[1] : error;

            var mods = await _adminService.GetAllModeratorsAsync();
            ViewBag.CreateModViewModel = model;
            return View("Users", mods);
        }

        TempData["Success"] = $"Moderator account '{model.Username.Trim()}' created successfully.";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Bans a Moderator account. Verifies the target is a Moderator before applying.
    /// Broadcasts a SignalR event to update the status badge in real-time on the admin panel.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanMod(int userId)
    {
        var user = await _adminService.BanModeratorAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "Moderator not found.";
            return RedirectToAction(nameof(Users));
        }

        // Notify all admin clients to update the status badge in real-time
        await _hubContext.Clients.Group("admins").SendAsync("UserBanned", userId);
        // Also notify everyone for the public profile indicator
        await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, "Banned");

        TempData["Success"] = $"Moderator '{user.Username}' has been banned.";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Unbans a Moderator account, restoring their status to Offline.
    /// Broadcasts a SignalR event to update the status badge in real-time on the admin panel.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbanMod(int userId)
    {
        var user = await _adminService.UnbanModeratorAsync(userId);
        if (user == null)
        {
            TempData["Error"] = "Moderator not found.";
            return RedirectToAction(nameof(Users));
        }

        // Notify all admin clients to update the status badge in real-time
        await _hubContext.Clients.Group("admins").SendAsync("UserOffline", userId);
        // Also notify everyone for the public profile indicator
        await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, "Offline");

        TempData["Success"] = $"Moderator '{user.Username}' has been unbanned.";
        return RedirectToAction(nameof(Users));
    }
}
