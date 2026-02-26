using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Comment")]
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public int ChapterId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "ntext")]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ChapterId")]
        public virtual Chapter Chapter { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}