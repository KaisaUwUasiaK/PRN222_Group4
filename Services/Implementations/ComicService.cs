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
                .Where(c => c.Status == "Published" || c.Status == "Approved") // Tuỳ logic status của bạn
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        //Get comic detail (With Tags, Chapters, Comments and User's Comment)
        public async Task<Comic> GetComicDetailAsync(int comicId)
        {
            var comic = await _context.Comics
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Comments) 
                        .ThenInclude(cmt => cmt.User) 
                .FirstOrDefaultAsync(c => c.ComicId == comicId);

            return comic;
        }

        // 3. Get Chapter and get prev/after chapter
        public async Task<(Chapter CurrentChapter, int? PrevChapterId, int? NextChapterId)> GetChapterForReadingAsync(int chapterId)
        {
            var currentChapter = await _context.Chapters
                .Include(ch => ch.Comic)
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
        public async Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsPagedAsync(int page, int pageSize)
        {
            var query = _context.Comics
                .Where(c => c.Status == "OnWorking" || c.Status == "Completed");

            int totalCount = await query.CountAsync();

            var comics = await query
                .Include(c => c.ComicTags)
                    .ThenInclude(ct => ct.Tag)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize) 
                .Take(pageSize)              
                .ToListAsync();

            return (comics, totalCount);
        }
    }

}