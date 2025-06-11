using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMovieService _movieService;
        private readonly IPromotionService _promotionService;

        public HomeController(ILogger<HomeController> logger, IPromotionService promotionService, IMovieService movieService)
        {
            _logger = logger;
            _promotionService = promotionService;
            _movieService = movieService;
        }

        public IActionResult Index()
        {
            var movies = _movieService.GetAll();
            var promotions = _promotionService.GetAll();

            ViewBag.Movies = movies;
            ViewBag.Promotions = promotions;

            return View();
        }

        public IActionResult MovieList()
        {
            return View();
        }

        public IActionResult Showtime()
        {
            return View();
        }
        
    }
}
