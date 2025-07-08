using Microsoft.AspNetCore.Mvc;

namespace MovieTheater.Controllers
{
    public class HomeController1 : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
