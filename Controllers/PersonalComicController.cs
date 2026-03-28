using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize(Roles = "User")]
    public class PersonalComicController : Controller
    {
        private readonly IPersonalComicService _comicService;
        private readonly IFavoriteService _favoriteService;
        private readonly INotificationService _notifService;
        private readonly AppDbContext _db;
        private readonly LogService _logService;

        public PersonalComicController(
            IPersonalComicService comicService,
            IFavoriteService favoriteService,
            INotificationService notifService,
            AppDbContext db,
            LogService logService)
        {
            _comicService = comicService;
            _favoriteService = favoriteService;
            _notifService = notifService;
            _db = db;
            _logService = logService;
        }

        // Get current user authorize to system
        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return 0;
        }

        // Get comic uploaded by user
        public async Task<IActionResult> Index()
        {
            var comics = await _comicService.GetUserComicsAsync(GetCurrentUserId());
            return View(comics);
        }

        // Get tag list
        public async Task<IActionResult> Create()
        {
            ViewBag.Tags = await _comicService.GetAllTagsAsync();
            return View();
        }

        // Upload comic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comic comic, int[] selectedTags, IFormFile? coverImage)
        {
            ModelState.Remove("CoverImage");
            ModelState.Remove("AuthorId");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Chapters");
            ModelState.Remove("ComicTags");
            ModelState.Remove("Author");

            if (coverImage == null || coverImage.Length == 0)
            {
                ModelState.AddModelError("coverImage", "Please choose cover image!");
            }
            else 
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = System.IO.Path.GetExtension(coverImage.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("coverImage", "Invalid format, accepted only  JPG, PNG, WEBP file.");
                }
            }
            if (selectedTags == null || selectedTags.Length == 0)
            {
                ModelState.AddModelError("selectedTags", "Please choose at least 1 tag");
            }

            if (ModelState.IsValid)
            {
                await _comicService.CreateComicAsync(GetCurrentUserId(), comic, selectedTags ?? new int[0], coverImage);

                // Gửi thông báo cho TẤT CẢ Moderator khi có truyện mới chờ duyệt
                var authorName = User.Identity?.Name ?? "Unknown";
                var moderatorIds = await _db.Users
                    .Where(u => u.Role.RoleName == "Moderator")
                    .Select(u => u.UserId)
                    .ToListAsync();

                foreach (var modId in moderatorIds)
                    await _notifService.NewComicPendingAsync(modId, comic.ComicId, comic.Title, authorName);
                await _logService.AddLogAsync(GetCurrentUserId(), $"Uploaded new comic: {comic.Title}");

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tags = await _comicService.GetAllTagsAsync();
            return View(comic);
        }

        // Get comic to update
        public async Task<IActionResult> Edit(int id)
        {
            var comic = await _comicService.GetComicForEditAsync(id, GetCurrentUserId());
            if (comic == null) return NotFound();

            ViewBag.Tags = await _comicService.GetAllTagsAsync();
            ViewBag.SelectedTagIds = comic.ComicTags.Select(ct => ct.TagId).ToList();
            return View(comic);
        }

        // Update comic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Comic comic, int[] selectedTags, IFormFile? coverImage)
        {
            ModelState.Remove("AuthorId");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("CoverImage");
            ModelState.Remove("Author");
            ModelState.Remove("Chapters");
            ModelState.Remove("ComicTags");

            if (id != comic.ComicId) return NotFound();

            if (ModelState.IsValid)
            {
                bool success = await _comicService.EditComicAsync(GetCurrentUserId(), id, comic, selectedTags, coverImage);
                if (!success) return Forbid();

                // Gửi thông báo cho TẤT CẢ Moderator khi có truyện được cập nhật chờ duyệt
                var authorName = User.Identity?.Name ?? "Unknown";
                var moderatorIds = await _db.Users
                    .Where(u => u.Role.RoleName == "Moderator")
                    .Select(u => u.UserId)
                    .ToListAsync();

                foreach (var modId in moderatorIds)
                {
                    await _notifService.NewComicPendingAsync(modId, comic.ComicId, comic.Title, authorName);
                }

                await _logService.AddLogAsync(GetCurrentUserId(), $"Updated comic: {comic.Title}");

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tags = await _comicService.GetAllTagsAsync();
            ViewBag.SelectedTagIds = selectedTags != null ? selectedTags.ToList() : new List<int>();
            return View(comic);
        }

        // Remove comic
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Delete(int id)
        {
            // Lấy tên truyện trước khi xóa để ghi log cho rõ ràng
            var comic = await _db.Comics.FindAsync(id);
            string title = comic?.Title ?? "Unknown Comic";

            bool success = await _comicService.DeleteComicAsync(GetCurrentUserId(), id);
            if (success)
            {
                // 5. GHI LOG TẠI ĐÂY
                await _logService.AddLogAsync(GetCurrentUserId(), $"Deleted comic: {title}");
            }

            return RedirectToAction(nameof(Index));
        }

        // Get chapter list
        public async Task<IActionResult> Chapters(int id)
        {
            var comic = await _comicService.GetComicWithChaptersAsync(id, GetCurrentUserId());
            if (comic == null) return NotFound();
            return View(comic);
        }

        public async Task<IActionResult> Read(int id)
        {
            var result = await _comicService.GetChapterForReadAsync(id, GetCurrentUserId());
            if (result.Chapter == null) return NotFound();
            ViewBag.Images = result.Images;
            return View(result.Chapter);
        }

        // Upload chapter
        [HttpPost]
        public async Task<IActionResult> CreateChapter(int comicId, int chapterNumber, string title, List<IFormFile> pages)
        {
            if (chapterNumber < 0)
            {
                TempData["ErrorMessage"] = "Chapter number cannot be negative (Số chương không được là số âm).";
                return RedirectToAction("Chapters", new { id = comicId });
            }
            var result = await _comicService.CreateChapterAsync(GetCurrentUserId(), comicId, chapterNumber, title, pages);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Forbidden") return Forbid();
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Chapters", new { id = comicId });
            }

            // Gửi thông báo chương mới cho tất cả follower của truyện
            var comic = await _db.Comics.FindAsync(comicId);
            if (comic != null)
            {
                var followerIds = await _db.Favorites
                    .Where(f => f.ComicId == comicId)
                    .Select(f => f.UserId)
                    .ToListAsync();

                if (followerIds.Any())
                    await _notifService.NewChapterAsync(followerIds, comicId, comic.Title, chapterNumber);
            }

            return RedirectToAction(nameof(Chapters), new { id = comicId });
        }

        // Get chapter to update
        public async Task<IActionResult> EditChapter(int id)
        {
            var chapter = await _comicService.GetChapterAsync(id, GetCurrentUserId());
            if (chapter == null) return NotFound();
            return View(chapter);
        }

        // Update chapter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChapter(int id, Chapter model, List<IFormFile> newPages)
        {
            // 1. Check Id match
            if (id != model.ChapterId) return BadRequest();
            if (model.ChapterNumber < 0)
            {
                ModelState.AddModelError("ChapterNumber", "Chapter number cannot be negative.");
                return View(model);
            }
            // 2. Validate form
            if (!ModelState.IsValid)
                return View(model);

            // 3. Check file extensions
            if (newPages != null && newPages.Any())
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                foreach (var file in newPages)
                {
                    var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("", $"File '{file.FileName}' is invalid. Only JPG, PNG, WEBP are allowed.");
                        return View(model);
                    }
                }
            }

            // 4. Call service
            bool success = await _comicService.EditChapterAsync(GetCurrentUserId(), id, model, newPages ?? new List<IFormFile>());
            if (!success) return NotFound();

            return RedirectToAction("Chapters", new { id = model.ComicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var comicId = await _comicService.DeleteChapterAsync(GetCurrentUserId(), id);
            if (comicId == null) return NotFound();
            return RedirectToAction("Chapters", new { id = comicId });
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                var favComics = await _favoriteService.GetUserFavoritesAsync(userId);
                return View(favComics);
            }
            return RedirectToAction("Login", "Authentication");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFavorite(int comicId)
        {
            int userId = GetCurrentUserId();
            if (userId > 0)
            {
                await _favoriteService.ToggleFavoriteAsync(comicId, userId);
                TempData["Info"] = "Removed from your favorites.";
            }
            return RedirectToAction("Favorites");
        }
    }
}