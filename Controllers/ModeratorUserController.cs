using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Controllers
{
    /// <summary>
    /// Controller cho Moderator quản lý trạng thái tài khoản User (không gồm Admin/Moderator).
    /// </summary>
    [Authorize(Roles = "Moderator")]
    [Route("ModeratorUsers")]
    public class ModeratorUserController : Controller
    {
        private readonly IModeratorUserService _moderatorUserService;
        private readonly IModerationService _moderationService;
        private readonly IReportService _reportService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<UserStatusHub> _hubContext;

        public ModeratorUserController(
            IModeratorUserService moderatorUserService,
            IModerationService moderationService,
            IReportService reportService,
            INotificationService notificationService,
            IHubContext<UserStatusHub> hubContext)
        {
            _moderatorUserService = moderatorUserService;
            _moderationService = moderationService;
            _reportService = reportService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Set badge sidebar cho layout Moderator.
        /// </summary>
        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        /// <summary>
        /// GET: /ModeratorUsers
        /// Hiển thị danh sách User để Moderator quản lý trạng thái.
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            await SetSidebarBadgesAsync();
            var users = await _moderatorUserService.GetAllUsersAsync();
            return View(users);
        }

        /// <summary>
        /// POST: /ModeratorUsers/Ban
        /// Moderator không được ban trực tiếp tại màn quản lý User.
        /// Ban chỉ thực hiện qua luồng xử lý Report.
        /// </summary>
        [HttpPost("Ban")]
        [ValidateAntiForgeryToken]
        public IActionResult Ban(int userId)
        {
            TempData["Error"] = "Direct ban is disabled here. Please process violations through the Report workflow.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: /ModeratorUsers/Unban
        /// Gỡ ban tài khoản User và broadcast trạng thái real-time qua SignalR.
        /// Đồng thời gửi notification cho user được unban và log notification cho moderator thao tác.
        /// </summary>
        [HttpPost("Unban")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unban(int userId)
        {
            var user = await _moderatorUserService.UnbanUserAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found, invalid role, or account is not banned.";
                return RedirectToAction(nameof(Index));
            }

            await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, nameof(AccountStatus.Offline));
            await _hubContext.Clients.All.SendAsync("UserOffline", userId);

            // Notification cho user vừa được mở khóa
            await _notificationService.AccountUnbannedAsync(userId);

            // Notification ghi nhận sự kiện cho moderator thao tác
            var moderatorIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            if (int.TryParse(moderatorIdValue, out int moderatorId) && moderatorId > 0)
            {
                await _notificationService.SendAsync(
                    moderatorId,
                    "User unbanned",
                    $"You unbanned user '{user.Username}' (ID: {user.UserId}).",
                    "system",
                    Url.Action("Index", "ModeratorUser"));
            }

            TempData["Success"] = $"User '{user.Username}' has been unbanned.";
            return RedirectToAction(nameof(Index));
        }
    }
}