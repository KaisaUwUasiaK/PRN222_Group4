using Group4_ReadingComicWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Services
{
    public interface IComicService
    {
        Task<List<Comic>> GetPublicComicsAsync();
        Task<Comic> GetComicDetailAsync(int comicId);

        Task<(Chapter CurrentChapter, int? PrevChapterId, int? NextChapterId)> GetChapterForReadingAsync(int chapterId);
    }
}