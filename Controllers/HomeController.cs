using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMovieService _movieService;
        private readonly IPromotionService _promotionService;
        private readonly IAccountService _accountService;

        public HomeController(ILogger<HomeController> logger, IPromotionService promotionService, IMovieService movieService, IAccountService accountService)
        {
            _logger = logger;
            _promotionService = promotionService;
            _movieService = movieService;
            _accountService = accountService;
        }

        /// <summary>
        /// [GET] /Home/Index
        /// Trang chủ hiển thị danh sách phim và khuyến mãi hiện có.
        /// </summary>
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && User.IsInRole("Member"))
                {
                    _accountService.CheckAndUpgradeRank(userId);
                }
            }

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

        public IActionResult GradientColorPicker()
        {
            return View("GradientColorPicker");
        }
    }
}