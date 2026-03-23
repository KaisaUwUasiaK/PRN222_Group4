using Group4_ReadingComicWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Services
{
    public interface IComicService
    {
        Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsPagedAsync(int page, int pageSize, string? searchTerm = null);
        Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsAdvancedAsync(
            int page, int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            string? status = null,
            List<int>? tagIds = null);
        Task<List<Comic>> GetPublicComicsAsync();
        Task<Comic?> GetComicDetailAsync(int comicId);
        Task<(Chapter? CurrentChapter, int? PrevChapterId, int? NextChapterId)> GetChapterForReadingAsync(int chapterId);
        Task IncrementViewCountAsync(int comicId);
        Task<List<Tag>> GetAllTagsAsync();
        Task<Comic?> GetRandomComicAsync(string? status = null, List<int>? tagIds = null);
    }
}