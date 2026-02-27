using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private AppDbContext _context;

        public HomeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Comic>> GetNewComicsAsync()
        {
            return await _context.Comics
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.CreatedAt)
            .Take(12)
            .ToListAsync();
        }

        public async Task<Comic> GetTrendingComicAsync()
        {
            return await _context.Comics
            .Include(c => c.ComicTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.ViewCount)
            .FirstOrDefaultAsync();
        }
    }
}
