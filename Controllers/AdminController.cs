using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;
using Group4_ReadingComicWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IReportService _reportService;
        private readonly IHubContext<UserStatusHub> _hubContext;
        private readonly INotificationService _notifService;

        public AdminController(
            IAdminService adminService,
            IReportService reportService,
            IHubContext<UserStatusHub> hubContext,
            INotificationService notifService)
        {
            _adminService = adminService;
            _reportService = reportService;
            _hubContext = hubContext;
            _notifService = notifService;
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var pendingModeratorReports = await _reportService.GetPendingModeratorReportsCountAsync();
            ViewBag.PendingModeratorReportsCount = pendingModeratorReports;
            return View();
        }

        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            var moderators = await _adminService.GetAllModeratorsAsync();

            if (moderators == null)
            {
                TempData["Error"] = "Moderator role not found in database.";
                return View(new List<User>());
            }

            ViewBag.CreateModViewModel = new CreateModViewModel();
            return View(moderators);
        }

        [HttpPost("CreateMod")]
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
                var parts = error.Split(':', 3);

                if (parts.Length > 0 && parts[0] == "username")
                    ModelState.AddModelError(nameof(model.Username), parts.Length > 2 ? parts[2] : error);
                else if (parts.Length > 0 && parts[0] == "email")
                    ModelState.AddModelError(nameof(model.Email), parts.Length > 2 ? parts[2] : error);
                else
                    TempData["Error"] = parts.Length > 1 ? parts[1] : error;

                var mods = await _adminService.GetAllModeratorsAsync();
                ViewBag.CreateModViewModel = model;
                return View("Users", mods);
            }

            TempData["Success"] = $"Moderator account '{model.Username.Trim()}' created successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost("BanMod")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanMod(int userId)
        {
            var user = await _adminService.BanModeratorAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Moderator not found.";
                return RedirectToAction(nameof(Users));
            }

            // SignalR: cập nhật trạng thái real-time trên dashboard
            await _hubContext.Clients.Group("admins").SendAsync("UserBanned", userId);
            await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, "Banned");

            // Notification: gửi thông báo vào panel của Moderator bị ban
            await _notifService.AccountBannedAsync(userId, "Tài khoản bị Admin khoá.");

            TempData["Success"] = $"Moderator '{user.Username}' has been banned.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost("UnbanMod")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanMod(int userId)
        {
            var user = await _adminService.UnbanModeratorAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Moderator not found.";
                return RedirectToAction(nameof(Users));
            }

            // SignalR: cập nhật trạng thái real-time trên dashboard
            await _hubContext.Clients.Group("admins").SendAsync("UserOffline", userId);
            await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, "Offline");

            // Notification: gửi thông báo mở khoá cho Moderator
            await _notifService.AccountUnbannedAsync(userId);

            TempData["Success"] = $"Moderator '{user.Username}' has been unbanned.";
            return RedirectToAction(nameof(Users));
        }
    }
}