using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("ComicTag")]
    public class ComicTag
    {
        [Key, Column(Order = 0)]
        public int ComicId { get; set; }

        [Key, Column(Order = 1)]
        public int TagId { get; set; }

        [ForeignKey("ComicId")]
        public virtual Comic Comic { get; set; }

        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }
    }
}
