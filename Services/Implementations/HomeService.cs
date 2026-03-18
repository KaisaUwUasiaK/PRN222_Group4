using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly AppDbContext _context;

        public HomeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Comic>> GetNewComicsAsync()
        {
            return await _context.Comics
            .Include(c => c.Author)
            .Include(c => c.Chapters)
            .Include(c => c.ComicTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.Chapters.Max(ch => (DateTime?)ch.CreatedAt))
            .Take(5)
            .ToListAsync();
        }

        public async Task<List<Comic>> GetTrendingComicsAsync()
        {
            return await _context.Comics
            .Include(c => c.Author)
            .Include(c => c.Chapters)
            .Include(c => c.ComicTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.ViewCount)
            .Take(5)
            .ToListAsync();
        }

        public async Task<List<Comic>> GetMaybeYouLikeComicsAsync()
        {
            return await _context.Comics
            .Include(c => c.Author)
            .Include(c => c.Chapters)
            .Include(c => c.ComicTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderBy(c => Guid.NewGuid())
            .Take(5)
            .ToListAsync();
        }

        public async Task<Comic?> GetTrendingComicAsync()
        {
            return await _context.Comics
            .Include(c => c.Author)
            .Include(c => c.ComicTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.ViewCount)
            .FirstOrDefaultAsync();
        }
    }
}
