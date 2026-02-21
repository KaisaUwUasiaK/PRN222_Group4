using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IReportService
    {
        Task<bool> CreateReportAsync(int reporterId, int targetUserId, string reason, string? description);
        Task<List<Report>> GetUserReportsAsync();
        Task<List<Report>> GetModeratorReportsAsync();
        Task<Report?> GetReportByIdAsync(int reportId);
        Task<bool> ProcessReportAsync(int reportId, int processedById, ReportAction action, string? note);
        Task<bool> RejectReportAsync(int reportId, int processedById, string? note);
        Task<int> GetPendingUserReportsCountAsync();
        Task<int> GetPendingModeratorReportsCountAsync();
    }
}