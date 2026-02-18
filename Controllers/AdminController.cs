using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly IHubContext<UserStatusHub> _hubContext;

    public AdminController(AppDbContext context, IHubContext<UserStatusHub> hubContext)
    {
        _context = context;
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
        // Find the Moderator role
        var ModeratorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
        if (ModeratorRole == null)
        {
            TempData["Error"] = "Moderator role not found in database.";
            return View(new List<User>());
        }

        var Moderators = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.RoleId == ModeratorRole.RoleId)
            .ToListAsync();

        ViewBag.CreateModViewModel = new CreateModViewModel();
        return View(Moderators);
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
            var modRole2 = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
            var mods2 = modRole2 != null
                ? await _context.Users.Include(u => u.Role).Where(u => u.RoleId == modRole2.RoleId).ToListAsync()
                : new List<User>();
            ViewBag.CreateModViewModel = model;
            return View("Users", mods2);
        }

        // Check duplicate username
        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError(nameof(model.Username), "Username is already taken.");
            var modRole2 = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
            var mods2 = modRole2 != null
                ? await _context.Users.Include(u => u.Role).Where(u => u.RoleId == modRole2.RoleId).ToListAsync()
                : new List<User>();
            ViewBag.CreateModViewModel = model;
            return View("Users", mods2);
        }

        // Check duplicate email
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            var modRole2 = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
            var mods2 = modRole2 != null
                ? await _context.Users.Include(u => u.Role).Where(u => u.RoleId == modRole2.RoleId).ToListAsync()
                : new List<User>();
            ViewBag.CreateModViewModel = model;
            return View("Users", mods2);
        }

        var ModeratorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
        if (ModeratorRole == null)
        {
            TempData["Error"] = "Moderator role not found in database.";
            return RedirectToAction(nameof(Users));
        }

        var newModerator = new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            RoleId = ModeratorRole.RoleId,
            Status = AccountStatus.Offline
        };

        _context.Users.Add(newModerator);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Moderator account '{newModerator.Username}' created successfully.";
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
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null || user.Role.RoleName != "Moderator")
        {
            TempData["Error"] = "Moderator not found.";
            return RedirectToAction(nameof(Users));
        }

        user.Status = AccountStatus.Banned;
        await _context.SaveChangesAsync();

        // Notify all admin clients to update the status badge in real-time
        await _hubContext.Clients.Group("admins").SendAsync("UserBanned", userId);

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
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null || user.Role.RoleName != "Moderator")
        {
            TempData["Error"] = "Moderator not found.";
            return RedirectToAction(nameof(Users));
        }

        user.Status = AccountStatus.Offline;
        await _context.SaveChangesAsync();

        // Notify all admin clients to update the status badge in real-time
        await _hubContext.Clients.Group("admins").SendAsync("UserOffline", userId);

        TempData["Success"] = $"Moderator '{user.Username}' has been unbanned.";
        return RedirectToAction(nameof(Users));
    }
}
