using System;
using System.Collections.Generic;
using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.ViewModels
{
    public class DashboardViewModel
    {
        // Stats
        public int TotalComics { get; set; }
        public long TotalViews { get; set; }
        public int TotalUsers { get; set; }

        // Moderators
        public int TotalModerators { get; set; }
        public int ActiveModerators { get; set; }
        public int BannedModerators { get; set; }

        public List<LogViewModel> RecentLogs { get; set; } = new List<LogViewModel>();
    }

    public class LogViewModel
    {
        public int LogId { get; set; }
        public string AdminUsername { get; set; } = null!;
        public string Action { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}