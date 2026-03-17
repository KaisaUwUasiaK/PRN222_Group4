namespace Group4_ReadingComicWeb.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Action { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
