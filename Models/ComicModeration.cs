using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PRN222_Group4.Models;

namespace Group4_ReadingComicWeb.Models
{
    [Table("ComicModeration")]
    public class ComicModeration
    {
        [Key]
        public int ComicModerationId { get; set; }

        [Required]
        public int ComicId { get; set; }

        [ForeignKey("ComicId")]
        public virtual Comic Comic { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string ModerationStatus { get; set; } = "Pending";
        // Pending | Approved | Rejected | Hidden

        public int? ModeratorId { get; set; }

        [ForeignKey("ModeratorId")]
        public virtual User? Moderator { get; set; }

        [Column(TypeName = "ntext")]
        public string? Note { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }

    // Enum để biểu diễn trạng thái kiểm duyệt
    public enum ModerationStatus
    {
        Pending,    // Chờ duyệt
        Approved,   // Đã phê duyệt
        Rejected,   // Từ chối
        Hidden      // Ẩn (vi phạm sau khi đã duyệt)
    }
}