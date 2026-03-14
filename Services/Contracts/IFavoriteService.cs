using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IFavoriteService
    {
        Task<bool> ToggleFavoriteAsync(int comicId, int userId);

        Task<bool> IsFavoritedAsync(int comicId, int userId);

        Task<List<Comic>> GetUserFavoritesAsync(int userId);
    }
}