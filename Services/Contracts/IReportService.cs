using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    /// <summary>
    /// Interface định nghĩa các nghiệp vụ xử lý report (báo cáo vi phạm).
    /// Phân quyền theo vai trò:
    /// - Moderator: xử lý report nhắm vào User thường.
    /// - Admin: xử lý report nhắm vào Moderator.
    /// Hỗ trợ các hành động: Warning, Ban, RemoveRole, Dismiss.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Tạo report mới từ người dùng.
        /// Kiểm tra: không được tự report chính mình,
        /// không được report trùng (đã có report Pending cho cùng target).
        /// </summary>
        /// <param name="reporterId">UserId của người gửi report.</param>
        /// <param name="targetUserId">UserId của người bị report.</param>
        /// <param name="reason">Lý do report (bắt buộc).</param>
        /// <param name="description">Mô tả chi tiết bổ sung (tùy chọn).</param>
        /// <returns>true nếu tạo thành công, false nếu vi phạm ràng buộc.</returns>
        Task<bool> CreateReportAsync(int reporterId, int targetUserId, string reason, string? description);

        /// <summary>
        /// Lấy danh sách report nhắm vào User thường (role = "User") đang ở trạng thái Pending.
        /// Dùng cho trang UserReports của Moderator.
        /// Include Reporter, TargetUser → Role, ProcessedBy.
        /// Sắp xếp theo CreatedAt giảm dần (report mới nhất lên trước).
        /// </summary>
        Task<List<Report>> GetUserReportsAsync();

        /// <summary>
        /// Lấy danh sách report nhắm vào Moderator (role = "Moderator") đang ở trạng thái Pending.
        /// Dùng cho trang ModeratorReports của Admin.
        /// Include Reporter, TargetUser → Role, ProcessedBy.
        /// </summary>
        Task<List<Report>> GetModeratorReportsAsync();

        /// <summary>
        /// Lấy chi tiết một report theo ID.
        /// Include đầy đủ: Reporter, TargetUser → Role, ProcessedBy.
        /// Dùng cho trang Details khi review từng report.
        /// </summary>
        /// <param name="reportId">ID của report cần xem.</param>
        /// <returns>Report nếu tìm thấy, null nếu không tồn tại.</returns>
        Task<Report?> GetReportByIdAsync(int reportId);

        /// <summary>
        /// Xử lý report với một hành động cụ thể (Warning / Ban / RemoveRole / Dismiss).
        /// - Warning: ghi log cảnh cáo cho target user.
        /// - Ban: chuyển status target user sang Banned.
        /// - RemoveRole: hạ role Moderator xuống User (chỉ Admin dùng).
        /// - Dismiss: bỏ qua, không xử phạt.
        /// Cập nhật report: Status = Resolved, ghi ActionTaken, ProcessedById, ProcessedAt.
        /// </summary>
        /// <param name="reportId">ID report cần xử lý.</param>
        /// <param name="processedById">UserId của người xử lý (Moderator hoặc Admin).</param>
        /// <param name="action">Hành động xử lý (enum ReportAction).</param>
        /// <param name="note">Ghi chú xử lý (tùy chọn).</param>
        /// <returns>true nếu thành công, false nếu không tìm thấy report hoặc target user.</returns>
        Task<bool> ProcessReportAsync(int reportId, int processedById, ReportAction action, string? note);

        /// <summary>
        /// Từ chối report: đánh dấu report là Rejected, action = Dismiss.
        /// Không thực hiện hành động xử phạt nào lên target user.
        /// Dùng khi Moderator/Admin cho rằng report không hợp lệ.
        /// </summary>
        /// <param name="reportId">ID report cần reject.</param>
        /// <param name="processedById">UserId của người xử lý.</param>
        /// <param name="note">Ghi chú lý do reject (tùy chọn).</param>
        /// <returns>true nếu thành công, false nếu không tìm thấy report.</returns>
        Task<bool> RejectReportAsync(int reportId, int processedById, string? note);

        /// <summary>
        /// Đếm số report nhắm vào User thường đang Pending.
        /// Dùng cho badge sidebar trong layout Moderator.
        /// </summary>
        Task<int> GetPendingUserReportsCountAsync();

        /// <summary>
        /// Đếm số report nhắm vào Moderator đang Pending.
        /// Dùng cho badge sidebar trong layout Admin.
        /// </summary>
        Task<int> GetPendingModeratorReportsCountAsync();
    }
}