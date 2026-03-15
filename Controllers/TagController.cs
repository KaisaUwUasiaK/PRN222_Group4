using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group4_ReadingComicWeb.Controllers
{
    /// <summary>
    /// Controller của Mod để quản lý Tag (thể loại) cho comics.
    /// </summary>
    [Authorize(Roles = "Moderator")]
    [Route("Tags")]
    public class TagController : Controller
    {
        private readonly ITagService _tagService;
        private readonly IModerationService _moderationService;
        private readonly IReportService _reportService;

        /// <summary>
        /// Khởi tạo một thể hiện của TagController.
        /// </summary>
        /// <param name="tagService"></param>
        /// <param name="moderationService"></param>
        /// <param name="reportService"></param>
        public TagController(
            ITagService tagService,
            IModerationService moderationService,
            IReportService reportService)
        {
            _tagService = tagService;
            _moderationService = moderationService;
            _reportService = reportService;
        }

        /// <summary>
        /// Thiết lập các badge cho sidebar.
        /// </summary>
        /// <returns></returns>
        private async Task SetSidebarBadgesAsync()
        {
            ViewBag.PendingComicsCount = await _moderationService.GetPendingCountAsync();
            ViewBag.PendingUserReportsCount = await _reportService.GetPendingUserReportsCountAsync();
        }

        /// <summary>
        /// Hiển thị danh sách các tag.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            await SetSidebarBadgesAsync();
            var tags = await _tagService.GetAllWithUsageAsync();
            return View(tags);
        }

        /// <summary>
        /// Hiển thị trang tạo tag mới.
        /// </summary>
        /// <returns></returns>
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await SetSidebarBadgesAsync();
            return View(new Tag());
        }

        /// <summary>
        /// Xử lý việc tạo tag mới.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tag model)
        {
            model.TagName = model.TagName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.TagName))
            {
                ModelState.AddModelError(nameof(model.TagName), "Tag name is required.");
            }

            if (await _tagService.IsTagNameExistsAsync(model.TagName))
            {
                ModelState.AddModelError(nameof(model.TagName), "Tag name already exists.");
            }

            if (!ModelState.IsValid)
            {
                await SetSidebarBadgesAsync();
                return View(model);
            }

            await _tagService.CreateAsync(model);
            TempData["Success"] = "Tag created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Hiển thị trang chỉnh sửa tag.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var tag = await _tagService.GetByIdAsync(id);
            if (tag == null)
            {
                return NotFound();
            }

            await SetSidebarBadgesAsync();
            return View(tag);
        }

        /// <summary>
        /// Xử lý việc chỉnh sửa tag.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tag model)
        {
            if (id != model.TagId)
            {
                return NotFound();
            }

            model.TagName = model.TagName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.TagName))
            {
                ModelState.AddModelError(nameof(model.TagName), "Tag name is required.");
            }

            if (await _tagService.IsTagNameExistsAsync(model.TagName, model.TagId))
            {
                ModelState.AddModelError(nameof(model.TagName), "Tag name already exists.");
            }

            if (!ModelState.IsValid)
            {
                await SetSidebarBadgesAsync();
                return View(model);
            }

            var updated = await _tagService.UpdateAsync(model);
            if (!updated)
            {
                return NotFound();
            }

            TempData["Success"] = "Tag updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xử lý việc xóa tag.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await _tagService.GetByIdAsync(id);
            if (tag == null)
            {
                TempData["Error"] = "Tag not found.";
                return RedirectToAction(nameof(Index));
            }

            var usageCount = await _tagService.GetComicUsageCountAsync(id);
            if (usageCount > 0)
            {
                TempData["Error"] = $"Cannot delete tag '{tag.TagName}' because it is used by {usageCount} comic(s).";
                return RedirectToAction(nameof(Index));
            }

            await _tagService.DeleteAsync(id);
            TempData["Success"] = $"Tag '{tag.TagName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}