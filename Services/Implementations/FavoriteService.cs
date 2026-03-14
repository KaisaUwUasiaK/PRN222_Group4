using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        private readonly AppDbContext _context;

        public FavoriteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ToggleFavoriteAsync(int comicId, int userId)
        {
            var existingFav = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ComicId == comicId && f.UserId == userId);

            if (existingFav != null)
            {
                _context.Favorites.Remove(existingFav);
                await _context.SaveChangesAsync();
                return false;
            }
            else
            {
                var newFav = new Favorite
                {
                    ComicId = comicId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Favorites.Add(newFav);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> IsFavoritedAsync(int comicId, int userId)
        {
            return await _context.Favorites.AnyAsync(f => f.ComicId == comicId && f.UserId == userId);
        }

        public async Task<List<Comic>> GetUserFavoritesAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt) 
                .Select(f => f.Comic)
                .ToListAsync();
        }
    }
}