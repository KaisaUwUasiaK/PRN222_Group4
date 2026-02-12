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
    }
}