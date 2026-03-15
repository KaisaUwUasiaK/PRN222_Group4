namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface INotificationService
    {
        /// <summary>Gửi thông báo tuỳ chỉnh tới 1 user bất kỳ.</summary>
        Task SendAsync(int userId, string title, string content,
                       string type = "system", string? actionUrl = null);

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

        /// <summary>Gửi cho 1 MODERATOR khi có REPORT MỚI từ user.</summary>
        Task NewReportAsync(int moderatorId, int reportId, string reportedContent);

        /// <summary>Gửi cho USER khi bị CẢNH BÁO bởi Admin/Moderator.</summary>
        Task AccountWarningAsync(int userId, string reason);

        /// <summary>Gửi cho USER khi bị BAN.</summary>
        Task AccountBannedAsync(int userId, string reason);

        /// <summary>Gửi cho USER khi được UNBAN.</summary>
        Task AccountUnbannedAsync(int userId);
    }
}