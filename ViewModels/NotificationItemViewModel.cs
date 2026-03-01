namespace Group4_ReadingComicWeb.ViewModels
{
    public class NotificationItemViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}