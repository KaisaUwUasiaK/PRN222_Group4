using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Tag")]
    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        [StringLength(100)]
        public string TagName { get; set; } = null!;

        public virtual ICollection<ComicTag> ComicTags { get; set; } = new List<ComicTag>();
    }
}
