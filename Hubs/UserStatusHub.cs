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

        /// <summary>
        /// Fired when a client establishes a SignalR connection.
        /// Uses a lock to prevent race conditions when the same user opens multiple tabs.
        /// DB status and admin notification are only triggered on the FIRST connection of a user.
        /// </summary>
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

                // Add admin connections to the "admins" group for targeted broadcasts
                if (Context.User?.IsInRole("Admin") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

                // Only update DB and notify admins on the first tab/connection
                if (isFirstConnection)
                {
                    await SetUserStatusInDb(userId.Value, AccountStatus.Online);
                    await Clients.Group("admins").SendAsync("UserOnline", userId.Value);
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Fired when a client disconnects from SignalR.
        /// DB status is only set to Offline when ALL connections (tabs) of the user are closed.
        /// </summary>
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

                // Only update DB and notify admins when the last tab is closed
                if (isLastConnection)
                {
                    await SetUserStatusInDb(userId.Value, AccountStatus.Offline);
                    await Clients.Group("admins").SendAsync("UserOffline", userId.Value);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Updates the user's status in the database.
        /// Skips the update if the user is Banned to prevent overriding the ban status.
        /// Uses a scoped DbContext since Hub instances are transient.
        /// </summary>
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

        /// <summary>
        /// Extracts the authenticated user's ID from the JWT/cookie claims.
        /// Returns null if the claim is missing or cannot be parsed.
        /// </summary>
        private int? GetUserId()
        {
            var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
                return id;
            return null;
        }
    }
}
