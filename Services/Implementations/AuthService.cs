using System.Security.Claims;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.Utils;
using Group4_ReadingComicWeb.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public AuthService(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        /// <inheritdoc/>
        public async Task<(User? User, string? ErrorMessage)> LoginAsync(
            string email, string password, bool rememberMe, HttpContext httpContext)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            // Verify credentials — use BCrypt to compare hashed password
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (null, "Invalid email or password.");

            // Prevent banned accounts from logging in
            if (user.Status == AccountStatus.Banned)
                return (user, "banned");

            user.Status = AccountStatus.Online;
            await _context.SaveChangesAsync();

            // Build claims for the authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            // Include avatar URL in claims so layouts can display it without a DB query
            if (!string.IsNullOrEmpty(user.AvatarUrl))
                claims.Add(new Claim("AvatarUrl", user.AvatarUrl));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return (user, null);
        }

        /// <inheritdoc/>
        public async Task<string?> RegisterAsync(RegisterViewModel model)
        {
            var fullname = ValidationRules.NormalizeSpaces(model.Fullname);
            var email = model.Email.Trim();

            // Check if email is already taken
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                return "Email is already in use.";

            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (userRole == null)
                return "Default role 'User' not found.";

            var user = new User
            {
                Username = fullname,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = userRole.RoleId,
                Status = AccountStatus.Offline
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return null; // null = success
        }

        /// <inheritdoc/>
        public async Task LogoutAsync(int userId, HttpContext httpContext)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = AccountStatus.Offline;
                await _context.SaveChangesAsync();
            }

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        /// <inheritdoc/>
        public async Task SendPasswordResetEmailAsync(string email, string resetBaseUrl)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
            if (user == null) return; // Silent — prevent user enumeration

            // Generate a secure one-time token valid for 15 minutes
            var token = Guid.NewGuid().ToString("N");
            user.ResetPasswordToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var link = $"{resetBaseUrl}?token={token}";
            var subject = "Reset Your Password - ComicVerse";
            var body = $@"<p>You requested to reset your password.</p>
<p>Click the link below to reset your password:</p>
<p><a href=""{link}"">Reset Password</a></p>
<p>This link will expire in 15 minutes. If you did not request this, please ignore this email.</p>";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
            catch
            {
                // Swallow email errors — user still gets generic success message
            }
        }

        /// <inheritdoc/>
        public async Task<User?> GetUserByResetTokenAsync(string token)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.ResetPasswordToken == token &&
                u.ResetTokenExpiry.HasValue &&
                u.ResetTokenExpiry > DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await GetUserByResetTokenAsync(token);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
