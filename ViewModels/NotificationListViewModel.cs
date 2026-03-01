namespace Group4_ReadingComicWeb.ViewModels
{
    public class NotificationListViewModel
    {
        public List<NotificationItemViewModel> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}