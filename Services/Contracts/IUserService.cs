using Group4_ReadingComicWeb.Models;
using Microsoft.AspNetCore.Http;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IUserService
    {
        /// <summary>
        /// Gets a user by ID, including their Role.
        /// Returns null if not found.
        /// </summary>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Updates the user's username and bio.
        /// Returns null on success, or an error message on failure.
        /// </summary>
        Task<string?> UpdateProfileAsync(int userId, string username, string? bio);

        /// <summary>
        /// Verifies the current password and updates to the new password.
        /// Returns null on success, or an error message on failure.
        /// </summary>
        Task<string?> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Validates, saves the uploaded avatar file, deletes the old one, and updates the user record.
        /// Returns (success, errorMessage, newAvatarUrl).
        /// </summary>
        Task<(bool Success, string? Error, string? AvatarUrl)> UploadAvatarAsync(int userId, IFormFile file, string webRootPath);

        /// <summary>
        /// Deletes the user's avatar file (if any) and removes the user from the database.
        /// Returns true on success, false if user not found.
        /// </summary>
        Task<bool> DeleteAccountAsync(int userId, string webRootPath);

        /// <summary>
        /// Re-issues the authentication cookie so that updated claims (username, avatar, etc.)
        /// are reflected immediately without requiring a new login.
        /// </summary>
        Task RefreshUserSignInAsync(User user, HttpContext httpContext);
    }
}
