using Group4_ReadingComicWeb.Models;
using Group4_ReadingComicWeb.Models.Enum;
using Group4_ReadingComicWeb.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Group4_ReadingComicWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }
        //Get trendiing comic and new comics
        public async Task<IActionResult> Index()
    {
        var trendingComic = await _homeService.GetTrendingComicAsync();

        var newComics = await _homeService.GetNewComicsAsync();

        ViewBag.NewComics = newComics;
        return View(trendingComic);
    }


}
}
