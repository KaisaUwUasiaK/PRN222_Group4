using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    /// <summary>
    /// Interface định nghĩa các nghiệp vụ kiểm duyệt truyện (Comic Moderation).
    /// Moderator sử dụng các method này để duyệt, từ chối, ẩn truyện,
    /// xem nội dung chapter, và truy vấn thống kê phục vụ dashboard.
    /// </summary>
    public interface IModerationService
    {
        /// <summary>
        /// Lấy danh sách tất cả truyện đang ở trạng thái "Pending" (chờ duyệt).
        /// Bao gồm thông tin Comic, Author và Moderator (nếu có).
        /// Sắp xếp theo ngày tạo truyện tăng dần (truyện cũ nhất lên trước).
        /// </summary>
        Task<List<ComicModeration>> GetPendingComicsAsync();

        /// <summary>
        /// Lấy chi tiết một bản ghi kiểm duyệt theo ComicModerationId.
        /// Dùng khi moderator click "Review" để xem chi tiết truyện cần duyệt.
        /// Include cả Comic → Author và Moderator.
        /// </summary>
        /// <param name="moderationId">ID của bản ghi ComicModeration.</param>
        /// <returns>ComicModeration nếu tìm thấy, null nếu không tồn tại.</returns>
        Task<ComicModeration?> GetModerationByIdAsync(int moderationId);

        /// <summary>
        /// Lấy bản ghi kiểm duyệt theo ComicId.
        /// Hữu ích khi cần tra cứu trạng thái moderation từ phía Comic.
        /// </summary>
        /// <param name="comicId">ID của truyện trong bảng Comic.</param>
        /// <returns>ComicModeration nếu tìm thấy, null nếu không tồn tại.</returns>
        Task<ComicModeration?> GetModerationByComicIdAsync(int comicId);

        /// <summary>
        /// Lấy toàn bộ lịch sử kiểm duyệt của một truyện cụ thể.
        /// Hiển thị trên trang Review để moderator biết truyện đã từng bị
        /// reject/approve/hide bao nhiêu lần và lý do gì.
        /// Sắp xếp theo ProcessedAt giảm dần (mới nhất lên trước).
        /// </summary>
        /// <param name="comicId">ID của truyện cần xem lịch sử.</param>
        Task<List<ComicModeration>> GetModerationHistoryAsync(int comicId);

        /// <summary>
        /// Lấy chapter theo ID, include Comic (để hiển thị tên truyện trên trang đọc).
        /// Dùng cho moderator xem nội dung ảnh chapter khi review truyện.
        /// </summary>
        /// <param name="chapterId">ChapterId cần lấy.</param>
        /// <returns>Chapter nếu tìm thấy, null nếu không tồn tại.</returns>
        Task<Chapter?> GetChapterByIdAsync(int chapterId);

        /// <summary>
        /// Phê duyệt truyện: cập nhật ComicModeration.ModerationStatus = "Approved"
        /// và đồng bộ Comic.Status = "Approved" để truyện hiển thị trên trang chính.
        /// Ghi nhận moderator nào đã duyệt và thời gian xử lý.
        /// </summary>
        /// <param name="moderationId">ID bản ghi ComicModeration cần approve.</param>
        /// <param name="moderatorId">UserId của moderator thực hiện hành động.</param>
        /// <returns>true nếu thành công, false nếu không tìm thấy bản ghi.</returns>
        Task<bool> ApproveComicAsync(int moderationId, int moderatorId);

        /// <summary>
        /// Từ chối truyện: cập nhật ModerationStatus = "Rejected",
        /// đồng bộ Comic.Status = "Rejected", ghi lý do từ chối vào Note.
        /// Truyện bị reject sẽ không hiển thị trên trang chính.
        /// </summary>
        /// <param name="moderationId">ID bản ghi ComicModeration cần reject.</param>
        /// <param name="moderatorId">UserId của moderator thực hiện hành động.</param>
        /// <param name="reason">Lý do từ chối (bắt buộc).</param>
        /// <returns>true nếu thành công, false nếu không tìm thấy bản ghi.</returns>
        Task<bool> RejectComicAsync(int moderationId, int moderatorId, string reason);

        /// <summary>
        /// Ẩn truyện đã được duyệt trước đó do vi phạm nội dung.
        /// Cập nhật ModerationStatus = "Hidden", đồng bộ Comic.Status = "Hidden",
        /// ghi lý do ẩn vào Note. Dùng khi truyện đã public nhưng phát hiện vi phạm sau.
        /// </summary>
        /// <param name="moderationId">ID bản ghi ComicModeration cần hide.</param>
        /// <param name="moderatorId">UserId của moderator thực hiện hành động.</param>
        /// <param name="reason">Lý do ẩn truyện (bắt buộc).</param>
        /// <returns>true nếu thành công, false nếu không tìm thấy bản ghi.</returns>
        Task<bool> HideComicAsync(int moderationId, int moderatorId, string reason);

        /// <summary>
        /// Bỏ ẩn truyện: cập nhật ModerationStatus = "Approved"
        /// và đồng bộ Comic.Status = "OnWorking" để truyện hiển thị lại.
        /// </summary>
        /// <param name="moderationId">ID bản ghi ComicModeration cần unhide.</param>
        /// <param name="moderatorId">UserId của moderator thực hiện hành động.</param>
        /// <returns>true nếu thành công, false nếu không tìm thấy bản ghi.</returns>
        Task<bool> UnhideComicAsync(int moderationId, int moderatorId);

        /// <summary>
        /// Lấy danh sách truyện phục vụ trang quản lý (Comic Management) kèm phân trang.
        /// Lọc bỏ các truyện đang ở trạng thái Pending.
        /// Hỗ trợ lọc theo uploader (UserId) và trạng thái.
        /// </summary>
        /// <param name="uploaderId">ID của user đăng tải (null nếu lấy tất cả).</param>
        /// <param name="status">Trạng thái cần lọc (null nếu lấy tất cả).</param>
        /// <param name="page">Số trang hiện tại.</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang.</param>
        Task<(List<ComicModeration> Items, int TotalCount)> GetComicsManagementAsync(int? uploaderId, string? status, int page, int pageSize);

        /// <summary>
        /// Lấy danh sách tất cả người dùng đã đăng truyện (Uploaders).
        /// Dùng cho dropdown filter trong trang Comic Management.
        /// </summary>
        Task<List<User>> GetUploadersAsync();

        /// <summary>
        /// Lấy toàn bộ bản ghi kiểm duyệt (mọi trạng thái).
        /// Sắp xếp theo ProcessedAt giảm dần. Dùng cho trang lịch sử tổng hợp.
        /// </summary>
        Task<List<ComicModeration>> GetAllModerationsAsync();

        /// <summary>
        /// Đếm số truyện đang chờ duyệt (status = "Pending").
        /// Hiển thị badge số lượng trên sidebar layout.
        /// </summary>
        Task<int> GetPendingCountAsync();

        /// <summary>
        /// Đếm số truyện đã duyệt trong tháng hiện tại.
        /// Lọc theo cả Month và Year để tránh trùng cross-year.
        /// Dùng cho thống kê dashboard.
        /// </summary>
        Task<int> GetApprovedCountThisMonthAsync();

        /// <summary>
        /// Đếm số truyện bị từ chối trong tháng hiện tại.
        /// Lọc theo cả Month và Year. Dùng cho thống kê dashboard.
        /// </summary>
        Task<int> GetRejectedCountThisMonthAsync();

        /// <summary>
        /// Đếm tổng số truyện đang bị ẩn (mọi thời gian).
        /// Dùng cho thống kê dashboard.
        /// </summary>
        Task<int> GetHiddenCountAsync();
    }
}