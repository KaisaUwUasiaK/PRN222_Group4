using Group4_ReadingComicWeb.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Comic")]
    public class Comic
    {
        [Key]
        public int ComicId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Column(TypeName = "ntext")]
        public string Description { get; set; } = null!;

        public string CoverImage { get; set; } = null!;

        public int ViewCount { get; set; }
        public int AuthorId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AuthorId")]
        public virtual User Author { get; set; } = null!;

        public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

        public virtual ICollection<ComicTag> ComicTags { get; set; } = new List<ComicTag>();
    }
}
