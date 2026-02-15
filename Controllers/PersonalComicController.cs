using Group4_ReadingComicWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize]
    public class PersonalComicController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PersonalComicController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        private int GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return 0;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var comics = await _context.Comics
                .Include(c => c.Chapters) 
                .Where(c => c.AuthorId == userId)
                .Where(c => c.Status != "Canceled" && c.Status != "Deleted")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(comics);
        }

        public IActionResult Create()
        {
            ViewBag.Tags = _context.Tags.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comic comic, int[] selectedTags, IFormFile coverImage)
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

            // 3. Kiểm tra lại ModelState (Lúc này nếu thiếu ảnh, IsValid sẽ là false)
            if (ModelState.IsValid)
            {
                comic.AuthorId = GetCurrentUserId();
                comic.CreatedAt = DateTime.Now;
                comic.Status = "Pending";

                // Logic lưu ảnh (Chắc chắn chạy được vì đã check null ở trên)
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "covers");

                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(stream);
                }
                comic.CoverImage = "/uploads/covers/" + fileName;

                _context.Comics.Add(comic);
                await _context.SaveChangesAsync();

                // Lưu Tags...
                if (selectedTags != null)
                {
                    foreach (var tagId in selectedTags)
                    {
                        _context.ComicTags.Add(new ComicTag { ComicId = comic.ComicId, TagId = tagId });
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tags = _context.Tags.ToList();
            return View(comic);
        }

        public async Task<IActionResult> Chapters(int id)
        {
            var userId = GetCurrentUserId();
            var comic = await _context.Comics
                .Include(c => c.Chapters.OrderBy(ch => ch.ChapterNumber))
                .FirstOrDefaultAsync(c => c.ComicId == id && c.AuthorId == userId);

            if (comic == null) return NotFound();

            return View(comic);
        }

        [HttpPost]
        public async Task<IActionResult> CreateChapter(int comicId, int chapterNumber, string title, List<IFormFile> pages)
        {
            var userId = GetCurrentUserId();
            var comic = await _context.Comics.FirstOrDefaultAsync(c => c.ComicId == comicId && c.AuthorId == userId);

            if (comic == null) return Forbid();

            string wwwRootPath = _environment.WebRootPath;
            string folderName = $"comic-{comicId}/chap-{chapterNumber}";
            string folderPath = Path.Combine(wwwRootPath, "uploads", folderName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (pages != null && pages.Count > 0)
            {
                var sortedPages = pages.OrderBy(f => f.FileName).ToList();

                for (int i = 0; i < sortedPages.Count; i++)
                {
                    var file = sortedPages[i];

                    string extension = Path.GetExtension(file.FileName);
                    string newFileName = $"page-{(i + 1):000}{extension}";

                    string filePath = Path.Combine(folderPath, newFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            var chapter = new Chapter
            {
                ComicId = comicId,
                ChapterNumber = chapterNumber,
                Title = title,
                Path = $"/uploads/{folderName}",
                CreatedAt = DateTime.Now
            };

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chapters), new { id = comicId });
        }
       
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();

            var comic = await _context.Comics
                .Include(c => c.ComicTags)
                .FirstOrDefaultAsync(c => c.ComicId == id && c.AuthorId == userId);

            if (comic == null) return NotFound();

            ViewBag.Tags = _context.Tags.ToList();

            ViewBag.SelectedTagIds = comic.ComicTags.Select(ct => ct.TagId).ToList();

            return View(comic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Comic comic, int[] selectedTags, IFormFile coverImage)
        {
            ModelState.Remove("AuthorId");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("CoverImage"); 
            ModelState.Remove("Author");
            ModelState.Remove("Chapters"); 
            ModelState.Remove("ComicTags"); 
            if (id != comic.ComicId) return NotFound();

            var userId = GetCurrentUserId();

            var existingComic = await _context.Comics
                .Include(c => c.ComicTags)
                .FirstOrDefaultAsync(c => c.ComicId == id && c.AuthorId == userId);

            if (existingComic == null) return Forbid();

            if (ModelState.IsValid)
            {
                existingComic.Title = comic.Title;
                existingComic.Description = comic.Description;
                existingComic.Status = "Pending";

                if (coverImage != null)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
                    var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "covers");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await coverImage.CopyToAsync(stream);
                    }

                    

                    existingComic.CoverImage = "/uploads/covers/" + fileName;
                }

                var oldTags = _context.ComicTags.Where(ct => ct.ComicId == id);
                _context.ComicTags.RemoveRange(oldTags);

                if (selectedTags != null)
                {
                    foreach (var tagId in selectedTags)
                    {
                        _context.ComicTags.Add(new ComicTag { ComicId = id, TagId = tagId });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tags = _context.Tags.ToList();
            ViewBag.SelectedTagIds = selectedTags != null ? selectedTags.ToList() : new List<int>();
            return View(comic);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();

            var comic = await _context.Comics
                .Include(c => c.Chapters) 
                .Include(c => c.ComicTags) 
                .FirstOrDefaultAsync(c => c.ComicId == id && c.AuthorId == userId);

            if (comic == null)
            {
                return NotFound();
            }

            if (comic.Status == "Pending")
            {
                
                comic.Status = "Canceled";
            }
            else
            {
                
                if (!string.IsNullOrEmpty(comic.CoverImage))
                {
                    var coverPath = Path.Combine(_environment.WebRootPath, comic.CoverImage.TrimStart('/'));
                    if (System.IO.File.Exists(coverPath))
                    {
                        System.IO.File.Delete(coverPath);
                    }
                }

              
                var comicFolderPath = Path.Combine(_environment.WebRootPath, "uploads", $"comic-{id}");
                if (Directory.Exists(comicFolderPath))
                {
                    Directory.Delete(comicFolderPath, true); // true = xóa đệ quy cả file bên trong
                }

                _context.Comics.Remove(comic);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        //Chapter
        public async Task<IActionResult> Read(int id)
        {
            var userId = GetCurrentUserId();

            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(c => c.ChapterId == id && c.Comic.AuthorId == userId);

            if (chapter == null) return NotFound();

            var imageList = new List<string>();
            if (!string.IsNullOrEmpty(chapter.Path))
            {
                string physicalPath = Path.Combine(_environment.WebRootPath, chapter.Path.TrimStart('/'));

                if (Directory.Exists(physicalPath))
                {
                    var files = Directory.GetFiles(physicalPath)
                        .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg") || f.EndsWith(".webp"))
                        .OrderBy(f => f)
                        .Select(f => Path.GetFileName(f));

                    foreach (var fileName in files)
                    {
                        imageList.Add($"{chapter.Path}/{fileName}");
                    }
                }
            }

            ViewBag.Images = imageList;
            return View(chapter);
        }

        public async Task<IActionResult> EditChapter(int id)
        {
            var userId = GetCurrentUserId();
            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(c => c.ChapterId == id && c.Comic.AuthorId == userId);

            if (chapter == null) return NotFound();

            return View(chapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChapter(int id, Chapter model, List<IFormFile> newPages)
        {
            var userId = GetCurrentUserId();
            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(c => c.ChapterId == id && c.Comic.AuthorId == userId);

            if (chapter == null) return NotFound();

            chapter.ChapterNumber = model.ChapterNumber;
            chapter.Title = model.Title;
            // chapter.UpdatedAt = DateTime.Now;

            if (newPages != null && newPages.Count > 0)
            {
                string physicalPath = Path.Combine(_environment.WebRootPath, chapter.Path.TrimStart('/'));

                if (Directory.Exists(physicalPath))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(physicalPath);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                }
                else
                {
                    Directory.CreateDirectory(physicalPath);
                }

                var sortedPages = newPages.OrderBy(f => f.FileName).ToList();
                for (int i = 0; i < sortedPages.Count; i++)
                {
                    var file = sortedPages[i];
                    string extension = Path.GetExtension(file.FileName);
                    string newFileName = $"page-{(i + 1):000}{extension}";
                    string filePath = Path.Combine(physicalPath, newFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Chapters", new { id = chapter.ComicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var userId = GetCurrentUserId();
            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(c => c.ChapterId == id && c.Comic.AuthorId == userId);

            if (chapter == null) return NotFound();

            int comicId = chapter.ComicId; // Lưu lại ID để redirect

            // 1. Xóa Folder ảnh trên server
            if (!string.IsNullOrEmpty(chapter.Path))
            {
                string physicalPath = Path.Combine(_environment.WebRootPath, chapter.Path.TrimStart('/'));
                if (Directory.Exists(physicalPath))
                {
                    Directory.Delete(physicalPath, true); // true = Xóa cả file bên trong
                }
            }

            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();

            return RedirectToAction("Chapters", new { id = comicId });
        }
    }

}
