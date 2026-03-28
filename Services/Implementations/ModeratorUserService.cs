using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    /// <summary>
    /// Triển khai nghiệp vụ quản lý trạng thái tài khoản User cho Moderator.
    /// </summary>
    public class ModeratorUserService : IModeratorUserService
    {
        private readonly AppDbContext _context;

        public ModeratorUserService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "User")
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<User?> BanUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role.RoleName != "User")
            {
                return null;
            }

            user.Status = AccountStatus.Banned;
            await _context.SaveChangesAsync();
            return user;
        }

        /// <inheritdoc/>
        public async Task<User?> UnbanUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role.RoleName != "User")
            {
                return null;
            }

            user.Status = AccountStatus.Offline;
            await _context.SaveChangesAsync();
            return user;
        }
    }
}