using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Group4_ReadingComicWeb.Controllers
{
    public class ComicController : Controller
    {
        private readonly IComicService _comicService;

        public ComicController(IComicService comicService)
        {
            _comicService = comicService;
        }

        // List all public comics
        public async Task<IActionResult> Index()
        {
            var allComics = await _comicService.GetPublicComicsAsync();
            return View(allComics);
        }

        // View comic detail
        public async Task<IActionResult> Detail(int id)
        {
            var comic = await _comicService.GetComicDetailAsync(id);

            if (comic == null) return NotFound();

            return View(comic);
        }

        // Read chapter in comic
        public async Task<IActionResult> Read(int id)
        {
            var result = await _comicService.GetChapterForReadingAsync(id);

            if (result.CurrentChapter == null) return NotFound();

            ViewBag.PrevChapterId = result.PrevChapterId;
            ViewBag.NextChapterId = result.NextChapterId;

            return View(result.CurrentChapter);
        }
    }
}