using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize(Roles = "Moderator")]
    [Route("Moderation")]
    public class ModerationController : Controller
    {
        private readonly IModerationService _moderationService;
        private readonly IReportService _reportService;
        private readonly ITagService _tagService;
        private readonly INotificationService _notifService;

        public ModerationController(
            IModerationService moderationService,
            IReportService reportService,
            ITagService tagService,
            INotificationService notifService)
        {
            _moderationService = moderationService;
            _reportService = reportService;
            _tagService = tagService;
            _notifService = notifService;
        }

        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await SetSidebarBadgesAsync();
            ViewBag.TotalTagsCount = await _tagService.GetTotalTagsCountAsync();

            var pendingComics = await _moderationService.GetPendingComicsAsync();
            return View(pendingComics);
        }

        [HttpGet("Pending")]
        public async Task<IActionResult> Pending()
        {
            await SetSidebarBadgesAsync();
            var pendingModerations = await _moderationService.GetPendingComicsAsync();
            return View(pendingModerations);
        }

        [HttpGet("Review/{id}")]
        public async Task<IActionResult> Review(int id)
        {
            var moderation = await _moderationService.GetModerationByIdAsync(id);
            if (moderation == null)
                return NotFound();

            await SetSidebarBadgesAsync();
            return View(moderation);
        }

        [HttpGet("ReadChapter/{chapterId}")]
        public async Task<IActionResult> ReadChapter(int chapterId, int moderationId)
        {
            var chapter = await _moderationService.GetChapterByIdAsync(chapterId);
            if (chapter == null)
                return NotFound();

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

        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var moderatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (moderatorId == 0) return Unauthorized();

            // Load trước để lấy thông tin comic gửi notification
            var moderation = await _moderationService.GetModerationByIdAsync(id);
            if (moderation == null) return NotFound();

            var result = await _moderationService.ApproveComicAsync(id, moderatorId);
            if (!result) return NotFound();

            // Gửi thông báo cho tác giả
            await _notifService.ComicApprovedAsync(
                moderation.Comic.AuthorId,
                moderation.ComicId,
                moderation.Comic.Title);

            TempData["Success"] = "Comic approved successfully!";
            return RedirectToAction("Pending");
        }

        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var moderatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (moderatorId == 0) return Unauthorized();

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError(string.Empty, "Reason is required.");
                var mod = await _moderationService.GetModerationByIdAsync(id);
                if (mod == null) return NotFound();
                await SetSidebarBadgesAsync();
                return View("Review", mod);
            }

            // Load trước để lấy thông tin comic gửi notification
            var moderation = await _moderationService.GetModerationByIdAsync(id);
            if (moderation == null) return NotFound();

            var result = await _moderationService.RejectComicAsync(id, moderatorId, reason);
            if (!result) return NotFound();

            // Gửi thông báo cho tác giả kèm lý do từ chối
            await _notifService.ComicRejectedAsync(
                moderation.Comic.AuthorId,
                moderation.ComicId,
                moderation.Comic.Title,
                reason);

            TempData["Success"] = "Comic rejected successfully!";
            return RedirectToAction("Pending");
        }

        [HttpPost("Hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id, string reason)
        {
            var moderatorId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (moderatorId == 0) return Unauthorized();

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError(string.Empty, "Reason is required.");
                var mod = await _moderationService.GetModerationByIdAsync(id);
                if (mod == null) return NotFound();
                await SetSidebarBadgesAsync();
                return View("Review", mod);
            }

            // Load trước để lấy thông tin comic gửi notification
            var moderation = await _moderationService.GetModerationByIdAsync(id);
            if (moderation == null) return NotFound();

            var result = await _moderationService.HideComicAsync(id, moderatorId, reason);
            if (!result) return NotFound();

            // Gửi thông báo cho tác giả kèm lý do ẩn
            await _notifService.ComicHiddenAsync(
                moderation.Comic.AuthorId,
                moderation.ComicId,
                moderation.Comic.Title,
                reason);

            TempData["Success"] = "Comic hidden successfully!";
            return RedirectToAction("Pending");
        }
    }
}