using Group4_ReadingComicWeb.Models.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group4_ReadingComicWeb.Models
{
    [Table("Reports")]
    public class Report
    {
        public int ReportId { get; set; }

        // Người report
        public int ReporterId { get; set; }
        public virtual User Reporter { get; set; } = null!;

        // Đối tượng bị report
        public int TargetUserId { get; set; }
        public virtual User TargetUser { get; set; } = null!;

        // Nội dung report
        public string Reason { get; set; } = null!;
        public string? Description { get; set; }
        public ReportType ReportType { get; set; } = ReportType.Comment;

        // Trạng thái xử lý
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        // Xử lý
        public ReportAction? ActionTaken { get; set; }
        public string? ResolutionNote { get; set; }
        public int? ProcessedById { get; set; }
        public virtual User? ProcessedBy { get; set; }

        // Timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}