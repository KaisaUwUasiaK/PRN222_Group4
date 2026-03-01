using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Controllers
{
    public class ComicController : Controller
    {
        private readonly IComicService _comicService;

        private readonly ICommentService _commentService;

        private readonly IMemoryCache _memoryCache;
        private readonly IFavoriteService _favoriteService;

        public ComicController(IComicService comicService, ICommentService commentService, IMemoryCache memoryCache, IFavoriteService favoriteService)
        {
            _comicService = comicService;
            _commentService = commentService;
            _memoryCache = memoryCache;
            _favoriteService = favoriteService;
        }


        // List all public comics
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 12; 

            var (comics, totalCount) = await _comicService.GetPublicComicsPagedAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.PageSize = pageSize;

            return View(comics);
        }


        // View comic detail
        public async Task<IActionResult> Detail(int id, int commentPage = 1)
        {
            var comic = await _comicService.GetComicDetailAsync(id);

            if (comic == null) return NotFound();

            bool isFavorited = false;
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                isFavorited = await _favoriteService.IsFavoritedAsync(id, userId);
            }
            ViewBag.IsFavorited = isFavorited;

            int commentPageSize = 10;
            var allComments = comic.Chapters
                .Where(c => c.Comments != null)
                .SelectMany(c => c.Comments)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            ViewBag.TotalComments = allComments.Count;
            ViewBag.TotalCommentPages = (int)Math.Ceiling((double)allComments.Count / commentPageSize);
            ViewBag.CurrentCommentPage = commentPage;

            ViewBag.PaginatedComments = allComments
                .Skip((commentPage - 1) * commentPageSize)
                .Take(commentPageSize)
                .ToList();

            return View(comic);
        }

        // Read chapter in comic
        public async Task<IActionResult> Read(int id, int commentPage = 1)
        {
            var result = await _comicService.GetChapterForReadingAsync(id);

            if (result.CurrentChapter == null) return NotFound();

            ViewBag.PrevChapterId = result.PrevChapterId;
            ViewBag.NextChapterId = result.NextChapterId;

            // Logic phân trang comment
            int commentPageSize = 10;
            var allComments = result.CurrentChapter.Comments != null
                ? result.CurrentChapter.Comments.OrderByDescending(c => c.CreatedAt).ToList()
                : new List<Comment>();

            ViewBag.TotalComments = allComments.Count;
            ViewBag.TotalCommentPages = (int)Math.Ceiling((double)allComments.Count / commentPageSize);
            ViewBag.CurrentCommentPage = commentPage;
            ViewBag.PaginatedComments = allComments
                .Skip((commentPage - 1) * commentPageSize)
                .Take(commentPageSize)
                .ToList();

            return View(result.CurrentChapter);
        }
        // Add comment 
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int ChapterId, string Content)
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["ErrorMessage"] = "Nội dung bình luận không được để trống!";
                return RedirectToAction("Read", new { id = ChapterId });
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                var cacheKey = $"CommentCooldown_{userId}";

                if (_memoryCache.TryGetValue(cacheKey, out _))
                {
                    TempData["ErrorMessage"] = "Bạn bình luận quá nhanh. Vui lòng đợi 15 giây trước khi gửi tiếp!";
                    return RedirectToAction("Read", new { id = ChapterId });
                }

                await _commentService.AddCommentAsync(ChapterId, userId, Content);

                _memoryCache.Set(cacheKey, true, TimeSpan.FromSeconds(15));

                TempData["SuccessMessage"] = "Đã gửi bình luận thành công!";
            }

            return RedirectToAction("Read", new { id = ChapterId });
        }
        //Delete comment
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId, int chapterId, string source, int? comicId)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                await _commentService.DeleteCommentAsync(commentId, userId);
            }

            if (source == "Detail" && comicId.HasValue)
            {
                return RedirectToAction("Detail", new { id = comicId.Value });
            }

            return RedirectToAction("Read", new { id = chapterId });
        }
       

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int comicId)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                bool isAdded = await _favoriteService.ToggleFavoriteAsync(comicId, userId);

                if (isAdded)
                    TempData["Success"] = "Added to your favorites!";
                else
                    TempData["Info"] = "Removed from favorites.";
            }

            return RedirectToAction("Detail", new { id = comicId });
        }
    }
}