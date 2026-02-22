namespace Group4_ReadingComicWeb.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
