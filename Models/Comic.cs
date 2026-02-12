using Group4_ReadingComicWeb.Models.Enum;
using PRN222_Group4.Models;
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
        public string Title { get; set; }

        [Column(TypeName = "ntext")]
        public string Description { get; set; }

        public string CoverImage { get; set; }

        public int AuthorId { get; set; } // FK tới User

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

      
        public virtual ICollection<Chapter> Chapters { get; set; }

        public virtual ICollection<ComicTag> ComicTags { get; set; }

       
    }
}
