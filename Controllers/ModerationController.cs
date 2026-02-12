using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize(Roles = "Moderator")]
    public class ModerationController : Controller
    {
        private readonly IModerationService _moderationService;

        public ModerationController(IModerationService moderationService)
        {
            _moderationService = moderationService;
        }

        // GET: Moderation/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var pendingCount = await _moderationService.GetPendingCountAsync();
            var approvedCount = await _moderationService.GetApprovedCountThisMonthAsync();
            var rejectedCount = await _moderationService.GetRejectedCountThisMonthAsync();
            var hiddenCount = await _moderationService.GetHiddenCountAsync();
            var pendingModerations = await _moderationService.GetPendingComicsAsync();

            ViewBag.PendingCount = pendingCount;
            ViewBag.ApprovedCount = approvedCount;
            ViewBag.RejectedCount = rejectedCount;
            ViewBag.HiddenCount = hiddenCount;

            return View(pendingModerations);
        }

        // GET: Moderation/Pending
        public async Task<IActionResult> Pending()
        {
            var pendingModerations = await _moderationService.GetPendingComicsAsync();
            return View(pendingModerations);
        }

        // GET: Moderation/Review/5
        public async Task<IActionResult> Review(int id)
        {
            var moderation = await _moderationService.GetModerationByIdAsync(id);
            if (moderation == null)
                return NotFound();

            var history = await _moderationService.GetModerationHistoryAsync(moderation.ComicId);
            ViewBag.ModerationHistory = history;

            return View(moderation);
        }

        // POST: Moderation/Approve/5
        [HttpPost]
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

        // POST: Moderation/Reject/5
        [HttpPost]
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
                var history = await _moderationService.GetModerationHistoryAsync(moderation.ComicId);
                ViewBag.ModerationHistory = history;
                return View("Review", moderation);
            }

            var result = await _moderationService.RejectComicAsync(id, moderatorId, reason);
            if (!result)
                return NotFound();

            TempData["Success"] = "Comic rejected successfully!";
            return RedirectToAction("Pending");
        }

        // POST: Moderation/Hide/5
        [HttpPost]
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
                var history = await _moderationService.GetModerationHistoryAsync(moderation.ComicId);
                ViewBag.ModerationHistory = history;
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