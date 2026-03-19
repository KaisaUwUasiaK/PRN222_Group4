using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        /// <summary>FK tới bảng Users — người nhận thông báo.</summary>
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>Tiêu đề ngắn — hiển thị ở danh sách.</summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Nội dung đầy đủ — hiển thị khi click vào chi tiết.</summary>
        [Column(TypeName = "ntext")]
        public string? Content { get; set; }

        /// <summary>
        /// Phân loại thông báo — dùng để hiển thị icon và màu sắc khác nhau trên UI.
        /// Giá trị: comic_approved | comic_rejected | comic_hidden |
        ///          new_chapter | new_report | new_pending |
        ///          account_warning | account_banned | account_unbanned | system
        /// </summary>
        [StringLength(50)]
        public string NotificationType { get; set; } = "system";

        /// <summary>
        /// URL điều hướng khi nhấn nút "Xem chi tiết" — nullable.
        /// </summary>
        [StringLength(500)]
        public string? ActionUrl { get; set; }

        /// <summary>false = chưa đọc (bold + chấm xanh), true = đã đọc.</summary>
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}