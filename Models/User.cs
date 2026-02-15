using System.Collections.Generic;

namespace Group4_ReadingComicWeb.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public AccountStatus Status { get; set; } = AccountStatus.Offline;

        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }

        public ICollection<Comic> Comics { get; set; } = new List<Comic>();
        public ICollection<Log> Logs { get; set; } = new List<Log>();
    }

    // Enum trạng thái tài khoản
    public enum AccountStatus
    {
        Online,    // Đang hoạt động
        Offline,   // Ngoại tuyến
        Suspended, // Bị tạm ngừng
        Banned     // Bị cấm
    }
}