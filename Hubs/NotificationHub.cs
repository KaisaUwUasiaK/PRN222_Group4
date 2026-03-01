using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public const string ClientMethodNotificationReceived = "NotificationReceived";

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (userId.HasValue && (role == "User" || role == "Moderator"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
            }

            await base.OnConnectedAsync();
        }

        public static string UserGroup(int userId) => $"user:{userId}";

        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var id))
                return id;
            return null;
        }
    }
}