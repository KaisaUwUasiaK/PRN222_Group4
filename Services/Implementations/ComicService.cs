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

        private IQueryable<Comic> ApplyFilters(IQueryable<Comic> query, string? searchTerm, List<string>? tags, string? status, string? sortBy)
        {
            // Always show public comics only
            query = query.Where(c => c.Status == "OnWorking" || c.Status == "Completed");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                // Map frontend status to backend status
                if (status == "Ongoing") query = query.Where(c => c.Status == "OnWorking");
                else if (status == "Completed") query = query.Where(c => c.Status == "Completed");
            }

            if (tags != null && tags.Any())
            {
                // Matching ALL selected tags (Logical AND)
                foreach (var tag in tags)
                {
                    query = query.Where(c => c.ComicTags.Any(ct => ct.Tag.TagName == tag));
                }
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy)
                {
                    case "Newest":
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    case "Latest":
                        query = query.OrderBy(c => c.CreatedAt);
                        break;
                    default:
                        query = query.OrderByDescending(c => c.ViewCount);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            return query;
        }

        public async Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsPagedAsync(
            int page, int pageSize, string? searchTerm = null, List<string>? tags = null, string? status = null, string? sortBy = null)
        {
            var query = _context.Comics.AsQueryable();
            query = ApplyFilters(query, searchTerm, tags, status, sortBy);

            int totalCount = await query.CountAsync();

            var comics = await query
                .Include(c => c.Author)
                .Include(c => c.Chapters)
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (comics, totalCount);
        }

        public async Task<Comic?> GetRandomComicAsync(string? searchTerm = null, List<string>? tags = null, string? status = null)
        {
            var query = _context.Comics.AsNoTracking();
            query = ApplyFilters(query, searchTerm, tags, status, null);

            var totalCount = await query.CountAsync();
            if (totalCount == 0) return null;

            int offset = new Random().Next(0, totalCount);
            return await query.Skip(offset).FirstOrDefaultAsync();
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