using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    /// <summary>
    /// Entity bản ghi kiểm duyệt truyện.
    /// Mỗi Comic khi được submit sẽ tạo 1 record ComicModeration với status Pending.
    /// Moderator xử lý → cập nhật ModerationStatus, ghi ModeratorId, Note, ProcessedAt.
    /// Quan hệ: ComicModeration → Comic (1-1), ComicModeration → User/Moderator (N-1).
    /// </summary>
    [Table("ComicModeration")]
    public class ComicModeration
    {
        [Key]
        public int ComicModerationId { get; set; }

        /// <summary>FK tới bảng Comic — truyện cần kiểm duyệt.</summary>
        [Required]
        public int ComicId { get; set; }

        [ForeignKey("ComicId")]
        public virtual Comic Comic { get; set; } = null!;

        /// <summary>
        /// Trạng thái kiểm duyệt: Pending | Approved | Rejected | Hidden.
        /// Lưu dạng string, sử dụng nameof(ModerationStatus.XXX) để gán giá trị.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ModerationStatus { get; set; } = "Pending";

        /// <summary>FK tới bảng Users — moderator đã xử lý (null nếu chưa xử lý).</summary>
        public int? ModeratorId { get; set; }

        [ForeignKey("ModeratorId")]
        public virtual User? Moderator { get; set; }

        /// <summary>Ghi chú / lý do khi reject hoặc hide (null nếu approve).</summary>
        [Column(TypeName = "ntext")]
        public string? Note { get; set; }

        /// <summary>Thời điểm moderator xử lý (null nếu chưa xử lý).</summary>
        public DateTime? ProcessedAt { get; set; }
    }
}