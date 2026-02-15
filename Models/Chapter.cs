using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    public class Chapter
    {
        [Key]
        public int ChapterId { get; set; }

        public int ComicId { get; set; }

        [ForeignKey("ComicId")]
        public Comic Comic { get; set; } = null!;

        public int ChapterNumber { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [Column(TypeName = "ntext")]
        public string? Path { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
