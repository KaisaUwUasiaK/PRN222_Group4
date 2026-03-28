using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IReportService
    {
        /// <summary>
        /// Tạo report mới.
        /// Trả về ReportId nếu tạo thành công, null nếu vi phạm ràng buộc
        /// (tự report mình hoặc đã có report Pending trùng).
        /// ⚠ THAY ĐỔI: trả Task&lt;int?&gt; thay vì Task&lt;bool&gt; để caller lấy reportId
        /// phục vụ gửi notification cho moderator.
        /// </summary>
        Task<int?> CreateReportAsync(int reporterId, int targetUserId,
                                     string reason, string? description,
                                     int? commentId = null);

        /// <summary>Danh sách report Pending nhắm vào User thường (role = "User").</summary>
        Task<List<Report>> GetUserReportsAsync();

        /// <summary>Danh sách report Pending nhắm vào Moderator (role = "Moderator").</summary>
        Task<List<Report>> GetModeratorReportsAsync();

        /// <summary>Chi tiết một report theo ID (include Reporter, TargetUser, Comment, ProcessedBy).</summary>
        Task<Report?> GetReportByIdAsync(int reportId);

        /// <summary>Xử lý report: Warning / Ban / RemoveRole / Dismiss.</summary>
        Task<bool> ProcessReportAsync(int reportId, int processedById,
                                      ReportAction action, string? note);

        /// <summary>Từ chối report (đánh dấu Rejected, không xử phạt target).</summary>
        Task<bool> RejectReportAsync(int reportId, int processedById, string? note);

        /// <summary>Số report Pending nhắm vào User thường — dùng cho badge sidebar Moderator.</summary>
        Task<int> GetPendingUserReportsCountAsync();

        /// <summary>Số report Pending nhắm vào Moderator — dùng cho badge sidebar Admin.</summary>
        Task<int> GetPendingModeratorReportsCountAsync();

        /// <summary>Lấy role name của target user — dùng để route notification đúng nơi khi tạo report.</summary>
        Task<string?> GetTargetRoleAsync(int targetUserId);
    }
}