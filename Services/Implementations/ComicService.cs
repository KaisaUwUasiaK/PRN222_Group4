using Group4_ReadingComicWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Services
{
    public class ComicService : IComicService
    {
        private readonly AppDbContext _context;

        public ComicService(AppDbContext context)
        {
            _context = context;
        }

        // Get public comic list
        public async Task<List<Comic>> GetPublicComicsAsync()
        {
            return await _context.Comics
                .Include(c => c.Author)
                .Where(c => c.Status == "Published" || c.Status == "Approved") // Tuỳ logic status của bạn
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        //Get comic detail (With Tags, Chapters, Comments and User's Comment)
        public async Task<Comic?> GetComicDetailAsync(int comicId)
        {
            var comic = await _context.Comics
                .Include(c => c.Author)
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Comments)
                        .ThenInclude(cmt => cmt.User)
                .FirstOrDefaultAsync(c => c.ComicId == comicId);

            return comic;
        }

        //Get Chapter and get prev/after chapter
        public async Task<(Chapter? CurrentChapter, int? PrevChapterId, int? NextChapterId)> GetChapterForReadingAsync(int chapterId)
        {
            var currentChapter = await _context.Chapters
                .Include(ch => ch.Comic)
                    .ThenInclude(c => c.Author)
                .Include(ch => ch.Comments)
                    .ThenInclude(cmt => cmt.User)
                .FirstOrDefaultAsync(ch => ch.ChapterId == chapterId);

            if (currentChapter == null)
            {
                return (null, null, null);
            }

            // Find previous chapter with comicId
            var prevChapterId = await _context.Chapters
                .Where(c => c.ComicId == currentChapter.ComicId && c.ChapterNumber < currentChapter.ChapterNumber)
                .OrderByDescending(c => c.ChapterNumber)
                .Select(c => c.ChapterId)
                .FirstOrDefaultAsync();

            //Find next chapter
            var nextChapterId = await _context.Chapters
                .Where(c => c.ComicId == currentChapter.ComicId && c.ChapterNumber > currentChapter.ChapterNumber)
                .OrderBy(c => c.ChapterNumber)
                .Select(c => c.ChapterId)
                .FirstOrDefaultAsync();

            return (currentChapter,
                    prevChapterId == 0 ? (int?)null : prevChapterId,
                    nextChapterId == 0 ? (int?)null : nextChapterId);
        }

        public async Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsPagedAsync(int page, int pageSize, string? searchTerm = null)
        {
            var query = _context.Comics
                .Where(c => c.Status == "OnWorking" || c.Status == "Completed");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(c => c.Title.Contains(searchTerm));
            }

            int totalCount = await query.CountAsync();

            var comics = await query
                .Include(c => c.Author)
                .Include(c => c.Chapters)
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (comics, totalCount);
        }

        public async Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsAdvancedAsync(
            int page, int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            string? status = null,
            List<int>? tagIds = null)
        {
            var query = _context.Comics
                .Where(c => c.Status == "OnWorking" || c.Status == "Completed")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => c.Title.Contains(searchTerm.Trim()));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (tagIds != null && tagIds.Count > 0)
                query = query.Where(c => c.ComicTags.Any(ct => tagIds.Contains(ct.TagId)));

            int totalCount = await query.CountAsync();

            IQueryable<Comic> orderedQuery = sortBy switch
            {
                "views" => query.OrderByDescending(c => c.ViewCount),
                "oldest" => query.OrderBy(c => c.CreatedAt),
                "title" => query.OrderBy(c => c.Title),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            var comics = await orderedQuery
                .Include(c => c.Author)
                .Include(c => c.Chapters)
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (comics, totalCount);
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags.OrderBy(t => t.TagName).ToListAsync();
        }

        public async Task<Comic?> GetRandomComicAsync(string? status = null, List<int>? tagIds = null)
        {
            var query = _context.Comics
                .Where(c => c.Status == "OnWorking" || c.Status == "Completed")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (tagIds != null && tagIds.Count > 0)
                query = query.Where(c => c.ComicTags.Any(ct => tagIds.Contains(ct.TagId)));

            var count = await query.CountAsync();
            if (count == 0) return null;

            var skip = new Random().Next(0, count);
            return await query.Skip(skip).FirstOrDefaultAsync();
        }
        //increase view count
        async Task IComicService.IncrementViewCountAsync(int comicId)
        {
           
            var comic = await _context.Comics.FindAsync(comicId);

            if (comic != null)
            {
                comic.ViewCount += 1;

                _context.Comics.Update(comic);
                await _context.SaveChangesAsync();
            }
        }
    }

}