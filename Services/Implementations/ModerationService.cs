using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using PRN222_Group4.Models;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class ModerationService : IModerationService
    {
        private readonly AppDbContext _context;

        public ModerationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ComicModeration>> GetPendingComicsAsync()
        {
            return await _context.ComicModerations
                .Where(cm => cm.ModerationStatus == "Pending")
                .Include(cm => cm.Comic)
                .Include(cm => cm.Moderator)
                .OrderBy(cm => cm.Comic.CreatedAt)
                .ToListAsync();
        }

        public async Task<ComicModeration?> GetModerationByIdAsync(int moderationId)
        {
            return await _context.ComicModerations
                .Include(cm => cm.Comic)
                .Include(cm => cm.Moderator)
                .FirstOrDefaultAsync(cm => cm.ComicModerationId == moderationId);
        }

        public async Task<ComicModeration?> GetModerationByComicIdAsync(int comicId)
        {
            return await _context.ComicModerations
                .Include(cm => cm.Comic)
                .Include(cm => cm.Moderator)
                .FirstOrDefaultAsync(cm => cm.ComicId == comicId);
        }

        public async Task<List<ComicModeration>> GetModerationHistoryAsync(int comicId)
        {
            return await _context.ComicModerations
                .Where(cm => cm.ComicId == comicId)
                .Include(cm => cm.Moderator)
                .OrderByDescending(cm => cm.ProcessedAt)
                .ToListAsync();
        }

        public async Task<bool> ApproveComicAsync(int moderationId, int moderatorId)
        {
            var moderation = await _context.ComicModerations.FindAsync(moderationId);
            if (moderation == null)
                return false;

            moderation.ModerationStatus = "Approved";
            moderation.ModeratorId = moderatorId;
            moderation.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectComicAsync(int moderationId, int moderatorId, string reason)
        {
            var moderation = await _context.ComicModerations.FindAsync(moderationId);
            if (moderation == null)
                return false;

            moderation.ModerationStatus = "Rejected";
            moderation.ModeratorId = moderatorId;
            moderation.Note = reason;
            moderation.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HideComicAsync(int moderationId, int moderatorId, string reason)
        {
            var moderation = await _context.ComicModerations.FindAsync(moderationId);
            if (moderation == null)
                return false;

            moderation.ModerationStatus = "Hidden";
            moderation.ModeratorId = moderatorId;
            moderation.Note = reason;
            moderation.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ComicModeration>> GetAllModerationsAsync()
        {
            return await _context.ComicModerations
                .Include(cm => cm.Comic)
                .Include(cm => cm.Moderator)
                .OrderByDescending(cm => cm.ProcessedAt)
                .ToListAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == "Pending");
        }

        public async Task<int> GetApprovedCountThisMonthAsync()
        {
            var now = DateTime.Now;
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == "Approved" &&
                    cm.ProcessedAt.HasValue &&
                    cm.ProcessedAt.Value.Month == now.Month &&
                    cm.ProcessedAt.Value.Year == now.Year);
        }

        public async Task<int> GetRejectedCountThisMonthAsync()
        {
            var now = DateTime.Now;
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == "Rejected" &&
                    cm.ProcessedAt.HasValue &&
                    cm.ProcessedAt.Value.Month == now.Month &&
                    cm.ProcessedAt.Value.Year == now.Year);
        }

        public async Task<int> GetHiddenCountAsync()
        {
            return await _context.ComicModerations
                .CountAsync(cm => cm.ModerationStatus == "Hidden");
        }
    }
}