using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Notification")]
    public class Notification
    {
        public int NotificationId { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [MaxLength(255)]
        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}