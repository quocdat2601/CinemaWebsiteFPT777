using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly ICinemaService _cinemaService;
        private readonly ISeatTypeService _seatTypeService;

        public AdminController(IMovieService movieService, IEmployeeService employeeService, IPromotionService promotionService, ICinemaService cinemaService, ISeatTypeService seatTypeService)
        {
            _movieService = movieService;
            _employeeService = employeeService;
            _promotionService = promotionService;
            _cinemaService = cinemaService;
            _seatTypeService = seatTypeService;
        }

        // GET: AdminController
        [Authorize(Roles = "1")]
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
                    var cinema = _cinemaService.GetAll();
                    var seatTypes = _seatTypeService.GetAll();

                    ViewBag.SeatTypes = seatTypes;
                    return PartialView("ShowroomMg", cinema);
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
