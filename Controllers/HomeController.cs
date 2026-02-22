using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Group4_ReadingComicWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                if (roleClaim == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
            }

            var trendingComic = await _homeService.GetTrendingComicAsync();
            var newComics = await _homeService.GetNewComicsAsync();

            ViewBag.NewComics = newComics;

            return View(trendingComic);
        }
    }
}