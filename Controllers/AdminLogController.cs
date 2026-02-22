using Microsoft.AspNetCore.Mvc;
using Group4_ReadingComicWeb.Services;
using System;

namespace Group4_ReadingComicWeb.Controllers
{
    public class AdminLogController : Controller
    {
        public IActionResult Index(string search, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 5;

            var logs = LogService.GetLogs();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                logs = logs
                    .Where(x => x.Username.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || x.Action.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Date filter
            if (fromDate.HasValue)
                logs = logs.Where(x => x.CreatedAt >= fromDate.Value).ToList();

            if (toDate.HasValue)
                logs = logs.Where(x => x.CreatedAt <= toDate.Value).ToList();

            // Paging
            int totalItems = logs.Count();
            var pagedLogs = logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(pagedLogs);
        }
    }
}