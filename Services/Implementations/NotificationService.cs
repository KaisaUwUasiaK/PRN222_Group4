using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    /// <summary>
    /// Mỗi lần gửi thông báo:
    ///   1. Lưu record vào bảng Notifications (DB) để user đọc lại sau.
    ///   2. Push real-time qua SignalR group "user_{userId}".
    /// Nếu user đang offline, thông báo vẫn lưu DB và client load lại khi mở panel.
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

            // Push real-time — nếu user online thì nhận ngay
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
                $"Truyện \"{comicTitle}\" đã được phê duyệt",
                $"Chúc mừng! Truyện <b>\"{comicTitle}\"</b> đã được Moderator phê duyệt thành công.\n\nTruyện của bạn hiện đã hiển thị công khai.",
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
                "comic_hidden",
                $"/PersonalComic/Edit/{comicId}");

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

        // ── Report notifications ──────────────────────────────────────────────

        /// <summary>
        /// Gửi cho TẤT CẢ Moderator khi có report mới.
        /// Query DB lấy danh sách moderator, gửi lần lượt.
        /// </summary>
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
            {
                await SendAsync(
                    modId,
                    "Có báo cáo mới cần xử lý",
                    $"Một người dùng vừa gửi báo cáo về: <b>{reportedContent}</b>.\n\nVui lòng kiểm tra và xử lý báo cáo này.",
                    "new_report",
                    $"/Reports/{reportId}");
            }
        }

        /// <summary>
        /// Thông báo cho REPORTER khi report đã được xử lý (cả 2 chiều: reporter + target).
        /// Target notification (Warning/Ban) được gọi riêng trong controller.
        /// </summary>
        public Task ReportHandledNotifyReporterAsync(
            int reporterId,
            string targetUsername,
            string actionLabel,
            string? note,
            int reportId)
            => SendAsync(
                reporterId,
                "Báo cáo của bạn đã được xử lý",
                $"Báo cáo bạn gửi về người dùng <b>@{targetUsername}</b> đã được Moderator xem xét.\n\n" +
                $"<b>Kết quả:</b> {actionLabel}" +
                (string.IsNullOrWhiteSpace(note) ? "" : $"\n<b>Ghi chú:</b> {note}"),
                "report_handled",
                $"/Reports/{reportId}");

        /// <summary>
        /// Thông báo cho REPORTER khi report bị từ chối (không hợp lệ).
        /// </summary>
        public Task ReportRejectedNotifyReporterAsync(int reporterId, int reportId)
            => SendAsync(
                reporterId,
                "Báo cáo của bạn không được chấp nhận",
                "Báo cáo bạn gửi đã được Moderator xem xét và xác định <b>không đủ cơ sở</b> để xử lý.\n\nCảm ơn bạn đã đóng góp cho cộng đồng.",
                "report_rejected",
                $"/Reports/{reportId}");

        // ── Account notifications ─────────────────────────────────────────────

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

        /// <summary>
        /// Gửi cho TẤT CẢ Admin khi có report nhắm vào Moderator.
        /// </summary>
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
            {
                await SendAsync(
                    adminId,
                    "Có báo cáo Moderator mới cần xử lý",
                    $"Một người dùng vừa gửi báo cáo về Moderator: <b>{reportedContent}</b>.\n\nVui lòng kiểm tra và xử lý.",
                    "new_report",
                    $"/Reports/{reportId}");
            }
        }
    }
}