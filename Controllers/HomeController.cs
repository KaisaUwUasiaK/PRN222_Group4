using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Group4_ReadingComicWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IFavoriteService _favoriteService;

        public HomeController(IHomeService homeService, IFavoriteService favoriteService)
        {
            _homeService = homeService;
            _favoriteService = favoriteService;
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
            
            bool isFavorited = false;
            if (trendingComic != null && User.Identity?.IsAuthenticated == true)
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int userId))
                {
                    isFavorited = await _favoriteService.IsFavoritedAsync(trendingComic.ComicId, userId);
                }
            }
            ViewBag.IsFavorited = isFavorited;

            var newComics = await _homeService.GetNewComicsAsync();
            var trendingComics = await _homeService.GetTrendingComicsAsync();
            var maybeYouLikeComics = await _homeService.GetMaybeYouLikeComicsAsync();

            ViewBag.NewComics = newComics;
            ViewBag.TrendingComics = trendingComics;
            ViewBag.MaybeYouLikeComics = maybeYouLikeComics;

            return View(trendingComic);
        }
    }
}
