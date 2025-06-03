using Microsoft.AspNetCore.Mvc;

namespace MovieTheater.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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
