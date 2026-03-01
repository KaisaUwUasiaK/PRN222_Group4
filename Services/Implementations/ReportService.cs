using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    /// <summary>
    /// Service xử lý toàn bộ nghiệp vụ report (báo cáo vi phạm).
    /// Phân luồng report theo role của TargetUser:
    /// - TargetUser là "User" → Moderator xử lý.
    /// - TargetUser là "Moderator" → Admin xử lý.
    /// Hỗ trợ 4 hành động: Warning (cảnh cáo), Ban (khóa tài khoản),
    /// RemoveRole (hạ quyền Moderator), Dismiss (bỏ qua).
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách report Pending nhắm vào User thường.
        /// Lọc: TargetUser.Role.RoleName == "User" AND Status == Pending.
        /// Include đầy đủ quan hệ để view hiển thị thông tin.
        /// </summary>
        public async Task<List<Report>> GetUserReportsAsync()
        {
            return await _context.Reports
                .Where(r => r.TargetUser.Role.RoleName == "User" && r.Status == ReportStatus.Pending)
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                    .ThenInclude(u => u.Role)
                .Include(r => r.ProcessedBy)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách report Pending nhắm vào Moderator.
        /// Lọc: TargetUser.Role.RoleName == "Moderator" AND Status == Pending.
        /// Chỉ Admin mới được gọi (phân quyền ở controller).
        /// </summary>
        public async Task<List<Report>> GetModeratorReportsAsync()
        {
            return await _context.Reports
                .Where(r => r.TargetUser.Role.RoleName == "Moderator" && r.Status == ReportStatus.Pending)
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                    .ThenInclude(u => u.Role)
                .Include(r => r.ProcessedBy)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Tạo report mới.
        /// Ràng buộc:
        /// - Không tự report chính mình (reporterId == targetUserId → false).
        /// - Không report trùng: nếu đã tồn tại report Pending cùng reporter + target → false.
        /// Mặc định Status = Pending, CreatedAt = UTC now.
        /// </summary>
        public async Task<bool> CreateReportAsync(int reporterId, int targetUserId, string reason, string? description)
        {
            if (reporterId == targetUserId) return false;

            var existingReport = await _context.Reports
                .FirstOrDefaultAsync(r => r.ReporterId == reporterId &&
                           r.TargetUserId == targetUserId &&
                           r.Status == ReportStatus.Pending);

            if (existingReport != null) return false;

            var report = new Report
            {
                ReporterId = reporterId,
                TargetUserId = targetUserId,
                Reason = reason,
                Description = description,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy chi tiết report theo ID.
        /// Include: Reporter, TargetUser → Role, ProcessedBy.
        /// Dùng cho trang Details và kiểm tra quyền trước khi xử lý.
        /// </summary>
        public async Task<Report?> GetReportByIdAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                    .ThenInclude(u => u.Role)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        /// <summary>
        /// Xử lý report theo hành động được chọn.
        /// Cập nhật Report: Status = Resolved, ghi ActionTaken, ProcessedById, ProcessedAt.
        /// Thực thi hành động lên TargetUser:
        /// - Warning: chỉ ghi log cảnh cáo (không thay đổi trạng thái user).
        /// - Ban: chuyển user.Status = Banned (user không thể đăng nhập).
        /// - RemoveRole: hạ quyền Moderator → User (chỉ áp dụng nếu target là Moderator).
        /// - Dismiss: không làm gì, chỉ đóng report.
        /// Mỗi hành động đều ghi Log để audit trail.
        /// </summary>
        public async Task<bool> ProcessReportAsync(int reportId, int processedById, ReportAction action, string? note)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;

            var targetUser = await _context.Users.FindAsync(report.TargetUserId);
            if (targetUser == null) return false;

            report.Status = ReportStatus.Resolved;
            report.ActionTaken = action;
            report.ResolutionNote = note;
            report.ProcessedById = processedById;
            report.ProcessedAt = DateTime.UtcNow;

            switch (action)
            {
                case ReportAction.Warning:
                    // Ghi log cảnh cáo — user vẫn hoạt động bình thường
                    await LogActionAsync(targetUser.UserId, $"Warning: {report.Reason} - {note}");
                    break;

                case ReportAction.Ban:
                    // Khóa tài khoản — user không thể đăng nhập
                    targetUser.Status = AccountStatus.Banned;
                    await LogActionAsync(targetUser.UserId, $"Banned: {report.Reason}");
                    break;

                case ReportAction.RemoveRole:
                    // Hạ quyền Moderator → User (chỉ Admin xử lý report Moderator)
                    if (targetUser.Role.RoleName == "Moderator")
                    {
                        var userRole = await _context.Roles
                            .FirstOrDefaultAsync(r => r.RoleName == "User");
                        if (userRole != null)
                        {
                            targetUser.RoleId = userRole.RoleId;
                            await LogActionAsync(targetUser.UserId, $"Moderator role removed: {report.Reason}");
                        }
                    }
                    break;

                case ReportAction.Dismiss:
                    // Không xử phạt — chỉ đóng report
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Từ chối report — đánh dấu report không hợp lệ.
        /// Status = Rejected, ActionTaken = Dismiss (không xử phạt).
        /// Dùng khi Moderator/Admin cho rằng report là sai hoặc không đủ bằng chứng.
        /// </summary>
        public async Task<bool> RejectReportAsync(int reportId, int processedById, string? note)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;

            report.Status = ReportStatus.Rejected;
            report.ActionTaken = ReportAction.Dismiss;
            report.ResolutionNote = note;
            report.ProcessedById = processedById;
            report.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Đếm report Pending nhắm vào User thường.
        /// Query nhẹ (COUNT), gọi ở mọi trang moderator cho badge sidebar.
        /// </summary>
        public async Task<int> GetPendingUserReportsCountAsync()
        {
            return await _context.Reports
                .CountAsync(r => r.TargetUser.Role.RoleName == "User" && r.Status == ReportStatus.Pending);
        }

        /// <summary>
        /// Đếm report Pending nhắm vào Moderator.
        /// Query nhẹ (COUNT), gọi ở mọi trang admin cho badge sidebar.
        /// </summary>
        public async Task<int> GetPendingModeratorReportsCountAsync()
        {
            return await _context.Reports
                .CountAsync(r => r.TargetUser.Role.RoleName == "Moderator" && r.Status == ReportStatus.Pending);
        }

        /// <summary>
        /// Ghi log hành động xử lý vào bảng Log.
        /// Phục vụ audit trail — theo dõi ai bị xử phạt gì, khi nào.
        /// Gọi nội bộ từ ProcessReportAsync khi thực thi Warning, Ban, RemoveRole.
        /// </summary>
        /// <param name="userId">UserId của người bị xử phạt.</param>
        /// <param name="action">Mô tả hành động (vd: "Banned: spam content").</param>
        private async Task LogActionAsync(int userId, string action)
        {
            var log = new Log
            {
                UserId = userId,
                Action = action,
                CreatedAt = DateTime.UtcNow
            };
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}