using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface ITagService
    {
        Task<List<TagListItemViewModel>> GetAllWithUsageAsync();
        Task<Tag?> GetByIdAsync(int id);
        Task<bool> IsTagNameExistsAsync(string tagName, int? excludeTagId = null);
        Task CreateAsync(Tag tag);
        Task<bool> UpdateAsync(Tag tag);
        Task<bool> IsInUseAsync(int id);
        Task<int> GetComicUsageCountAsync(int id);
        Task<int> GetTotalTagsCountAsync();
        Task<bool> DeleteAsync(int id);
    }
}