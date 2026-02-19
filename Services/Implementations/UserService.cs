using System.Security.Claims;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Comics)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        /// <inheritdoc/>
        public async Task<string?> UpdateProfileAsync(int userId, string username, string? bio)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return "User not found.";

            var normalizedUsername = ValidationRules.NormalizeSpaces(username);
            var normalizedBio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

            // Check if username is already taken by another user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == normalizedUsername && u.UserId != userId);
            if (existingUser != null)
                return "Username is already taken.";

            user.Username = normalizedUsername;
            user.Bio = normalizedBio;

            await _context.SaveChangesAsync();
            return null; // null = success
        }

        /// <inheritdoc/>
        public async Task<string?> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return "User not found.";

            // Verify current password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return "Current password is incorrect.";

            // Hash and save new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return null; // null = success
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? Error, string? AvatarUrl)> UploadAvatarAsync(
            int userId, IFormFile file, string webRootPath)
        {
            if (file == null || file.Length == 0)
                return (false, "No file uploaded.", null);

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return (false, "Invalid file type. Only JPG, PNG, and GIF are allowed.", null);

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return (false, "File size exceeds 5MB limit.", null);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "User not found.", null);

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(webRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Delete old avatar file if exists
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldAvatarPath = Path.Combine(webRootPath, user.AvatarUrl.TrimStart('/'));
                if (File.Exists(oldAvatarPath))
                    File.Delete(oldAvatarPath);
            }

            // Generate unique filename and save
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update user avatar URL in database
            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            await _context.SaveChangesAsync();

            return (true, null, user.AvatarUrl);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAccountAsync(int userId, string webRootPath)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Delete avatar file if exists
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var avatarPath = Path.Combine(webRootPath, user.AvatarUrl.TrimStart('/'));
                if (File.Exists(avatarPath))
                    File.Delete(avatarPath);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <inheritdoc/>
        public async Task RefreshUserSignInAsync(User user, HttpContext httpContext)
        {
            // Reload user with role to build full claims
            var fullUser = await _context.Users
                .Include(u => u.Role)
                .FirstAsync(u => u.UserId == user.UserId);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, fullUser.UserId.ToString()),
                new Claim(ClaimTypes.Name, fullUser.Username),
                new Claim(ClaimTypes.Email, fullUser.Email),
                new Claim(ClaimTypes.Role, fullUser.Role.RoleName)
            };

            if (!string.IsNullOrEmpty(fullUser.AvatarUrl))
                claims.Add(new Claim("AvatarUrl", fullUser.AvatarUrl));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
        }
    }
}
