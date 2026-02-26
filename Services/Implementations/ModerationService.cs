using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    /// <summary>
    /// Service xử lý toàn bộ nghiệp vụ kiểm duyệt truyện.
    /// Tương tác trực tiếp với bảng ComicModeration và Comic trong database.
    /// Mỗi hành động Approve/Reject/Hide đều đồng bộ trạng thái
    /// giữa ComicModeration.ModerationStatus và Comic.Status.
    /// </summary>
    public class ModerationService : IModerationService
    {
        private readonly AppDbContext _context;

        public ModerationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách truyện đang chờ duyệt.
        /// Include Comic → Author (hiển thị tên tác giả) và Moderator.
        /// Sắp xếp theo CreatedAt tăng dần: truyện submit sớm nhất được review trước.
        /// </summary>
        public async Task<List<ComicModeration>> GetPendingComicsAsync()
        {
            return await _context.ComicModerations
                .Where(cm => cm.ModerationStatus == nameof(ModerationStatus.Pending))
                .Include(cm => cm.Comic)
                    .ThenInclude(c => c.Author)
                .Include(cm => cm.Moderator)
                .OrderBy(cm => cm.Comic.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Tìm bản ghi kiểm duyệt theo ID.
        /// Include Comic → Author, Comic → Chapters để trang Review
        /// hiển thị đầy đủ thông tin truyện và danh sách chapter.
        /// </summary>
        public async Task<ComicModeration?> GetModerationByIdAsync(int moderationId)
        {
            return await _context.ComicModerations
                .Include(cm => cm.Comic)
                    .ThenInclude(c => c.Author)
                .Include(cm => cm.Comic)
                    .ThenInclude(c => c.Chapters)
                .Include(cm => cm.Moderator)
                .FirstOrDefaultAsync(cm => cm.ComicModerationId == moderationId);
        }

        /// <summary>
        /// Tìm bản ghi kiểm duyệt theo ComicId.
        /// Tra cứu ngược từ truyện → trạng thái moderation.
        /// </summary>
        public async Task<ComicModeration?> GetModerationByComicIdAsync(int comicId)
        {
            return await _context.ComicModerations
                .Include(cm => cm.Comic)
                    .ThenInclude(c => c.Author)
                .Include(cm => cm.Comic)
                    .ThenInclude(c => c.Chapters)
                .Include(cm => cm.Moderator)
                .FirstOrDefaultAsync(cm => cm.ComicId == comicId);
        }

        /// <summary>
        /// Lấy lịch sử kiểm duyệt của một truyện.
        /// Chỉ Include Moderator (tên người xử lý), không cần Comic vì đã biết ComicId.
        /// Sắp xếp ProcessedAt giảm dần: hành động gần nhất hiện trước.
        /// </summary>
        public async Task<List<ComicModeration>> GetModerationHistoryAsync(int comicId)
        {
            return await _context.ComicModerations
                .Where(cm => cm.ComicId == comicId)
                .Include(cm => cm.Moderator)
                .OrderByDescending(cm => cm.ProcessedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Phê duyệt truyện.
        /// - Include Comic để đồng bộ Comic.Status = "Approved".
        /// - Ghi nhận ModeratorId và ProcessedAt.
        /// - Sau khi approve, truyện sẽ hiển thị trên trang chính cho người đọc.
        /// </summary>
        public async Task<bool> ApproveComicAsync(int moderationId, int moderatorId)
        {
            var moderation = await _context.ComicModerations
                .Include(cm => cm.Comic)
                .FirstOrDefaultAsync(cm => cm.ComicModerationId == moderationId);

            if (moderation == null)
                return false;

            moderation.ModerationStatus = nameof(ModerationStatus.Approved);
            moderation.ModeratorId = moderatorId;
            moderation.ProcessedAt = DateTime.Now;

            moderation.Comic.Status = nameof(ModerationStatus.Approved);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Từ chối truyện.
        /// - Include Comic để đồng bộ Comic.Status = "Rejected".
        /// - Ghi lý do từ chối vào Note (hiển thị trong lịch sử moderation).
        /// - Truyện bị reject không hiển thị cho người đọc.
        /// </summary>
        public async Task<bool> RejectComicAsync(int moderationId, int moderatorId, string reason)
        {
            var moderation = await _context.ComicModerations
                .Include(cm => cm.Comic)
                .FirstOrDefaultAsync(cm => cm.ComicModerationId == moderationId);

            if (moderation == null)
                return false;

            moderation.ModerationStatus = nameof(ModerationStatus.Rejected);
            moderation.ModeratorId = moderatorId;
            moderation.Note = reason;
            moderation.ProcessedAt = DateTime.Now;

            moderation.Comic.Status = nameof(ModerationStatus.Rejected);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Ẩn truyện vi phạm.
        /// - Dùng khi truyện đã được approve nhưng phát hiện vi phạm sau đó.
        /// - Include Comic để đồng bộ Comic.Status = "Hidden".
        /// - Ghi lý do vi phạm vào Note.
        /// - Truyện bị hide sẽ biến mất khỏi trang chính.
        /// </summary>
        public async Task<bool> HideComicAsync(int moderationId, int moderatorId, string reason)
        {
            var moderation = await _context.ComicModerations
                .Include(cm => cm.Comic)
                .FirstOrDefaultAsync(cm => cm.ComicModerationId == moderationId);

            if (moderation == null)
                return false;

            moderation.ModerationStatus = nameof(ModerationStatus.Hidden);
            moderation.ModeratorId = moderatorId;
            moderation.Note = reason;
            moderation.ProcessedAt = DateTime.Now;

            moderation.Comic.Status = nameof(ModerationStatus.Hidden);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy toàn bộ bản ghi moderation (mọi trạng thái).
        /// Include Comic → Author và Moderator. Sắp xếp theo thời gian xử lý giảm dần.
        /// Phục vụ trang lịch sử tổng hợp (nếu có).
        /// </summary>
        public async Task<List<ComicModeration>> GetAllModerationsAsync()
        {
            return await _context.ComicModerations
                .Include(cm => cm.Comic)
                    .ThenInclude(c => c.Author)
                .Include(cm => cm.Moderator)
                .OrderByDescending(cm => cm.ProcessedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Đếm số truyện đang chờ duyệt.
        /// Query nhẹ (chỉ COUNT), dùng cho badge sidebar — gọi ở mọi trang moderator.
        /// </summary>
        public async Task<int> GetPendingCountAsync()
        {
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == nameof(ModerationStatus.Pending));
        }

        /// <summary>
        /// Đếm số truyện đã approve trong tháng hiện tại.
        /// Lọc theo cả Month + Year để tránh đếm nhầm cross-year.
        /// Dùng cho stat card trên dashboard.
        /// </summary>
        public async Task<int> GetApprovedCountThisMonthAsync()
        {
            var now = DateTime.Now;
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == nameof(ModerationStatus.Approved) &&
                    cm.ProcessedAt.HasValue &&
                    cm.ProcessedAt.Value.Month == now.Month &&
                    cm.ProcessedAt.Value.Year == now.Year);
        }

        /// <summary>
        /// Đếm số truyện bị reject trong tháng hiện tại.
        /// Lọc theo cả Month + Year. Dùng cho stat card trên dashboard.
        /// </summary>
        public async Task<int> GetRejectedCountThisMonthAsync()
        {
            var now = DateTime.Now;
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == nameof(ModerationStatus.Rejected) &&
                    cm.ProcessedAt.HasValue &&
                    cm.ProcessedAt.Value.Month == now.Month &&
                    cm.ProcessedAt.Value.Year == now.Year);
        }

        /// <summary>
        /// Đếm tổng số truyện đang bị ẩn (không giới hạn thời gian).
        /// Dùng cho stat card trên dashboard.
        /// </summary>
        public async Task<int> GetHiddenCountAsync()
        {
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == nameof(ModerationStatus.Hidden));
        }

        /// <summary>
        /// Lấy chapter theo ID, include Comic (để hiển thị tên truyện trên trang đọc).
        /// Dùng cho moderator xem nội dung ảnh chapter khi review truyện.
        /// </summary>
        public async Task<Chapter?> GetChapterByIdAsync(int chapterId)
        {
            return await _context.Chapters
                .Include(ch => ch.Comic)
                .FirstOrDefaultAsync(ch => ch.ChapterId == chapterId);
        }
    }
}