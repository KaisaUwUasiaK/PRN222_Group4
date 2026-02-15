using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Controllers
{
    // [Authorize(Roles = "Admin")] // Uncomment khi deploy
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ===== DEV LOGIN =====
        [AllowAnonymous]
        public async Task<IActionResult> DevLogin()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "DevAdmin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Dashboard");
        }

        // ===== DASHBOARD =====
        public async Task<IActionResult> Dashboard()
        {
            var viewModel = new DashboardViewModel();

            // Comics
            try
            {
                viewModel.TotalComics = await _context.Comics.CountAsync();
                viewModel.TotalViews = await _context.Comics.SumAsync(c => (long)c.ViewCount);
            }
            catch
            {
                viewModel.TotalComics = 0;
                viewModel.TotalViews = 0;
            }

            // Users
            viewModel.TotalUsers = await _context.Users
                .Where(u => u.RoleId == 3)
                .CountAsync();

            // Moderators (DÙNG Status)
            viewModel.TotalModerators = await _context.Users
                .Where(u => u.RoleId == 2)
                .CountAsync();

            viewModel.ActiveModerators = await _context.Users
                .Where(u => u.RoleId == 2 && u.Status != AccountStatus.Banned)
                .CountAsync();

            viewModel.BannedModerators = await _context.Users
                .Where(u => u.RoleId == 2 && u.Status == AccountStatus.Banned)
                .CountAsync();

            // Logs
            try
            {
                viewModel.RecentLogs = await _context.Logs
                    .Include(l => l.User)
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(10)
                    .Select(l => new LogViewModel
                    {
                        LogId = l.LogId,
                        AdminUsername = l.User.Username,
                        Action = l.Action,
                        CreatedAt = l.CreatedAt
                    })
                    .ToListAsync();
            }
            catch
            {
                viewModel.RecentLogs = new List<LogViewModel>();
            }

            return View(viewModel);
        }

        public async Task<IActionResult> ManageMod()
        {
            var viewModel = new ManageModViewModel
            {
                TotalModerators = await _context.Users
                    .Where(u => u.RoleId == 2)
                    .CountAsync(),

                ActiveModerators = await _context.Users
                    .Where(u => u.RoleId == 2 && u.Status != AccountStatus.Banned)
                    .CountAsync(),

                BannedModerators = await _context.Users
                    .Where(u => u.RoleId == 2 && u.Status == AccountStatus.Banned)
                    .CountAsync(),

                Moderators = await _context.Users
                    .Where(u => u.RoleId == 2)
                    .OrderByDescending(u => u.UserId)
                    .Select(u => new ModeratorDetailViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        PasswordHash = u.PasswordHash,
                        CreatedAt = DateTime.Now,
                        Status = u.Status
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // ===== BAN MODERATOR =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanModerator(int userId)
        {
            var moderator = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.RoleId == 2);

            if (moderator == null)
            {
                TempData["Error"] = "Moderator not found.";
                return RedirectToAction(nameof(ManageMod));
            }

            if (moderator.Status == AccountStatus.Banned)
            {
                TempData["Error"] = $"{moderator.Username} is already banned.";
                return RedirectToAction(nameof(ManageMod));
            }

            // Update Status
            moderator.Status = AccountStatus.Banned;

            // Log action
            try
            {
                var log = new Log
                {
                    UserId = GetCurrentUserId(),
                    Action = $"Banned moderator '{moderator.Username}' (ID: {moderator.UserId})",
                    CreatedAt = DateTime.Now
                };
                _context.Logs.Add(log);
            }
            catch
            {
                // Ignore if Log table not exists
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully banned {moderator.Username}!";
            return RedirectToAction(nameof(ManageMod));
        }

        // ===== UNBAN MODERATOR =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanModerator(int userId)
        {
            var moderator = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.RoleId == 2);

            if (moderator == null)
            {
                TempData["Error"] = "Moderator not found.";
                return RedirectToAction(nameof(ManageMod));
            }

            if (moderator.Status != AccountStatus.Banned)
            {
                TempData["Error"] = $"{moderator.Username} is not banned.";
                return RedirectToAction(nameof(ManageMod));
            }

            // Update Status
            moderator.Status = AccountStatus.Offline;

            // Log action
            try
            {
                var log = new Log
                {
                    UserId = GetCurrentUserId(),
                    Action = $"Unbanned moderator '{moderator.Username}' (ID: {moderator.UserId})",
                    CreatedAt = DateTime.Now
                };
                _context.Logs.Add(log);
            }
            catch
            {
                // Ignore
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully unbanned {moderator.Username}!";
            return RedirectToAction(nameof(ManageMod));
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 1;
        }
    }
}