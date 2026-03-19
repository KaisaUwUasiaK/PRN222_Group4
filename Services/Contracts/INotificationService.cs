namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface INotificationService
    {
        // ── Core ─────────────────────────────────────────────────────────────

        /// <summary>Gửi thông báo tuỳ chỉnh tới 1 user bất kỳ.</summary>
        Task SendAsync(int userId, string title, string content,
                       string type = "system", string? actionUrl = null);

        // ── Comic moderation ──────────────────────────────────────────────────

        /// <summary>Gửi cho TÁC GIẢ khi truyện được DUYỆT bởi Moderator.</summary>
        Task ComicApprovedAsync(int authorId, int comicId, string comicTitle);

        /// <summary>Gửi cho TÁC GIẢ khi truyện bị TỪ CHỐI bởi Moderator.</summary>
        Task ComicRejectedAsync(int authorId, int comicId, string comicTitle, string? reason);

        /// <summary>Gửi cho TÁC GIẢ khi truyện bị ẨN sau khi đã duyệt.</summary>
        Task ComicHiddenAsync(int authorId, int comicId, string comicTitle, string? reason);

        /// <summary>Gửi cho danh sách FOLLOWER khi có CHƯƠNG MỚI.</summary>
        Task NewChapterAsync(IEnumerable<int> followerIds, int comicId,
                             string comicTitle, int chapterNumber);

        /// <summary>Gửi cho 1 MODERATOR khi có TRUYỆN MỚI CHỜ DUYỆT.</summary>
        Task NewComicPendingAsync(int moderatorId, int comicId,
                                  string comicTitle, string authorName);

        // ── Report notifications ──────────────────────────────────────────────

        /// <summary>
        /// Gửi cho TẤT CẢ MODERATOR khi có REPORT MỚI từ user.
        /// Tự động query toàn bộ Moderator trong DB.
        /// </summary>
        /// <summary>Gửi cho TẤT CẢ MODERATOR khi có report nhắm vào User.</summary>
        Task NewReportForAllModeratorsAsync(int reportId, string reportedContent);

        /// <summary>Gửi cho TẤT CẢ ADMIN khi có report nhắm vào Moderator.</summary>
        Task NewReportForAllAdminsAsync(int reportId, string reportedContent);

        /// <summary>
        /// Thông báo cho NGƯỜI TỐ CÁO (reporter) khi report đã được MODERATOR XỬ LÝ.
        /// actionLabel ví dụ: "Cảnh cáo", "Khóa tài khoản", "Bỏ qua"
        /// </summary>
        Task ReportHandledNotifyReporterAsync(int reporterId, string targetUsername,
                                              string actionLabel, string? note, int reportId);

        /// <summary>
        /// Thông báo cho NGƯỜI TỐ CÁO (reporter) khi report bị REJECT (không hợp lệ).
        /// </summary>
        Task ReportRejectedNotifyReporterAsync(int reporterId, int reportId);

        // ── Account notifications ─────────────────────────────────────────────

        /// <summary>Gửi cho USER khi bị CẢNH BÁO bởi Admin/Moderator.</summary>
        Task AccountWarningAsync(int userId, string reason);

        /// <summary>Gửi cho USER khi bị BAN.</summary>
        Task AccountBannedAsync(int userId, string reason);

        /// <summary>Gửi cho USER khi được UNBAN.</summary>
        Task AccountUnbannedAsync(int userId);
    }
}