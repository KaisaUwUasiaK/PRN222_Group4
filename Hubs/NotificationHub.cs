using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Group4_ReadingComicWeb.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                // Mỗi user join group riêng — server sẽ push đúng group này
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;
            return null;
        }
    }
}