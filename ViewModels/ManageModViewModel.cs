using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.ViewModels
{
    public class ManageModViewModel
    {
        // Stats
        public int TotalModerators { get; set; }
        public int ActiveModerators { get; set; }
        public int BannedModerators { get; set; }

        public List<ModeratorDetailViewModel> Moderators { get; set; } = new List<ModeratorDetailViewModel>();
    }

    public class ModeratorDetailViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;  // Hiển thị hash
        public DateTime CreatedAt { get; set; }
        public AccountStatus Status { get; set; }

        public string StatusText
        {
            get
            {
                return Status switch
                {
                    AccountStatus.Online => "Online",
                    AccountStatus.Offline => "Offline",
                    AccountStatus.Banned => "Banned",
                    _ => "Unknown"
                };
            }
        }
    }
}
