using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    public class Log
    {
        [Key]
        public int LogId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Action { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
