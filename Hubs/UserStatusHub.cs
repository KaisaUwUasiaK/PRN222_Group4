using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;

namespace Group4_ReadingComicWeb.Hubs
{
    [Authorize]
    public class UserStatusHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Track connections per userId to handle multiple tabs
        // userId -> set of connectionIds
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();
        private static readonly object _lock = new();

        public UserStatusHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                bool isFirstConnection = false;
                lock (_lock)
                {
                    if (!_userConnections.ContainsKey(userId.Value))
                    {
                        _userConnections[userId.Value] = new HashSet<string>();
                        isFirstConnection = true;
                    }
                    _userConnections[userId.Value].Add(Context.ConnectionId);
                }

                // Join admin group to receive status updates
                if (Context.User?.IsInRole("Admin") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

                // Only on first connection: update DB + notify admins
                if (isFirstConnection)
                {
                    await SetUserStatusInDb(userId.Value, AccountStatus.Online);
                    await Clients.Group("admins").SendAsync("UserOnline", userId.Value);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                bool isLastConnection = false;
                lock (_lock)
                {
                    if (_userConnections.TryGetValue(userId.Value, out var connections))
                    {
                        connections.Remove(Context.ConnectionId);
                        if (connections.Count == 0)
                        {
                            _userConnections.TryRemove(userId.Value, out _);
                            isLastConnection = true;
                        }
                    }
                }

                // Only when ALL tabs are closed: update DB + notify admins
                if (isLastConnection)
                {
                    await SetUserStatusInDb(userId.Value, AccountStatus.Offline);
                    await Clients.Group("admins").SendAsync("UserOffline", userId.Value);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task SetUserStatusInDb(int userId, AccountStatus status)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(userId);
            if (user != null && user.Status != AccountStatus.Banned)
            {
                user.Status = status;
                await db.SaveChangesAsync();
            }
        }

        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;
            return null;
        }
    }
}
