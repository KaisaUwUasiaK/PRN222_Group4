using Group4_ReadingComicWeb.Models;

namespace Group4_ReadingComicWeb.Services
{
    public static class LogService
    {
        private static List<SystemLog> _logs = new List<SystemLog>();
        private static int _id = 1;

        public static void AddLog(string username, string action)
        {
            _logs.Add(new SystemLog
            {
                Id = _id++,
                Username = username,
                Action = action,
                CreatedAt = DateTime.Now
            });
        }

        public static List<SystemLog> GetLogs()
        {
            if (_logs.Count == 0)
            {
                GenerateFakeData();
            }

            return _logs.OrderByDescending(x => x.CreatedAt).ToList();
        }

        private static void GenerateFakeData()
        {
            var random = new Random();

            var users = new List<string>
            {
                "admin",
                "guest",
                "john_doe",
                "mangaFan99",
                "comicMaster",
                "readerPro",
                "akira",
                "naruto123"
            };

            var actions = new List<string>
            {
                "Login",
                "Logout",
                "Create Comic",
                "Update Comic",
                "Delete Comic",
                "Read Chapter",
                "Add Comment",
                "Delete Comment",
                "Register Account"
            };

            for (int i = 0; i < 50; i++)
            {
                _logs.Add(new SystemLog
                {
                    Id = _id++,
                    Username = users[random.Next(users.Count)],
                    Action = actions[random.Next(actions.Count)],
                    CreatedAt = DateTime.Now
                        .AddDays(-random.Next(0, 7))
                        .AddMinutes(-random.Next(0, 1440))
                });
            }
        }
    }
}
