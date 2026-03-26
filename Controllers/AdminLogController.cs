using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Controllers
{
    public class AdminLogController : Controller
    {
        private readonly LogService _logService;
        private readonly AppDbContext _context;

        // SỬA TẠI ĐÂY: Thêm AppDbContext vào tham số truyền vào
        public AdminLogController(LogService logService, AppDbContext context)
        {
            _logService = logService;
            _context = context;
        }

        public async Task<IActionResult> Index(string search, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 10;
            var query = _logService.GetLogsQuery();

            // 1. Tìm kiếm & Lọc ngày (Giữ nguyên logic tốt của bạn)
            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.User.Username.Contains(search) || x.Action.Contains(search));

            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));

            // 2. Thực hiện lấy dữ liệu
            int totalItems = await query.CountAsync();
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 3. Truyền dữ liệu bổ trợ ra View
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;

            return View(logs);
        }
    }
}