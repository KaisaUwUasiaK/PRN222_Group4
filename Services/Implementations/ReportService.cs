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

        public async Task<Report?> GetReportByIdAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                    .ThenInclude(u => u.Role)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

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
                    await LogActionAsync(targetUser.UserId, $"Warning: {report.Reason} - {note}");
                    break;

                case ReportAction.Ban:
                    targetUser.Status = AccountStatus.Banned;
                    await LogActionAsync(targetUser.UserId, $"Banned: {report.Reason}");
                    break;

                case ReportAction.RemoveRole:
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
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

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

        public async Task<int> GetPendingUserReportsCountAsync()
        {
            return await _context.Reports
                .CountAsync(r => r.TargetUser.Role.RoleName == "User" && r.Status == ReportStatus.Pending);
        }

        public async Task<int> GetPendingModeratorReportsCountAsync()
        {
            return await _context.Reports
                .CountAsync(r => r.TargetUser.Role.RoleName == "Moderator" && r.Status == ReportStatus.Pending);
        }

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