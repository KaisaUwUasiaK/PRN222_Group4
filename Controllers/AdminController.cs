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

        /// <summary>
        /// Constructor: inject các service và SignalR hub context.
        /// </summary>
        public AdminController(
            IAdminService adminService,
            IReportService reportService,
            IHubContext<UserStatusHub> hubContext)
        {
            _adminService = adminService;
            _reportService = reportService;
            _hubContext = hubContext;
        }

        /// <summary>
        /// GET: /Admin/Dashboard
        /// Hiển thị dashboard admin cùng số lượng report moderator đang pending.
        /// </summary>
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var pendingModeratorReports =
                await _reportService.GetPendingModeratorReportsCountAsync();

            ViewBag.PendingModeratorReportsCount = pendingModeratorReports;

            return View();
        }

        /// <summary>
        /// GET: /Admin/Users
        /// Hiển thị danh sách tất cả Moderator.
        /// </summary>
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

        /// <summary>
        /// POST: /Admin/CreateMod
        /// Tạo tài khoản Moderator mới.
        /// Validate username và email trước khi insert.
        /// </summary>
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
                {
                    ModelState.AddModelError(
                        nameof(model.Username),
                        parts.Length > 2 ? parts[2] : error);
                }
                else if (parts.Length > 0 && parts[0] == "email")
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        parts.Length > 2 ? parts[2] : error);
                }
                else
                {
                    TempData["Error"] = parts.Length > 1 ? parts[1] : error;
                }

                var mods = await _adminService.GetAllModeratorsAsync();
                ViewBag.CreateModViewModel = model;

                return View("Users", mods);
            }

            TempData["Success"] =
                $"Moderator account '{model.Username.Trim()}' created successfully.";

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// POST: /Admin/BanMod
        /// Ban tài khoản Moderator.
        /// Gửi SignalR event để cập nhật real-time.
        /// </summary>
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

            // Notify admin dashboard clients
            await _hubContext.Clients.Group("admins")
                .SendAsync("UserBanned", userId);

            // Notify all clients (profile indicator, comment badge, etc.)
            await _hubContext.Clients.All
                .SendAsync("UserStatusChanged", userId, "Banned");

            // NEW: Force logout the banned user immediately
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ForceLogout");

            TempData["Success"] =
                $"Moderator '{user.Username}' has been banned.";

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// POST: /Admin/UnbanMod
        /// Unban tài khoản Moderator (restore về Offline).
        /// Gửi SignalR event để cập nhật real-time.
        /// </summary>
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

            // Notify admin dashboard clients
            await _hubContext.Clients.Group("admins")
                .SendAsync("UserOffline", userId);

            // Notify all clients
            await _hubContext.Clients.All
                .SendAsync("UserStatusChanged", userId, "Offline");

            TempData["Success"] =
                $"Moderator '{user.Username}' has been unbanned.";

            return RedirectToAction(nameof(Users));
        }
    }
}