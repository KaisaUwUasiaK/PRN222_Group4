using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notifService;

        public NotificationController(AppDbContext db, INotificationService notifService)
        {
            _db = db;
            _notifService = notifService;
        }

        // GET /Notification/List
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(30)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Title,
                    n.Content,
                    n.NotificationType,
                    n.ActionUrl,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { notifications, unreadCount });
        }

        // POST /Notification/MarkRead/{id}
        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notif = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _db.SaveChangesAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { success = true, unreadCount });
        }

        // POST /Notification/MarkAllRead
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return Json(new { success = true, unreadCount = 0 });
        }

        // POST /Notification/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notif = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);
            if (notif == null) return NotFound();

            _db.Notifications.Remove(notif);
            await _db.SaveChangesAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { success = true, unreadCount });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;
            return null;
        }
    }
}