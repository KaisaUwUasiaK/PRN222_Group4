using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
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

        public async Task<List<Comic>> GetPublicComicsAsync()
        {
            return await _context.Comics
                .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
                .ToListAsync();
        }

        public async Task<Comic> GetComicDetailAsync(int comicId)
        {
            return await _context.Comics
                .Include(c => c.Chapters.OrderBy(ch => ch.ChapterNumber))
                .Include(c => c.Author)
                .FirstOrDefaultAsync(m => m.ComicId == comicId);
        }

        public async Task<(Chapter CurrentChapter, int? PrevChapterId, int? NextChapterId)> GetChapterForReadingAsync(int chapterId)
        {
            var chapter = await _context.Chapters
                .Include(ch => ch.Comic)
                .FirstOrDefaultAsync(ch => ch.ChapterId == chapterId);

            if (chapter == null) return (null, null, null);

            var prev = await _context.Chapters
                .Where(c => c.ComicId == chapter.ComicId && c.ChapterNumber < chapter.ChapterNumber)
                .OrderByDescending(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            var next = await _context.Chapters
                .Where(c => c.ComicId == chapter.ComicId && c.ChapterNumber > chapter.ChapterNumber)
                .OrderBy(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            return (chapter, prev?.ChapterId, next?.ChapterId);
        }
    }
}