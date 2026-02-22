using Group4_ReadingComicWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group4_ReadingComicWeb.Controllers
{
    public class ComicController : Controller
    {
        private readonly AppDbContext _context;

        public ComicController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var allComics = await _context.Comics.Where(c => c.Status == "OnWorking" && c.Status == "Completed").ToListAsync();
            return View(allComics);
        }
        public async Task<IActionResult> Detail(int id)
        {
            var comic = await _context.Comics
                .Include(c => c.Chapters.OrderBy(ch => ch.ChapterNumber)) 
                .Include(c => c.Author)
                .FirstOrDefaultAsync(m => m.ComicId == id);

            if (comic == null) return NotFound();
            return View(comic);
        }

        public async Task<IActionResult> Read(int id)
        {
            var chapter = await _context.Chapters
                .Include(ch => ch.Comic)
                .FirstOrDefaultAsync(ch => ch.ChapterId == id);

            if (chapter == null) return NotFound();

            var prev = await _context.Chapters
                .Where(c => c.ComicId == chapter.ComicId && c.ChapterNumber < chapter.ChapterNumber)
                .OrderByDescending(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            var next = await _context.Chapters
                .Where(c => c.ComicId == chapter.ComicId && c.ChapterNumber > chapter.ChapterNumber)
                .OrderBy(c => c.ChapterNumber)
                .FirstOrDefaultAsync();

            ViewBag.PrevChapterId = prev?.ChapterId; 
            ViewBag.NextChapterId = next?.ChapterId;

            return View(chapter);
        }
    }
}
