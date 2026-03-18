using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize]
    [Route("Reports")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IModerationService _moderationService;
        private readonly INotificationService _notifService;
        private readonly AppDbContext _db;

        public ReportController(
            IReportService reportService,
            IModerationService moderationService,
            INotificationService notifService,
            AppDbContext db)
        {
            _reportService = reportService;
            _moderationService = moderationService;
            _notifService = notifService;
            _db = db;
        }

        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        private async Task SetAdminSidebarBadgesAsync()
        {
            ViewBag.PendingModeratorReportsCount = await _reportService.GetPendingModeratorReportsCountAsync();
        }

        [Authorize(Roles = "Moderator")]
        [HttpGet("Users")]
        public async Task<IActionResult> UserReports()
        {
            await SetSidebarBadgesAsync();
            var reports = await _reportService.GetUserReportsAsync();
            return View(reports);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Moderators")]
        public async Task<IActionResult> ModeratorReports()
        {
            await SetAdminSidebarBadgesAsync();
            var reports = await _reportService.GetModeratorReportsAsync();
            return View(reports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null) return NotFound();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User") return Forbid();
            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User") return Forbid();

            if (userRole == "Moderator") await SetSidebarBadgesAsync();
            else if (userRole == "Admin") await SetAdminSidebarBadgesAsync();

            return View(report);
        }

        [HttpPost("{id}/Process")]
        public async Task<IActionResult> ProcessReport(int id, ReportAction action, string? note)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null) return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User") return Forbid();
            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User") return Forbid();

            var success = await _reportService.ProcessReportAsync(id, userId, action, note);

            if (success)
            {
                TempData["Success"] = "Report processed successfully.";
                return userRole == "Admin"
                    ? RedirectToAction("ModeratorReports")
                    : RedirectToAction("UserReports");
            }

            TempData["Error"] = "Failed to process report.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("{id}/Reject")]
        public async Task<IActionResult> RejectReport(int id, string? note)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var success = await _reportService.RejectReportAsync(id, userId, note);

            if (success)
            {
                TempData["Success"] = "Report rejected.";
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                return userRole == "Admin"
                    ? RedirectToAction("ModeratorReports")
                    : RedirectToAction("UserReports");
            }

            TempData["Error"] = "Failed to reject report.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateReport(int targetUserId, string reason, string? description, int? commentId)
        {
            var reporterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var success = await _reportService.CreateReportAsync(reporterId, targetUserId, reason, description, commentId);

            if (success)
            {
                // Lấy report vừa tạo để có ReportId
                var newReport = await _db.Reports
                    .Where(r => r.ReporterId == reporterId && r.TargetUserId == targetUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (newReport != null)
                {
                    // Lấy tên target user cho nội dung thông báo
                    var targetUser = await _db.Users.FindAsync(targetUserId);
                    var targetName = targetUser?.Username ?? $"User #{targetUserId}";

                    // Gửi thông báo cho Admin (không phải Moderator)
                    var adminIds = await _db.Users
                        .Where(u => u.Role.RoleName == "Admin")
                        .Select(u => u.UserId)
                        .ToListAsync();

                    foreach (var adminId in adminIds)
                        await _notifService.NewReportAsync(
                            adminId,
                            newReport.ReportId,
                            $"bình luận của @{targetName}");
                }

                TempData["Success"] = "Report submitted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to submit report. You may have already reported this user.";
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}