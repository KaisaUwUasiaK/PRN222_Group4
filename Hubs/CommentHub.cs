using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Group4_ReadingComicWeb.Hubs
{
    public class CommentHub : Hub
    {
        // Use for user access Read.cshtm
        public async Task JoinChapter(int chapterId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Chapter_{chapterId}");
        }

        //Use for user access Detail.cshtm
        public async Task JoinComic(int comicId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Comic_{comicId}");
        }
    }
}
