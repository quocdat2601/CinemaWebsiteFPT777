using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Linq;
namespace MovieTheater.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IMovieService _movieService;
        private readonly IMemberRepository _memberRepository;
        private readonly IAccountService _accountService;
        private readonly IInvoiceService _invoiceService;
        public EmployeeController(IEmployeeService service, ILogger<AccountController> logger, IMovieService movieService, IMemberRepository memberRepository, IAccountService accountService, IInvoiceService invoiceService)
        {
            _service = service;
            _logger = logger;
            _movieService = movieService;
            _memberRepository = memberRepository;
            _accountService = accountService;
            _invoiceService = invoiceService;
        }
        // GET: EmployeeController
        public IActionResult MainPage(string tab = "MemberMg")
        {
            ViewData["ActiveTab"] = tab;
            return View();
        } 

        [Authorize(Roles = "Employee")]
        public IActionResult MemberList()
        {
            var members = _memberRepository.GetAll();
            return PartialView("MemberMg", members);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult LoadTab(string tab, string keyword = null)
        {
            switch (tab)
            {
                case "MovieMg":
                    var movies = _movieService.GetAll();
                    return PartialView("MovieMg", movies);
                case "MemberMg":
                    var members = _memberRepository.GetAll();
                    return PartialView("~/Views/Admin/MemberMg.cshtml", members);
                case "ShowroomMg":
                    return PartialView("ShowroomMg");
                case "ScheduleMg":
                    return PartialView("SheduleMg");
                case "PromotionMg":
                    return PartialView("PromotionMg");
                case "BookingMg":
                    var invoices = _invoiceService.GetAll();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        keyword = keyword.Trim().ToLower();
                        invoices = invoices.Where(i =>
                            (i.InvoiceId != null && i.InvoiceId.ToLower().Contains(keyword)) ||
                            (i.AccountId != null && i.AccountId.ToLower().Contains(keyword)) ||
                            (i.Account != null && (
                                (i.Account.PhoneNumber != null && i.Account.PhoneNumber.ToLower().Contains(keyword)) ||
                                (i.Account.IdentityCard != null && i.Account.IdentityCard.ToLower().Contains(keyword))
                            ))
                        ).ToList();
                    }

                    return PartialView("BookingMg", invoices);
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

            var viewModel = new EmployeeEditViewModel
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
                AccountId = employee.AccountId
            };

            return View(viewModel);
        }

        // POST: EmployeeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(string id, EmployeeEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = _service.GetById(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (model.Password != model.ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Password and Confirm Password do not match";
                    return View(model);
                }

                if (model.Password != employee.Account.Password)
                {
                    var result = _accountService.UpdatePasswordByUsername(model.Username, model.Password);
                    if (!result)
                    {
                        TempData["ErrorMessage"] = "Failed to update password";
                        return View(model);
                    }
                }
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

                var registerModel = new RegisterViewModel
                {
                    AccountId = model.AccountId,
                    Username = model.Username,
                    Password = model.Password,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    IdentityCard = model.IdentityCard,
                    Email = model.Email,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    Image = model.Image,
                    ImageFile = model.ImageFile
                };

                var success = _service.Update(id, registerModel);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Update failed - Username already exists";
                    return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
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
