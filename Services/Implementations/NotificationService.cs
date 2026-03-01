using Group4_ReadingComicWeb.Hubs;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<Notification?> MarkAsReadAsync(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null) return null;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return notification;
        }

        public async Task<Notification?> CreateAndPushAsync(int userId, string title, string content)
        {
            // Chỉ tạo notification cho User và Moderator, không tạo cho Admin
            var roleName = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Role.RoleName)
                .FirstOrDefaultAsync();

            if (roleName != "User" && roleName != "Moderator")
                return null;

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push real-time qua SignalR
            await _hubContext.Clients
                .Group(NotificationHub.UserGroup(userId))
                .SendAsync(NotificationHub.ClientMethodNotificationReceived, new
                {
                    notification.NotificationId,
                    notification.Title,
                    notification.Content,
                    notification.IsRead,
                    CreatedAt = notification.CreatedAt
                });

            return notification;
        }
    }
}