using Microsoft.AspNetCore.SignalR;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    /// <summary>
    /// Implementation của INotificationService.
    /// Mỗi lần gửi thông báo: lưu record vào bảng Notifications (DB)
    /// rồi push real-time tới client qua SignalR group "user_{userId}".
    /// Nếu user đang offline thì thông báo vẫn được lưu DB,
    /// client sẽ tự load lại khi mở panel lần sau.
    /// </summary>
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
            int userId,
            string title,
            string content,
            string type = "system",
            string? actionUrl = null)
        {
            // 1. Lưu vào DB
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

            // 2. Push real-time — client trong group này nhận ngay
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

        // ── Shortcut methods ─────────────────────────────────────────────────

        public Task ComicApprovedAsync(int authorId, int comicId, string comicTitle)
            => SendAsync(
                authorId,
                $"Truyện \"{comicTitle}\" đã được phê duyệt",
                $"Chúc mừng! Truyện <b>\"{comicTitle}\"</b> đã được Moderator xem xét và phê duyệt thành công.\n\nTruyện của bạn hiện đã hiển thị công khai.",
                "comic_approved",
                $"/Comic/Detail/{comicId}");

        public Task ComicRejectedAsync(int authorId, int comicId, string comicTitle, string? reason)
            => SendAsync(
                authorId,
                $"Truyện \"{comicTitle}\" bị từ chối",
                $"Truyện <b>\"{comicTitle}\"</b> của bạn đã bị từ chối.\n\n<b>Lý do:</b> {reason ?? "Không có ghi chú."}\n\nBạn có thể chỉnh sửa và gửi lại.",
                "comic_rejected",
                $"/PersonalComic/Edit/{comicId}");

        public Task ComicHiddenAsync(int authorId, int comicId, string comicTitle, string? reason)
            => SendAsync(
                authorId,
                $"Truyện \"{comicTitle}\" đã bị ẩn",
                $"Truyện <b>\"{comicTitle}\"</b> đã bị Moderator ẩn khỏi trang chính.\n\n<b>Lý do:</b> {reason ?? "Không có ghi chú."}",
                "comic_hidden");

        public async Task NewChapterAsync(
            IEnumerable<int> followerIds,
            int comicId,
            string comicTitle,
            int chapterNumber)
        {
            foreach (var uid in followerIds)
                await SendAsync(
                    uid,
                    $"{comicTitle} — Chương {chapterNumber} mới",
                    $"Truyện <b>\"{comicTitle}\"</b> bạn đang theo dõi vừa cập nhật <b>chương {chapterNumber}</b>.",
                    "new_chapter",
                    $"/Comic/Detail/{comicId}");
        }

        public Task NewComicPendingAsync(
            int moderatorId,
            int comicId,
            string comicTitle,
            string authorName)
            => SendAsync(
                moderatorId,
                $"Truyện mới chờ duyệt: \"{comicTitle}\"",
                $"Tác giả <b>@{authorName}</b> vừa đăng truyện <b>\"{comicTitle}\"</b> và đang chờ kiểm duyệt.\n\nVui lòng xem xét và phê duyệt hoặc từ chối.",
                "new_pending",
                $"/Moderation/Review/{comicId}");

        public Task NewReportAsync(int moderatorId, int reportId, string reportedContent)
            => SendAsync(
                moderatorId,
                "Có báo cáo mới cần xử lý",
                $"Một người dùng vừa gửi báo cáo về: <b>{reportedContent}</b>.\n\nVui lòng kiểm tra và xử lý báo cáo này.",
                "new_report",
                $"/Report/Details/{reportId}");

        public Task AccountWarningAsync(int userId, string reason)
            => SendAsync(
                userId,
                "Tài khoản của bạn nhận được cảnh báo",
                $"Tài khoản của bạn đã bị ghi nhận vi phạm.\n\n<b>Lý do:</b> {reason}\n\nĐây là cảnh báo. Nếu tiếp tục vi phạm, tài khoản có thể bị tạm khóa.",
                "account_warning");

        public Task AccountBannedAsync(int userId, string reason)
            => SendAsync(
                userId,
                "Tài khoản của bạn đã bị khóa",
                $"Tài khoản của bạn đã bị khóa do vi phạm điều khoản.\n\n<b>Lý do:</b> {reason}",
                "account_banned");

        public Task AccountUnbannedAsync(int userId)
            => SendAsync(
                userId,
                "Tài khoản của bạn đã được mở khóa",
                "Tài khoản của bạn đã được Admin mở khóa. Bạn có thể đăng nhập và sử dụng bình thường.",
                "account_unbanned");
    }
}