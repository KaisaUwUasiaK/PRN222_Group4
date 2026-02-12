namespace PRN222_Group4.Models
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

        // Lương bổ sung các thuộc tính liên quan đến xử lý trạng thái người dùng (ban, hoạt động, v.v.) nếu cần thiết
        public UserStatus Status { get; set; } = UserStatus.Active;
        public DateTime? SuspendedUntil { get; set; }
        public string? BanReason { get; set; }

    }

    // Enum để biểu diễn trạng thái người dùng
    public enum UserStatus
    {
        Active, // Hoạt động
        Suspended, // Án Treo (kiểu giống thẻ vàng trong bóng đá)
        Banned // Bị cấm (kiểu giống thẻ đỏ trong bóng đá)
    }
}