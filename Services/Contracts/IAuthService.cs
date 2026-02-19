using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IAuthService
    {
        /// <summary>
        /// Validates credentials and signs the user in via cookie authentication.
        /// Returns the authenticated User on success, null on failure.
        /// </summary>
        Task<(User? User, string? ErrorMessage)> LoginAsync(string email, string password, bool rememberMe, HttpContext httpContext);

        /// <summary>
        /// Registers a new user account.
        /// Returns null on success, or an error message string on failure.
        /// </summary>
        Task<string?> RegisterAsync(RegisterViewModel model);

        /// <summary>
        /// Signs the user out and sets their status to Offline.
        /// </summary>
        Task LogoutAsync(int userId, HttpContext httpContext);

        /// <summary>
        /// Generates a password reset token and sends a reset email.
        /// </summary>
        Task SendPasswordResetEmailAsync(string email, string resetBaseUrl);

        /// <summary>
        /// Validates a reset token and returns the associated user, or null if invalid/expired.
        /// </summary>
        Task<User?> GetUserByResetTokenAsync(string token);

        /// <summary>
        /// Resets the user's password using the provided token.
        /// Returns true on success, false if token is invalid/expired.
        /// </summary>
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
