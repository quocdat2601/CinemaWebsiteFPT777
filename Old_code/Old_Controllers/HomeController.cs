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

        /// <summary>
        /// [GET] /Home/Index
        /// Trang chủ hiển thị danh sách phim và khuyến mãi hiện có.
        /// </summary>
        public IActionResult Index()
        {
            var movies = _movieService.GetAll();
            var promotions = _promotionService.GetAll();

            ViewBag.Movies = movies;
            ViewBag.Promotions = promotions;

            return View();
        }
        /// <summary>
        /// [GET] /Home/Chat
        /// Trang test chat realtime.
        /// </summary>
        public IActionResult Chat()
        {
            return View();
        }

        /// <summary>
        /// [GET] /Home/MovieList
        /// Trang hiển thị danh sách các bộ phim.
        /// </summary>
        public IActionResult MovieList()
        {
            return View();
        }

        /// <summary>
        /// [GET] /Home/Showtime
        /// Trang hiển thị lịch chiếu phim.
        /// </summary>
        public IActionResult Showtime()
        {
            return View();
        }

    }
}