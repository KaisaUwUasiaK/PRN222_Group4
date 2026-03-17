using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IHomeService
    {
        Task<List<Comic>> GetNewComicsAsync();

        Task<Comic?> GetTrendingComicAsync();

        Task<List<Comic?>> GetTrendingComicsAsync();

        Task<List<Comic?>> GetMaybeYouLikeComicsAsync();

    }
}
