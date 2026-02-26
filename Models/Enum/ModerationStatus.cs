namespace Group4_ReadingComicWeb.Models.Enum
{
    /// <summary>
    /// Trạng thái kiểm duyệt truyện trong hệ thống moderation.
    /// Dùng bởi ComicModeration.ModerationStatus (lưu dạng string qua nameof).
    /// </summary>
    public enum ModerationStatus
    {
        /// <summary>Chờ duyệt — truyện mới submit, chưa được moderator xử lý.</summary>
        Pending,

        /// <summary>Đã phê duyệt — truyện hiển thị trên trang chính cho người đọc.</summary>
        Approved,

        /// <summary>Từ chối — truyện không đạt yêu cầu, không hiển thị.</summary>
        Rejected,

        /// <summary>Ẩn — truyện đã duyệt nhưng bị phát hiện vi phạm sau đó.</summary>
        Hidden
    }
}