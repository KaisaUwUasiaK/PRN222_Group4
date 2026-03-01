using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services.Contracts
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsForUserAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification?> MarkAsReadAsync(int userId, int notificationId);
        Task<Notification?> CreateAndPushAsync(int userId, string title, string content);
    }
}