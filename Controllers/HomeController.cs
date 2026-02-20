using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Group4_ReadingComicWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HomeController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


    public async Task<IActionResult> Index()
    {
        var trendingComic = await _context.Comics
            .Include(c => c.ComicTags).ThenInclude(ct => ct.Tag)
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.ViewCount)
            .FirstOrDefaultAsync();

        var newComics = await _context.Comics
            .Where(c => c.Status == ComicStatus.OnWorking.ToString() || c.Status == ComicStatus.Completed.ToString())
            .OrderByDescending(c => c.CreatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.NewComics = newComics;
        return View(trendingComic);
    }


}
}
