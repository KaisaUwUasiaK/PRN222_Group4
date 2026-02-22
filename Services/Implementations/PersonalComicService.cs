using Group4_ReadingComicWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Services
{
    public class PersonalComicService : IPersonalComicService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PersonalComicService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<Comic>> GetUserComicsAsync(int userId)
        {
            return await _context.Comics
                .Include(c => c.Chapters)
                .Where(c => c.AuthorId == userId)
                .Where(c => c.Status != "Canceled" && c.Status != "Deleted")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags.ToListAsync();
        }

        public async Task CreateComicAsync(int userId, Comic comic, int[] selectedTags, IFormFile coverImage)
        {
            comic.AuthorId = userId;
            comic.CreatedAt = DateTime.Now;
            comic.Status = "Pending";

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

            if (selectedTags != null)
            {
                foreach (var tagId in selectedTags)
                {
                    _context.ComicTags.Add(new ComicTag { ComicId = comic.ComicId, TagId = tagId });
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Comic> GetComicWithChaptersAsync(int comicId, int userId)
        {
            return await _context.Comics
                .Include(c => c.Chapters.OrderBy(ch => ch.ChapterNumber))
                .FirstOrDefaultAsync(c => c.ComicId == comicId && c.AuthorId == userId);
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> CreateChapterAsync(int userId, int comicId, int chapterNumber, string title, List<IFormFile> pages)
        {
            var comic = await _context.Comics.FirstOrDefaultAsync(c => c.ComicId == comicId && c.AuthorId == userId);
            if (comic == null) return (false, "Forbidden");

            bool isChapterExists = await _context.Chapters
                .AnyAsync(ch => ch.ComicId == comicId && ch.ChapterNumber == chapterNumber);

            if (isChapterExists) return (false, "Chương số này đã tồn tại trong bộ truyện.");

            string wwwRootPath = _environment.WebRootPath;
            string folderName = $"comic-{comicId}/chap-{chapterNumber}";
            string folderPath = Path.Combine(wwwRootPath, "uploads", folderName);

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

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
            return (true, string.Empty);
        }

        public async Task<Comic> GetComicForEditAsync(int comicId, int userId)
        {
            return await _context.Comics
                .Include(c => c.ComicTags)
                .FirstOrDefaultAsync(c => c.ComicId == comicId && c.AuthorId == userId);
        }

        public async Task<bool> EditComicAsync(int userId, int id, Comic comic, int[] selectedTags, IFormFile coverImage)
        {
            var existingComic = await _context.Comics
                .Include(c => c.ComicTags)
                .FirstOrDefaultAsync(c => c.ComicId == id && c.AuthorId == userId);

            if (existingComic == null) return false;

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
            return true;
        }

        public async Task<bool> DeleteComicAsync(int userId, int id)
        {
            var comic = await _context.Comics
                .Include(c => c.Chapters)
                .Include(c => c.ComicTags)
                .FirstOrDefaultAsync(c => c.ComicId == id && c.AuthorId == userId);

            if (comic == null) return false;

            if (comic.Status == "Pending")
            {
                comic.Status = "Canceled";
            }
            else
            {
                if (!string.IsNullOrEmpty(comic.CoverImage))
                {
                    var coverPath = Path.Combine(_environment.WebRootPath, comic.CoverImage.TrimStart('/'));
                    if (File.Exists(coverPath)) File.Delete(coverPath);
                }

                var comicFolderPath = Path.Combine(_environment.WebRootPath, "uploads", $"comic-{id}");
                if (Directory.Exists(comicFolderPath))
                {
                    Directory.Delete(comicFolderPath, recursive: true);
                }

                _context.Comics.Remove(comic);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(Chapter Chapter, List<string> Images)> GetChapterForReadAsync(int chapterId, int userId)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Comic.AuthorId == userId);

            if (chapter == null) return (null, null);

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

            return (chapter, imageList);
        }

        public async Task<Chapter> GetChapterAsync(int chapterId, int userId)
        {
            return await _context.Chapters
                .Include(c => c.Comic)
                .FirstOrDefaultAsync(c => c.ChapterId == chapterId && c.Comic.AuthorId == userId);
        }

        public async Task<bool> EditChapterAsync(int userId, int id, Chapter model, List<IFormFile> newPages)
        {
            var chapter = await GetChapterAsync(id, userId);
            if (chapter == null) return false;

            chapter.ChapterNumber = model.ChapterNumber;
            chapter.Title = model.Title;

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
            return true;
        }

        public async Task<int?> DeleteChapterAsync(int userId, int id)
        {
            var chapter = await GetChapterAsync(id, userId);
            if (chapter == null) return null;

            int comicId = chapter.ComicId;

            if (!string.IsNullOrEmpty(chapter.Path))
            {
                string physicalPath = Path.Combine(_environment.WebRootPath, chapter.Path.TrimStart('/'));
                if (Directory.Exists(physicalPath))
                {
                    Directory.Delete(physicalPath, recursive: true);
                }
            }

            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();

            return comicId;
        }
    }
}