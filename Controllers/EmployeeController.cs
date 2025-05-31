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
                case "TicketSellingMg":
                    return PartialView("TicketSellingMg");
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
        public async Task<IActionResult> CreateAsync(RegisterViewModel model)
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
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    model.Image = "/image/" + uniqueFileName;
                }
               
                var success = _service.Register(model);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Registration failed - Username already exists";
                    return View(model);
                }

                TempData["ToastMessage"] = "Employee Created Succesfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });

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
                Image = employee.Account.Image,
                Password = null,
                ConfirmPassword = null
            };

            return View(viewModel);
        }

        // POST: EmployeeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(string id, RegisterViewModel model)
        {
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    model.Image = "/image/" + uniqueFileName;
                }
                else
                {
                    var existingEmployee = _service.GetById(id);
                    if (existingEmployee != null)
                    {
                        model.Image = existingEmployee.Account.Image;
                    }
                }

                var success = _service.Update(id, model);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Update failed - Username already exists";
                    return View(model);
                }

                TempData["ToastMessage"] = "Employee Updated Successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error during update: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Delete(string id)
        {
            var employee = _service.GetById(id);
            if (employee == null)
            {
                TempData["ToastMessage"] = "Employee not found.";
                return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ToastMessage"] = "Invalid employee ID.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
                }

                var employee = _service.GetById(id);
                if (employee == null)
                {
                    TempData["ToastMessage"] = "Employee not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
                }

                //movie.Schedules?.Clear();
                //movie.Types?.Clear();
                //movie.ShowDates?.Clear();

                bool success = _service.Delete(id);

                if (!success)
                {
                    TempData["ToastMessage"] = "Failed to delete employee.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
                }

                TempData["ToastMessage"] = "Employee deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
            }
        }
    }
}
