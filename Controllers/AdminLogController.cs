using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Thêm dòng này để dùng quyền Authorize

namespace Group4_ReadingComicWeb.Controllers
{
    // Thêm dòng này để chỉ cho phép Role là "Admin" truy cập vào toàn bộ Controller này
    [Authorize(Roles = "Admin")]
    public class AdminLogController : Controller
    {
        private readonly AppDbContext _context;

        public AdminLogController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 10;

            // Truy vấn lấy dữ liệu từ bảng Logs, kèm theo thông tin bảng User
            var query = _context.Logs.Include(l => l.User).AsQueryable();

            // Lọc theo từ khóa tìm kiếm (Username hoặc Action)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.User.Username.Contains(search) || x.Action.Contains(search));
            }

            // Lọc theo khoảng ngày
            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedAt <= toDate.Value.AddDays(1).AddTicks(-1));

            // Tính toán phân trang
            int totalItems = await query.CountAsync();
            var pagedLogs = await query.OrderByDescending(x => x.CreatedAt)
                                       .Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToListAsync();

            // Gửi dữ liệu ra View
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(pagedLogs);
        }
    }
}