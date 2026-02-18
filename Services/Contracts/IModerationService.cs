using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IModerationService
    {
        Task<List<ComicModeration>> GetPendingComicsAsync();
        Task<ComicModeration?> GetModerationByIdAsync(int moderationId);
        Task<ComicModeration?> GetModerationByComicIdAsync(int comicId);
        Task<List<ComicModeration>> GetModerationHistoryAsync(int comicId);
        Task<bool> ApproveComicAsync(int moderationId, int moderatorId);
        Task<bool> RejectComicAsync(int moderationId, int moderatorId, string reason);
        Task<bool> HideComicAsync(int moderationId, int moderatorId, string reason);
        Task<List<ComicModeration>> GetAllModerationsAsync();
        Task<int> GetPendingCountAsync();
        Task<int> GetApprovedCountThisMonthAsync();
        Task<int> GetRejectedCountThisMonthAsync();
        Task<int> GetHiddenCountAsync();
    }
}