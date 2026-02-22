using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<List<User>> GetAllModeratorsAsync()
        {
            var moderatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
            if (moderatorRole == null)
                return new List<User>();

            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == moderatorRole.RoleId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<string?> CreateModeratorAsync(CreateModViewModel model)
        {
            // Check duplicate username
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return $"username:{nameof(model.Username)}:Username is already taken.";

            // Check duplicate email
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return $"email:{nameof(model.Email)}:Email is already registered.";

            var moderatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Moderator");
            if (moderatorRole == null)
                return "role:Moderator role not found in database.";

            var newModerator = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = moderatorRole.RoleId,
                Status = AccountStatus.Offline
            };

            _context.Users.Add(newModerator);
            await _context.SaveChangesAsync();

            return null; // null = success
        }

        /// <inheritdoc/>
        public async Task<User?> BanModeratorAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role.RoleName != "Moderator")
                return null;

            user.Status = AccountStatus.Banned;
            await _context.SaveChangesAsync();

            return user;
        }

        /// <inheritdoc/>
        public async Task<User?> UnbanModeratorAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role.RoleName != "Moderator")
                return null;

            user.Status = AccountStatus.Offline;
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
