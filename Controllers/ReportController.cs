using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Models;
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

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // MODERATOR: Xem report User
        [Authorize(Roles = "Moderator")]
        [HttpGet("Users")]
        public async Task<IActionResult> UserReports()
        {
            var reports = await _reportService.GetUserReportsAsync();
            return View(reports);
        }

        // ADMIN: Xem report Moderator
        [Authorize(Roles = "Admin")]
        [HttpGet("Moderators")]
        public async Task<IActionResult> ModeratorReports()
        {
            var reports = await _reportService.GetModeratorReportsAsync();
            return View(reports);
        }

        // Chi tiết report
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();

            // Kiểm tra quyền: Moderator chỉ xem report User, Admin xem report Moderator
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User")
                return Forbid();

            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User")
                return Forbid();

            return View(report);
        }

        // Xử lý report
        [HttpPost("{id}/Process")]
        public async Task<IActionResult> ProcessReport(int id, ReportAction action, string? note)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Kiểm tra quyền
            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User")
                return Forbid();

            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User")
                return Forbid();

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

        // Reject report
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

        // Tạo report mới
        [HttpPost("Create")]
        public async Task<IActionResult> CreateReport(int targetUserId, string reason, string? description)
        {
            var reporterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var success = await _reportService.CreateReportAsync(reporterId, targetUserId, reason, description);

            if (success)
            {
                TempData["Success"] = "Report submitted successfully.";
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Failed to submit report. You may have already reported this user.";
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}