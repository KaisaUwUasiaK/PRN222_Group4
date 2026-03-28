using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    /// <summary>
    /// Service dành cho Moderator để quản lý tài khoản người dùng thường (role User).
    /// Không áp dụng cho Admin và Moderator.
    /// </summary>
    public interface IModeratorUserService
    {
        /// <summary>
        /// Lấy toàn bộ tài khoản có role User để hiển thị màn hình quản lý.
        /// </summary>
        Task<List<User>> GetAllUsersAsync();

        /// <summary>
        /// Ban tài khoản User.
        /// Trả về user đã cập nhật hoặc null nếu không tìm thấy/không phải role User.
        /// </summary>
        Task<User?> BanUserAsync(int userId);

        /// <summary>
        /// Gỡ ban tài khoản User, đưa trạng thái về Offline.
        /// Trả về user đã cập nhật hoặc null nếu không tìm thấy/không phải role User.
        /// </summary>
        Task<User?> UnbanUserAsync(int userId);
    }
}