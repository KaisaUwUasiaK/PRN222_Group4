using Microsoft.AspNetCore.SignalR;

namespace Group4_ReadingComicWeb.Hubs
{
    public class ComicHub : Hub
    {
        public async Task JoinModeratorGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "ModeratorGroup");
        }

        public async Task LeaveModeratorGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "ModeratorGroup");
        }
    }
}
