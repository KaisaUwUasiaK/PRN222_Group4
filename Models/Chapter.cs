using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Chapter")]
    public class Chapter
    {
        [Key]
        public int ChapterId { get; set; }

        public int ComicId { get; set; }

        [Required]
        public int ChapterNumber { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [Column(TypeName = "ntext")]
        public string Path { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ComicId")]
        public virtual Comic Comic { get; set; } = null!;
    }
}
