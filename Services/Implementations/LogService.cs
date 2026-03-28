using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Services.Implementations
{
    public class LogService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<LogHub> _hubContext;

        public LogService(AppDbContext context, IHubContext<LogHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public IQueryable<Log> GetLogsQuery()
        {
            return _context.Logs
                .Include(l => l.User)
                .OrderByDescending(x => x.CreatedAt);
        }

        public async Task AddLogAsync(int userId, string action)
        {
            var log = new Log
            {
                UserId = userId,
                Action = action,
                CreatedAt = DateTime.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            var logEntry = await _context.Logs
                .Include(u => u.User)
                .FirstOrDefaultAsync(l => l.LogId == log.LogId);

            // Gửi dữ liệu đầy đủ hơn để Client xử lý màu sắc Badge
            await _hubContext.Clients.All.SendAsync("ReceiveLog", new
            {
                id = logEntry.LogId,
                username = logEntry.User?.Username ?? "System",
                action = logEntry.Action,
                time = logEntry.CreatedAt.ToString("HH:mm:ss"),
                date = logEntry.CreatedAt.ToString("dd MMM, yyyy")
            });
        }
    }
}