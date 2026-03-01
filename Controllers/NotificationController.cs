using Group4_ReadingComicWeb.Services.Contracts;
using Group4_ReadingComicWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Controllers
{
    [Authorize(Roles = "User,Moderator")]
    [Route("Notifications")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET /Notifications
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetNotificationsForUserAsync(userId.Value);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);

            var vm = new NotificationListViewModel
            {
                UnreadCount = unreadCount,
                Notifications = notifications.Select(n => new NotificationItemViewModel
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList()
            };

            ViewData["Title"] = "Notifications";
            return View(vm);
        }

        // POST /Notifications/{id}/Read
        [ValidateAntiForgeryToken]
        [HttpPost("{id:int}/Read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var updated = await _notificationService.MarkAsReadAsync(userId.Value, id);
            if (updated == null) return NotFound();

            return Json(new { ok = true, notificationId = updated.NotificationId });
        }

        // GET /Notifications/UnreadCount
        [HttpGet("UnreadCount")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetUserId();
            if (userId == null) return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return Json(new { count });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var id))
                return id;
            return null;
        }
    }
}