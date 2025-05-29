using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.Services;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class AdminController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IEmployeeService _employeeService;
        private readonly IPromotionService _promotionService;
        public AdminController(IMovieService movieService, IEmployeeService employeeService, IPromotionService promotionService)
        {
            _movieService = movieService;
            _employeeService = employeeService;
            _promotionService = promotionService;
        }

        // GET: AdminController
        [RoleAuthorize(new[] { 1 })]
        public IActionResult MainPage(string tab = "Dashboard")
        {
            ViewData["ActiveTab"] = tab;
            return View();
        }


        public IActionResult LoadTab(string tab)
        {
            switch (tab)
            {
                case "Dashboard":
                    return PartialView("Dashboard");
                case "MemberMg":
                    return PartialView("MemberMg");
                case "EmployeeMg":
                    var employees = _employeeService.GetAll();
                    return PartialView("EmployeeMg", employees);
                case "MovieMg":
                    var movies = _movieService.GetAll();
                    return PartialView("MovieMg", movies);
                case "ShowroomMg":
                    return PartialView("ShowroomMg");
                case "ScheduleMg":
                    return PartialView("ScheduleMg");
                case "PromotionMg":
                    var promotions = _promotionService.GetAll();
                    return PartialView("PromotionMg", promotions);
                case "TicketSellingMg":
                    return PartialView("TicketSellingMg");
                default:
                    return Content("Tab not found.");
            }
        }

        // GET: AdminController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AdminController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
