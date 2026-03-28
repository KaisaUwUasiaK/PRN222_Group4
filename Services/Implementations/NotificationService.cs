using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // ── Core ──────────────────────────────────────────────────────────────

        public async Task SendAsync(
            int userId, string title, string content,
            string type = "system", string? actionUrl = null)
        {
            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                NotificationType = type,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notif);
            await _db.SaveChangesAsync();

            await _hub.Clients
                .Group($"user_{userId}")
                .SendAsync("ReceiveNotification", new
                {
                    notif.NotificationId,
                    notif.Title,
                    notif.Content,
                    notif.NotificationType,
                    notif.ActionUrl,
                    notif.IsRead,
                    CreatedAt = notif.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                });
        }

        // ── Comic moderation ──────────────────────────────────────────────────

        public Task ComicApprovedAsync(int authorId, int comicId, string comicTitle)
            => SendAsync(
                authorId,
                $"Your comic \"{comicTitle}\" has been approved",
                $"Congratulations! Your comic <b>\"{comicTitle}\"</b> has been reviewed and approved by a Moderator.\n\nIt is now publicly visible.",
                "comic_approved",
                $"/Comic/Detail/{comicId}");

        public Task ComicRejectedAsync(int authorId, int comicId, string comicTitle, string? reason)
            => SendAsync(
                authorId,
                $"Your comic \"{comicTitle}\" has been rejected",
                $"Your comic <b>\"{comicTitle}\"</b> was not approved.\n\n<b>Reason:</b> {reason ?? "No notes provided."}\n\nYou may edit and resubmit.",
                "comic_rejected",
                $"/PersonalComic/Edit/{comicId}");

        public Task ComicHiddenAsync(int authorId, int comicId, string comicTitle, string? reason)
            => SendAsync(
                authorId,
                $"Your comic \"{comicTitle}\" has been hidden",
                $"Your comic <b>\"{comicTitle}\"</b> has been hidden from the public by a Moderator.\n\n<b>Reason:</b> {reason ?? "No notes provided."}",
                "comic_hidden",
                $"/PersonalComic/Edit/{comicId}");

        public async Task NewChapterAsync(
            IEnumerable<int> followerIds, int comicId,
            string comicTitle, int chapterNumber)
        {
            foreach (var uid in followerIds)
                await SendAsync(
                    uid,
                    $"{comicTitle} — Chapter {chapterNumber} is out",
                    $"A new chapter has been uploaded for <b>\"{comicTitle}\"</b>: <b>Chapter {chapterNumber}</b>.",
                    "new_chapter",
                    $"/Comic/Detail/{comicId}");
        }

        public Task NewComicPendingAsync(
            int moderatorId, int comicId,
            string comicTitle, string authorName)
            => SendAsync(
                moderatorId,
                $"New comic pending review: \"{comicTitle}\"",
                $"Author <b>@{authorName}</b> has submitted <b>\"{comicTitle}\"</b> for review.\n\nPlease approve or reject it.",
                "new_pending",
                $"/Moderation/Review/{comicId}");

        // ── Report notifications ──────────────────────────────────────────────

        public async Task NewReportForAllModeratorsAsync(int reportId, string reportedContent)
        {
            var moderatorRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Moderator");
            if (moderatorRole == null) return;

            var moderatorIds = await _db.Users
                .Where(u => u.RoleId == moderatorRole.RoleId)
                .Select(u => u.UserId)
                .ToListAsync();

            foreach (var modId in moderatorIds)
                await SendAsync(
                    modId,
                    "New report requires your attention",
                    $"A user has submitted a report regarding: <b>{reportedContent}</b>.\n\nPlease review and take action.",
                    "new_report",
                    $"/Reports/{reportId}");
        }

        public async Task NewReportForAllAdminsAsync(int reportId, string reportedContent)
        {
            var adminRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null) return;

            var adminIds = await _db.Users
                .Where(u => u.RoleId == adminRole.RoleId)
                .Select(u => u.UserId)
                .ToListAsync();

            foreach (var adminId in adminIds)
                await SendAsync(
                    adminId,
                    "New moderator report requires your attention",
                    $"A user has submitted a report regarding a Moderator: <b>{reportedContent}</b>.\n\nPlease review and take action.",
                    "new_report",
                    $"/Reports/{reportId}");
        }

        public Task ReportHandledNotifyReporterAsync(
            int reporterId, string targetUsername,
            string actionLabel, string? note, int reportId)
            => SendAsync(
                reporterId,
                "Your report has been processed",
                $"Your report against <b>@{targetUsername}</b> has been reviewed by a Moderator.\n\n" +
                $"<b>Result:</b> {actionLabel}" +
                (string.IsNullOrWhiteSpace(note) ? "" : $"\n<b>Note:</b> {note}"),
                "report_handled");

        public Task ReportRejectedNotifyReporterAsync(int reporterId, int reportId)
            => SendAsync(
                reporterId,
                "Your report was not accepted",
                "Your report has been reviewed and determined to have <b>insufficient grounds</b> for action.\n\nThank you for helping keep the community safe.",
                "report_rejected");

        // ── Account notifications ─────────────────────────────────────────────

        public Task AccountWarningAsync(int userId, string reason)
            => SendAsync(
                userId,
                "Your account has received a warning",
                $"Your account has been flagged for a violation.\n\n<b>Reason:</b> {reason}\n\nThis is a warning. Further violations may result in a temporary suspension.",
                "account_warning");

        public Task AccountBannedAsync(int userId, string reason)
            => SendAsync(
                userId,
                "Your account has been suspended",
                $"Your account has been suspended due to a terms of service violation.\n\n<b>Reason:</b> {reason}",
                "account_banned");

        public Task AccountUnbannedAsync(int userId)
            => SendAsync(
                userId,
                "Your account has been reinstated",
                "Your account has been reinstated by an Administrator. You may now log in and use the platform normally.",
                "account_unbanned");
    }
}