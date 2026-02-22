using Group4_ReadingComicWeb.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Services
{
    public interface IPersonalComicService
    {
        // Comic Operations
        Task<List<Comic>> GetUserComicsAsync(int userId);
        Task<List<Tag>> GetAllTagsAsync();
        Task CreateComicAsync(int userId, Comic comic, int[] selectedTags, IFormFile coverImage);
        Task<Comic> GetComicWithChaptersAsync(int comicId, int userId);
        Task<Comic> GetComicForEditAsync(int comicId, int userId);
        Task<bool> EditComicAsync(int userId, int id, Comic comic, int[] selectedTags, IFormFile coverImage);
        Task<bool> DeleteComicAsync(int userId, int id);

        // Chapter Operations
        Task<(bool IsSuccess, string ErrorMessage)> CreateChapterAsync(int userId, int comicId, int chapterNumber, string title, List<IFormFile> pages);
        Task<(Chapter Chapter, List<string> Images)> GetChapterForReadAsync(int chapterId, int userId);
        Task<Chapter> GetChapterAsync(int chapterId, int userId);
        Task<bool> EditChapterAsync(int userId, int id, Chapter model, List<IFormFile> newPages);
        Task<int?> DeleteChapterAsync(int userId, int id);
    }
}