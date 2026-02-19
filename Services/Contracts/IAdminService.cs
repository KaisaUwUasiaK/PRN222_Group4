using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.ViewModels;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface IAdminService
    {
        /// <summary>
        /// Returns all users with the Moderator role.
        /// </summary>
        Task<List<User>> GetAllModeratorsAsync();

        /// <summary>
        /// Creates a new Moderator account.
        /// Returns null on success, or an error message on failure.
        /// </summary>
        Task<string?> CreateModeratorAsync(CreateModViewModel model);

        /// <summary>
        /// Bans a Moderator account.
        /// Returns the banned User on success, or null if not found / not a Moderator.
        /// </summary>
        Task<User?> BanModeratorAsync(int userId);

        /// <summary>
        /// Unbans a Moderator account, restoring their status to Offline.
        /// Returns the unbanned User on success, or null if not found / not a Moderator.
        /// </summary>
        Task<User?> UnbanModeratorAsync(int userId);
    }
}
