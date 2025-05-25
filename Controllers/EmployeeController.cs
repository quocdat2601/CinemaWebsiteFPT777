using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.Services;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IMovieService _movieService;

        public EmployeeController(IEmployeeService service, ILogger<AccountController> logger, IMovieService movieService)
        {
            _service = service;
            _logger = logger;
            _movieService = movieService;
        }
        // GET: EmployeeController
        public IActionResult MainPage(string tab = "MovieMg")
        {
            ViewData["ActiveTab"] = tab;
            return View();
        } 

        public IActionResult LoadTab(string tab)
        {
            switch (tab)
            {
                case "MovieMg":
                    var movies = _movieService.GetAll();
                    return PartialView("MovieMg", movies);
                case "ShowroomMg":
                    return PartialView("ShowroomMg");
                case "ScheduleMg":
                    return PartialView("SheduleMg");
                case "PromotionMg":
                    return PartialView("PromotionMg");
                default:
                    return Content("Tab not found.");
            }
        }

        // GET: EmployeeController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: EmployeeController/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new RegisterViewModel(); 
            return View(model);
        }

        // POST: EmployeeController/Create
        [HttpPost]
        public IActionResult Create(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return View(model);
            }

            try
            {
                var success = _service.Register(model);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Registration failed - Username already exists";
                    return View(model);
                }

                //TempData["SuccessMessage"] = "Sign up successful! Please log in.";
                TempData["ToastMessage"] = "Employee Created Succesfully!";
                return RedirectToAction("List","Employee");

                //return RedirectToAction("Login", "Account");

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error during registration: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Inner error: {ex.InnerException.Message}";
                }
                return View(model);
            }
        }

        // GET: EmployeeController/Edit/5
        public IActionResult Edit(string id)
        {
            var employee = _service.GetById(id);
            if (employee == null)
                return NotFound();

            var viewModel = new RegisterViewModel
            {
                Username = employee.Account.Username,
                FullName = employee.Account.FullName,
                DateOfBirth = (DateOnly)employee.Account.DateOfBirth,
                Gender = employee.Account.Gender,
                IdentityCard = employee.Account.IdentityCard,
                Email = employee.Account.Email,
                Address = employee.Account.Address,
                PhoneNumber = employee.Account.PhoneNumber,
                Image = employee.Account.Image
            };

            return View(viewModel);
        }


        // POST: EmployeeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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

        // GET: EmployeeController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: EmployeeController/Delete/5
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
