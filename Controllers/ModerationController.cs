using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group4_ReadingComicWeb.Controllers
{
    /// <summary>
    /// Controller xử lý toàn bộ luồng kiểm duyệt truyện cho Moderator.
    /// Yêu cầu đăng nhập với role "Moderator" (Authorize attribute).
    /// Route gốc: /Moderation
    /// Các trang: Dashboard, Pending (danh sách chờ), Review (chi tiết), ReadChapter (xem nội dung chapter).
    /// Các hành động: Approve, Reject (cần lý do), Hide (cần lý do).
    /// </summary>
    [Authorize(Roles = "Moderator")]
    [Route("Moderation")]
    public class ModerationController : Controller
    {
        private readonly IModerationService _moderationService;
        private readonly IReportService _reportService;

        public ModerationController(
            IModerationService moderationService,
            IReportService reportService)
        {
            _moderationService = moderationService;
            _reportService = reportService;
        }

        /// <summary>
        /// Set ViewBag cho sidebar badges trên layout moderator.
        /// Gọi ở mọi action trả về View để sidebar luôn hiển thị đúng số lượng.
        /// Không cần gọi ở các action POST redirect (vì redirect sẽ gọi GET action mới).
        /// </summary>
        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        /// <summary>
        /// GET: /Moderation/Dashboard
        /// Trang tổng quan: hiển thị số pending comics, số pending reports,
        /// và bảng 5 truyện chờ duyệt gần nhất.
        /// Model: List&lt;ComicModeration&gt; (danh sách pending).
        /// </summary>
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await SetSidebarBadgesAsync();
            var pendingComics = await _moderationService.GetPendingComicsAsync();
            return View(pendingComics);
        }

        /// <summary>
        /// GET: /Moderation/Pending
        /// Danh sách đầy đủ tất cả truyện đang chờ duyệt.
        /// Hiển thị: Title, Author, Description (truncated), Created Date, badge Pending.
        /// Model: List&lt;ComicModeration&gt;.
        /// </summary>
        [HttpGet("Pending")]
        public async Task<IActionResult> Pending()
        {
            await SetSidebarBadgesAsync();
            var pendingModerations = await _moderationService.GetPendingComicsAsync();
            return View(pendingModerations);
        }

        /// <summary>
        /// GET: /Moderation/Review/{id}
        /// Trang chi tiết review một truyện cụ thể.
        /// Hiển thị: ảnh bìa, thông tin tác giả, mô tả, danh sách chapter.
        /// Cung cấp 3 nút hành động: Approve, Reject (modal), Hide (modal).
        /// </summary>
        /// <param name="id">ComicModerationId cần review.</param>
        [HttpGet("Review/{id}")]
        public async Task<IActionResult> Review(int id)
        {
            var moderation = await _moderationService.GetModerationByIdAsync(id);
            if (moderation == null)
                return NotFound();

            await SetSidebarBadgesAsync();

            return View(moderation);
        }

        /// <summary>
        /// GET: /Moderation/ReadChapter/{chapterId}?moderationId={moderationId}
        /// Cho phép moderator xem nội dung ảnh bên trong 1 chapter để kiểm duyệt.
        /// Đọc ảnh từ thư mục vật lý dựa trên Chapter.Path.
        /// ViewBag.Images: danh sách đường dẫn ảnh.
        /// ViewBag.ModerationId: để nút "Back" quay về đúng trang Review.
        /// </summary>
        /// <param name="chapterId">ChapterId cần xem nội dung.</param>
        /// <param name="moderationId">ComicModerationId để quay lại trang Review.</param>
        [HttpGet("ReadChapter/{chapterId}")]
        public async Task<IActionResult> ReadChapter(int chapterId, int moderationId)
        {
            var chapter = await _moderationService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
                return NotFound();

            // Đọc danh sách ảnh từ folder vật lý
            var relativePath = chapter.Path.TrimStart('/');
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

            var images = new List<string>();
            if (Directory.Exists(folderPath))
            {
                images = Directory.GetFiles(folderPath)
                    .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg") || f.EndsWith(".webp"))
                    .Select(Path.GetFileName)
                    .OrderBy(f => f)
                    .Select(f =>
                    {
                        var cleanPath = chapter.Path.EndsWith("/") ? chapter.Path : chapter.Path + "/";
                        return $"{cleanPath}{f}";
                    })
                    .ToList();
            }

            await SetSidebarBadgesAsync();
            ViewBag.Images = images;
            ViewBag.ModerationId = moderationId;

            return View(chapter);
        }

        /// <summary>
        /// POST: /Moderation/Approve
        /// Phê duyệt truyện — không cần lý do.
        /// </summary>
        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var moderatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (moderatorId == 0)
                return Unauthorized();

            var result = await _moderationService.ApproveComicAsync(id, moderatorId);
            if (!result)
                return NotFound();

            TempData["Success"] = "Comic approved successfully!";
            return RedirectToAction("Pending");
        }

        /// <summary>
        /// POST: /Moderation/Reject
        /// Từ chối truyện — bắt buộc nhập lý do.
        /// </summary>
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var moderatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (moderatorId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError(string.Empty, "Reason is required.");
                var moderation = await _moderationService.GetModerationByIdAsync(id);
                if (moderation == null) return NotFound();
                await SetSidebarBadgesAsync();
                return View("Review", moderation);
            }

            var result = await _moderationService.RejectComicAsync(id, moderatorId, reason);
            if (!result)
                return NotFound();

            TempData["Success"] = "Comic rejected successfully!";
            return RedirectToAction("Pending");
        }

        /// <summary>
        /// POST: /Moderation/Hide
        /// Ẩn truyện vi phạm — bắt buộc nhập lý do.
        /// </summary>
        [HttpPost("Hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id, string reason)
        {
            var moderatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (moderatorId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError(string.Empty, "Reason is required.");
                var moderation = await _moderationService.GetModerationByIdAsync(id);
                if (moderation == null) return NotFound();
                await SetSidebarBadgesAsync();
                return View("Review", moderation);
            }

            var result = await _moderationService.HideComicAsync(id, moderatorId, reason);
            if (!result)
                return NotFound();

            TempData["Success"] = "Comic hidden successfully!";
            return RedirectToAction("Pending");
        }
    }
}