using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using System.Security.Claims;
using MovieTheater.Models; // Added for Movie model
using System.Linq;
using MovieTheater.Repository; // Added for ToList()

namespace MovieTheater.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IPromotionService _promotionService;
        private readonly IAccountService _accountService;
        private readonly IPersonRepository _personRepository;

        public HomeController(IPromotionService promotionService, IMovieService movieService, IAccountService accountService, IPersonRepository personRepository)
        {
            _promotionService = promotionService;
            _movieService = movieService;
            _accountService = accountService;
            _personRepository = personRepository;
        }

        /// <summary>
        /// [GET] /Home/Index
        /// Trang chủ hiển thị danh sách phim và khuyến mãi hiện có.
        /// </summary>
        public IActionResult Index() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && User.IsInRole("Member"))
                {
                    _accountService.CheckAndUpgradeRank(userId);
                }
            }

            // Get categorized movies
            var currentlyShowingMovies = _movieService.GetCurrentlyShowingMoviesWithDetails()?
                .ToList() ?? new List<Movie>();
            var comingSoonMovies = _movieService.GetComingSoonMoviesWithDetails().ToList();
            var promotions = _promotionService.GetAll();
            var people = _personRepository.GetAll().ToList();
            var movies = _movieService.GetAll().ToList();

            // Use first currently showing movie as active movie, fallback to coming soon
            Movie? activeMovie = currentlyShowingMovies.FirstOrDefault() ?? comingSoonMovies.FirstOrDefault();
            
            ViewBag.People = people;
            ViewBag.Movies = movies; // Use currently showing movies for hero section
            ViewBag.CurrentlyShowingMovies = currentlyShowingMovies; // For "Now Showing" slide
            ViewBag.ComingSoonMovies = comingSoonMovies; // For "Upcoming Movies" slide
            ViewBag.Promotions = promotions;

            return View(activeMovie);
        }

        /// <summary>
        /// [GET] /Home/MovieList
        /// Trang hiển thị danh sách các bộ phim.
        /// </summary>
        public IActionResult MovieList() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            return View();
        }

        /// <summary>
        /// [GET] /Home/Showtime
        /// Trang hiển thị lịch chiếu phim.
        /// </summary>
        public IActionResult Showtime() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            return View();
        }

        public IActionResult GradientColorPicker() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            return View("GradientColorPicker");
        }
    }
}