using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models;
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

    // POST: /Admin/CreateMod — create a new Moderator account
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

    // POST: /Admin/BanModerator — ban a Moderator account
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

        // Notify admins panel to update status badge
        await _hubContext.Clients.Group("admins").SendAsync("UserBanned", userId);

        TempData["Success"] = $"Moderator '{user.Username}' has been banned.";
        return RedirectToAction(nameof(Users));
    }

    // POST: /Admin/UnbanModerator — unban a Moderator account
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

        // Notify admins panel
        await _hubContext.Clients.Group("admins").SendAsync("UserOffline", userId);

        TempData["Success"] = $"Moderator '{user.Username}' has been unbanned.";
        return RedirectToAction(nameof(Users));
    }
}
