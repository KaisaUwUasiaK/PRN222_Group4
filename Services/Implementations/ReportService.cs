using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tạo report mới.
        /// Trả về ReportId (int) nếu thành công, null nếu vi phạm ràng buộc.
        /// ⚠ THAY ĐỔI SO VỚI BẢN CŨ: trả Task&lt;int?&gt; thay vì Task&lt;bool&gt;.
        /// </summary>
        public async Task<int?> CreateReportAsync(
            int reporterId, int targetUserId,
            string reason, string? description,
            int? commentId = null)
        {
            if (reporterId == targetUserId) return null;

            var existing = await _context.Reports
                .FirstOrDefaultAsync(r =>
                    r.ReporterId == reporterId &&
                    r.TargetUserId == targetUserId &&
                    r.CommentId == commentId &&
                    r.Status == ReportStatus.Pending);

            if (existing != null) return null;

            var report = new Report
            {
                ReporterId = reporterId,
                TargetUserId = targetUserId,
                CommentId = commentId,
                Reason = reason,
                Description = description,
                ReportType = commentId.HasValue ? ReportType.Comment : ReportType.User,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return report.ReportId;  // EF Core đã set Id sau SaveChanges
        }

        public async Task<List<Report>> GetUserReportsAsync()
        {
            return await _context.Reports
                .Where(r => r.TargetUser.Role.RoleName == "User" &&
                            r.Status == ReportStatus.Pending)
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser).ThenInclude(u => u.Role)
                .Include(r => r.Comment)
                .Include(r => r.ProcessedBy)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Report>> GetModeratorReportsAsync()
        {
            return await _context.Reports
                .Where(r => r.TargetUser.Role.RoleName == "Moderator" &&
                            r.Status == ReportStatus.Pending)
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser).ThenInclude(u => u.Role)
                .Include(r => r.Comment)
                .Include(r => r.ProcessedBy)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Report?> GetReportByIdAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser).ThenInclude(u => u.Role)
                .Include(r => r.Comment)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<bool> ProcessReportAsync(
            int reportId, int processedById,
            ReportAction action, string? note)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;

            var targetUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == report.TargetUserId);
            if (targetUser == null) return false;

            report.Status = ReportStatus.Resolved;
            report.ActionTaken = action;
            report.ResolutionNote = note;
            report.ProcessedById = processedById;
            report.ProcessedAt = DateTime.UtcNow;

            switch (action)
            {
                case ReportAction.Warning:
                    await LogActionAsync(targetUser.UserId,
                        $"Warning: {report.Reason} - {note}");
                    break;

                case ReportAction.Ban:
                    targetUser.Status = AccountStatus.Banned;
                    await LogActionAsync(targetUser.UserId,
                        $"Banned: {report.Reason}");
                    break;

                case ReportAction.RemoveRole:
                    if (targetUser.Role.RoleName == "Moderator")
                    {
                        var userRole = await _context.Roles
                            .FirstOrDefaultAsync(r => r.RoleName == "User");
                        if (userRole != null)
                        {
                            targetUser.RoleId = userRole.RoleId;
                            await LogActionAsync(targetUser.UserId,
                                $"Moderator role removed: {report.Reason}");
                        }
                    }
                    break;

                case ReportAction.Dismiss:
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectReportAsync(
            int reportId, int processedById, string? note)
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

        public async Task<int> GetPendingUserReportsCountAsync()
            => await _context.Reports
                .CountAsync(r => r.TargetUser.Role.RoleName == "User" &&
                                 r.Status == ReportStatus.Pending);

        public async Task<int> GetPendingModeratorReportsCountAsync()
            => await _context.Reports
                .CountAsync(r => r.TargetUser.Role.RoleName == "Moderator" &&
                                 r.Status == ReportStatus.Pending);

        /// <summary>
        /// Lấy role name của target user.
        /// Dùng trong CreateReport để quyết định gửi notification cho Mod hay Admin.
        /// </summary>
        public async Task<string?> GetTargetRoleAsync(int targetUserId)
        {
            return await _context.Users
                .Where(u => u.UserId == targetUserId)
                .Select(u => u.Role.RoleName)
                .FirstOrDefaultAsync();
        }

        private async Task LogActionAsync(int userId, string action)
        {
            _context.Logs.Add(new Log
            {
                UserId = userId,
                Action = action,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}