using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Controllers
{
    /// <summary>
    /// Controller xử lý luồng report (báo cáo vi phạm).
    /// Dùng chung cho cả Moderator và Admin, phân quyền theo role:
    /// - Moderator: xem/xử lý report User (route: /Reports/Users).
    /// - Admin: xem/xử lý report Moderator (route: /Reports/Moderators).
    /// - Mọi user đăng nhập: tạo report mới (route: POST /Reports/Create).
    /// Route gốc: /Reports
    /// </summary>
    [Authorize]
    [Route("Reports")]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IModerationService _moderationService;

        public ReportController(
            IReportService reportService,
            IModerationService moderationService)
        {
            _reportService = reportService;
            _moderationService = moderationService;
        }

        /// <summary>
        /// Set ViewBag cho sidebar badges trên layout moderator.
        /// Gọi ở các action mà Moderator truy cập (dùng _ModeratorLayout).
        /// Không gọi ở action Admin (dùng _AdminLayout riêng).
        /// </summary>
        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        /// <summary>
        /// GET: /Reports/Users
        /// [Moderator only] Danh sách report Pending nhắm vào User thường.
        /// Hiển thị: Reporter, Target User, Reason, Description, Date, nút Review.
        /// Model: List&lt;Report&gt;.
        /// </summary>
        [Authorize(Roles = "Moderator")]
        [HttpGet("Users")]
        public async Task<IActionResult> UserReports()
        {
            await SetSidebarBadgesAsync();
            var reports = await _reportService.GetUserReportsAsync();
            return View(reports);
        }

        /// <summary>
        /// GET: /Reports/Moderators
        /// [Admin only] Danh sách report Pending nhắm vào Moderator.
        /// Model: List&lt;Report&gt;. Dùng layout Admin (không cần SetSidebarBadgesAsync).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("Moderators")]
        public async Task<IActionResult> ModeratorReports()
        {
            var reports = await _reportService.GetModeratorReportsAsync();
            return View(reports);
        }

        /// <summary>
        /// GET: /Reports/{id}
        /// Chi tiết một report. Phân quyền:
        /// - Moderator chỉ xem report nhắm vào User (target role = "User").
        /// - Admin chỉ xem report nhắm vào Moderator (target role = "Moderator").
        /// Nếu vi phạm quyền → trả về Forbid (403).
        /// Set sidebar badges nếu người xem là Moderator.
        /// Model: Report.
        /// </summary>
        /// <param name="id">ReportId cần xem chi tiết.</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Moderator" && report.TargetUser.Role.RoleName != "User")
                return Forbid();

            if (userRole == "Admin" && report.TargetUser.Role.RoleName == "User")
                return Forbid();

            // Set sidebar badges nếu moderator đang xem (dùng layout moderator)
            if (userRole == "Moderator")
                await SetSidebarBadgesAsync();

            return View(report);
        }

        /// <summary>
        /// POST: /Reports/{id}/Process
        /// Xử lý report với hành động: Warning, Ban, RemoveRole, hoặc Dismiss.
        /// Phân quyền tương tự Details (Moderator → User, Admin → Moderator).
        /// Sau khi xử lý → redirect về danh sách report tương ứng.
        /// </summary>
        /// <param name="id">ReportId cần xử lý.</param>
        /// <param name="action">Hành động xử lý (enum ReportAction).</param>
        /// <param name="note">Ghi chú xử lý (tùy chọn).</param>
        [HttpPost("{id}/Process")]
        public async Task<IActionResult> ProcessReport(int id, ReportAction action, string? note)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

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

        /// <summary>
        /// POST: /Reports/{id}/Reject
        /// Từ chối report (đánh dấu không hợp lệ, không xử phạt target user).
        /// Sau khi reject → redirect về danh sách report tương ứng theo role.
        /// </summary>
        /// <param name="id">ReportId cần reject.</param>
        /// <param name="note">Ghi chú lý do reject (tùy chọn).</param>
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

        /// <summary>
        /// POST: /Reports/Create
        /// Tạo report mới từ bất kỳ user đã đăng nhập.
        /// Lấy reporterId từ claim NameIdentifier.
        /// Thành công → redirect về Home. Thất bại → quay lại trang trước.
        /// Lỗi có thể do: tự report mình, hoặc đã có report Pending trùng.
        /// </summary>
        /// <param name="targetUserId">UserId của người bị report.</param>
        /// <param name="reason">Lý do report.</param>
        /// <param name="description">Mô tả bổ sung (tùy chọn).</param>
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