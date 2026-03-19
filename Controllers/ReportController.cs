using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
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

        public ReportController(
            IReportService reportService,
            IModerationService moderationService,
            INotificationService notifService)
        {
            _reportService = reportService;
            _moderationService = moderationService;
            _notifService = notifService;
        }

        // ── Sidebar helpers ───────────────────────────────────────────────────

        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        private async Task SetAdminSidebarBadgesAsync()
        {
            ViewBag.PendingModeratorReportsCount =
                await _reportService.GetPendingModeratorReportsCountAsync();
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private string? GetUserRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value;

        // ── GET ───────────────────────────────────────────────────────────────

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

            var userRole = GetUserRole();
            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User")
                return Forbid();
            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User")
                return Forbid();

            if (userRole == "Moderator") await SetSidebarBadgesAsync();
            else if (userRole == "Admin") await SetAdminSidebarBadgesAsync();

            return View(report);
        }

        // ── POST ──────────────────────────────────────────────────────────────

        [HttpPost("{id}/Process")]
        public async Task<IActionResult> ProcessReport(int id, ReportAction action, string? note)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null) return NotFound();

            var userRole = GetUserRole();
            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User")
                return Forbid();
            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User")
                return Forbid();

            var success = await _reportService.ProcessReportAsync(id, GetUserId(), action, note);

            if (success)
            {
                // Notify TARGET (người bị báo cáo)
                switch (action)
                {
                    case ReportAction.Warning:
                        await _notifService.AccountWarningAsync(
                            report.TargetUserId,
                            $"Terms of service violation: {report.Reason}. {note}");
                        break;

                    case ReportAction.Ban:
                        await _notifService.AccountBannedAsync(
                            report.TargetUserId,
                            $"Terms of service violation: {report.Reason}. {note}");
                        break;

                    case ReportAction.RemoveRole:
                        await _notifService.AccountWarningAsync(
                            report.TargetUserId,
                            $"Your Moderator role has been removed due to a violation: {report.Reason}. {note}");
                        break;
                        // Dismiss: không notify target
                }

                // Notify REPORTER (người đã tố cáo)
                string actionLabel = action switch
                {
                    ReportAction.Warning => "The reported user has been warned",
                    ReportAction.Ban => "The reported user has been suspended",
                    ReportAction.RemoveRole => "The reported user's Moderator role has been removed",
                    ReportAction.Dismiss => "The report was reviewed but no action was taken",
                    _ => "Processed"
                };

                await _notifService.ReportHandledNotifyReporterAsync(
                    report.ReporterId,
                    report.TargetUser.Username,
                    actionLabel,
                    note,
                    id);

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
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null) return NotFound();

            var success = await _reportService.RejectReportAsync(id, GetUserId(), note);

            if (success)
            {
                await _notifService.ReportRejectedNotifyReporterAsync(report.ReporterId, id);

                TempData["Success"] = "Report rejected.";
                var userRole = GetUserRole();
                return userRole == "Admin"
                    ? RedirectToAction("ModeratorReports")
                    : RedirectToAction("UserReports");
            }

            TempData["Error"] = "Failed to reject report.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateReport(
            int targetUserId,
            string reason,
            string? description,
            int? commentId)
        {
            var reporterId = GetUserId();

            var reportId = await _reportService.CreateReportAsync(
                reporterId, targetUserId, reason, description, commentId);

            if (reportId.HasValue)
            {
                // Lấy role của target để gửi notification đúng nơi:
                // target là User  → notify Moderator
                // target là Mod   → notify Admin
                var targetRole = await _reportService.GetTargetRoleAsync(targetUserId);

                string reportedContent = commentId.HasValue
                    ? "a user comment"
                    : "user behavior";

                if (targetRole == "Moderator")
                    await _notifService.NewReportForAllAdminsAsync(
                        reportId.Value, reportedContent);
                else
                    await _notifService.NewReportForAllModeratorsAsync(
                        reportId.Value, reportedContent);

                TempData["Success"] = "Report submitted successfully.";
            }
            else
            {
                TempData["Error"] =
                    "Failed to submit report. You may have already reported this user.";
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}