using Group4_ReadingComicWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Services
{
    public interface IComicService
    {
        Task<(List<Comic> Comics, int TotalCount)> GetPublicComicsPagedAsync(int page, int pageSize, string? searchTerm = null, List<string>? tags = null, string? status = null, string? sortBy = null);
        Task<List<Comic>> GetPublicComicsAsync();
        Task<Comic?> GetComicDetailAsync(int comicId);
        Task<Comic?> GetRandomComicAsync(string? searchTerm = null, List<string>? tags = null, string? status = null);
        Task<(Chapter? CurrentChapter, int? PrevChapterId, int? NextChapterId)> GetChapterForReadingAsync(int chapterId);
        Task IncrementViewCountAsync(int comicId);
    }
}