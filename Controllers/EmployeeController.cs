using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Data;
namespace MovieTheater.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _service;
        private readonly IMovieService _movieService;
        private readonly IMemberRepository _memberRepository;
        private readonly IAccountService _accountService;
        private readonly IInvoiceService _invoiceService;
        private readonly ICinemaService _cinemaService;
        private readonly IPromotionService _promotionService;
        private readonly IFoodService _foodService;
        private readonly IVoucherService _voucherService;
        private readonly IPersonRepository _personRepository;
        public EmployeeController(IEmployeeService service, IMovieService movieService, IMemberRepository memberRepository, IAccountService accountService, IInvoiceService invoiceService, ICinemaService cinemaService, IPromotionService promotionService, IFoodService foodService, IVoucherService voucherService, IPersonRepository personRepository)
        {
            _service = service;
            _movieService = movieService;
            _memberRepository = memberRepository;
            _accountService = accountService;
            _invoiceService = invoiceService;
            _cinemaService = cinemaService;
            _promotionService = promotionService;
            _foodService = foodService;
            _voucherService = voucherService;
            _personRepository = personRepository;
        }
        // GET: EmployeeController
        [Authorize(Roles = "Employee")]
        public IActionResult MainPage(string tab = "MovieMg")
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
        public async Task<IActionResult> LoadTab(string tab, string keyword = null, string statusFilter = null, string range = "weekly", string bookingTypeFilter = null)
        {
            switch (tab)
            {
                case "MovieMg":
                    var movies = _movieService.GetAll();
                    return PartialView("MovieMg", movies);
                case "BookingMg":
                    var invoices = _invoiceService.GetAll();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        ViewBag.Keyword = keyword;
                        keyword = keyword.Trim().ToLower();

                        invoices = invoices.Where(i =>
                            (!string.IsNullOrEmpty(i.InvoiceId) && i.InvoiceId.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(i.AccountId) && i.AccountId.ToLower().Contains(keyword)) ||
                            (i.Account != null && (
                                (!string.IsNullOrEmpty(i.Account.PhoneNumber) && i.Account.PhoneNumber.ToLower().Contains(keyword)) ||
                                (!string.IsNullOrEmpty(i.Account.IdentityCard) && i.Account.IdentityCard.ToLower().Contains(keyword))
                            ))
                        ).ToList();
                    }

                    // Bổ sung filter trạng thái
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        if (statusFilter == "completed")
                            invoices = invoices.Where(b => b.Status == InvoiceStatus.Completed && !b.Cancel).ToList();
                        else if (statusFilter == "cancelled")
                            invoices = invoices.Where(b => b.Status == InvoiceStatus.Completed && b.Cancel).ToList();
                        else if (statusFilter == "notpaid")
                            invoices = invoices.Where(b => b.Status != InvoiceStatus.Completed).ToList();
                    }

                    // Bổ sung filter booking type (all vs normal vs employee)
                    if (!string.IsNullOrEmpty(bookingTypeFilter))
                    {
                        if (bookingTypeFilter == "normal")
                            invoices = invoices.Where(i => i.EmployeeId == null).ToList();
                        else if (bookingTypeFilter == "employee")
                            invoices = invoices.Where(i => i.EmployeeId != null).ToList();
                        // If bookingTypeFilter is "all" or any other value, don't filter (show all)
                    }

                    // Set the current booking type filter for the view
                    ViewBag.CurrentBookingTypeFilter = bookingTypeFilter ?? "all";

                    // Bổ sung sort
                    var sortBy = Request.Query["sortBy"].ToString();
                    if (!string.IsNullOrEmpty(sortBy))
                    {
                        if (sortBy == "movie_az")
                            invoices = invoices.OrderBy(i => i.MovieShow.Movie.MovieNameEnglish).ToList();
                        else if (sortBy == "movie_za")
                            invoices = invoices.OrderByDescending(i => i.MovieShow.Movie.MovieNameEnglish).ToList();
                        else if (sortBy == "id_asc")
                            invoices = invoices.OrderBy(i => i.InvoiceId).ToList();
                        else if (sortBy == "id_desc")
                            invoices = invoices.OrderByDescending(i => i.InvoiceId).ToList();
                        else if (sortBy == "account_az")
                            invoices = invoices.OrderBy(i => i.AccountId).ToList();
                        else if (sortBy == "account_za")
                            invoices = invoices.OrderByDescending(i => i.AccountId).ToList();
                        else if (sortBy == "identity_az")
                            invoices = invoices.OrderBy(i => i.Account != null ? i.Account.IdentityCard : "").ToList();
                        else if (sortBy == "identity_za")
                            invoices = invoices.OrderByDescending(i => i.Account != null ? i.Account.IdentityCard : "").ToList();
                        else if (sortBy == "phone_az")
                            invoices = invoices.OrderBy(i => i.Account != null ? i.Account.PhoneNumber : "").ToList();
                        else if (sortBy == "phone_za")
                            invoices = invoices.OrderByDescending(i => i.Account != null ? i.Account.PhoneNumber : "").ToList();
                        else if (sortBy == "time_asc")
                            invoices = invoices.OrderBy(i => i.MovieShow.Schedule.ScheduleTime).ToList();
                        else if (sortBy == "time_desc")
                            invoices = invoices.OrderByDescending(i => i.MovieShow.Schedule.ScheduleTime).ToList();
                    }

                    return PartialView("BookingMg", invoices);
                case "FoodMg":
                    // Sử dụng parameter keyword thay vì Request.Query["keyword"]
                    var searchKeyword = keyword ?? string.Empty;
                    var categoryFilter = Request.Query["categoryFilter"].ToString();
                    string statusFilterStr = Request.Query["statusFilter"].ToString();
                    bool? foodStatusFilter = true; // Default to Active
                    if (!string.IsNullOrEmpty(statusFilterStr) && statusFilterStr != "")
                    {
                        if (bool.TryParse(statusFilterStr, out var parsedBool))
                            foodStatusFilter = parsedBool;
                        else if (statusFilterStr == "1")
                            foodStatusFilter = true;
                        else if (statusFilterStr == "0")
                            foodStatusFilter = false;
                    }

                    var foods = await _foodService.GetAllAsync(searchKeyword, categoryFilter, foodStatusFilter);

                    // Bổ sung sort
                    var sortByFood = Request.Query["sortBy"].ToString();
                    if (!string.IsNullOrEmpty(sortByFood))
                    {
                        if (sortByFood == "name_az")
                            foods.Foods = foods.Foods.OrderBy(f => f.Name).ToList();
                        else if (sortByFood == "name_za")
                            foods.Foods = foods.Foods.OrderByDescending(f => f.Name).ToList();
                        else if (sortByFood == "category_az")
                            foods.Foods = foods.Foods.OrderBy(f => f.Category).ToList();
                        else if (sortByFood == "category_za")
                            foods.Foods = foods.Foods.OrderByDescending(f => f.Category).ToList();
                        else if (sortByFood == "price_asc")
                            foods.Foods = foods.Foods.OrderBy(f => f.Price).ToList();
                        else if (sortByFood == "price_desc")
                            foods.Foods = foods.Foods.OrderByDescending(f => f.Price).ToList();
                        else if (sortByFood == "created_asc")
                            foods.Foods = foods.Foods.OrderBy(f => f.CreatedDate).ToList();
                        else if (sortByFood == "created_desc")
                            foods.Foods = foods.Foods.OrderByDescending(f => f.CreatedDate).ToList();
                    }

                    ViewBag.Keyword = searchKeyword;
                    ViewBag.CategoryFilter = categoryFilter;
                    ViewBag.StatusFilter = string.IsNullOrEmpty(statusFilterStr) ? "true" : statusFilterStr;

                    return PartialView("FoodMg", foods);
                case "VoucherMg":
                    var filter = new Service.VoucherFilterModel
                    {
                        Keyword = Request.Query["keyword"].ToString(),
                        StatusFilter = Request.Query["statusFilter"].ToString(),
                        ExpiryFilter = Request.Query["expiryFilter"].ToString()
                    };
                    var filteredVouchers = _voucherService.GetFilteredVouchers(filter).ToList();

                    // Bổ sung sort
                    var sortByVoucher = Request.Query["sortBy"].ToString();
                    if (!string.IsNullOrEmpty(sortByVoucher))
                    {
                        if (sortByVoucher == "voucherid_asc")
                            filteredVouchers = filteredVouchers.OrderBy(v => v.VoucherId).ToList();
                        else if (sortByVoucher == "voucherid_desc")
                            filteredVouchers = filteredVouchers.OrderByDescending(v => v.VoucherId).ToList();
                        else if (sortByVoucher == "account_az")
                            filteredVouchers = filteredVouchers.OrderBy(v => v.AccountId).ToList();
                        else if (sortByVoucher == "account_za")
                            filteredVouchers = filteredVouchers.OrderByDescending(v => v.AccountId).ToList();
                        else if (sortByVoucher == "value_asc")
                            filteredVouchers = filteredVouchers.OrderBy(v => v.Value).ToList();
                        else if (sortByVoucher == "value_desc")
                            filteredVouchers = filteredVouchers.OrderByDescending(v => v.Value).ToList();
                        else if (sortByVoucher == "created_asc")
                            filteredVouchers = filteredVouchers.OrderBy(v => v.CreatedDate).ToList();
                        else if (sortByVoucher == "created_desc")
                            filteredVouchers = filteredVouchers.OrderByDescending(v => v.CreatedDate).ToList();
                        else if (sortByVoucher == "expiry_asc")
                            filteredVouchers = filteredVouchers.OrderBy(v => v.ExpiryDate).ToList();
                        else if (sortByVoucher == "expiry_desc")
                            filteredVouchers = filteredVouchers.OrderByDescending(v => v.ExpiryDate).ToList();
                    }

                    ViewBag.Keyword = filter.Keyword;
                    ViewBag.StatusFilter = filter.StatusFilter;
                    ViewBag.ExpiryFilter = filter.ExpiryFilter;
                    return PartialView("VoucherMg", filteredVouchers);
                
                case "CastMg":
                    var persons = _personRepository.GetAll();
                    ViewBag.Persons = persons;
                    ViewBag.Actors = persons.Where(c => c.IsDirector == false).ToList();
                    ViewBag.Directors = persons.Where(c => c.IsDirector == true).ToList();
                    return PartialView("CastMg");
                case "QRCode":
                    return PartialView("QRCode");
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
                AccountId = employee.AccountId,
                Status = employee.Status
            };

            return View(viewModel);
        }

        // POST: EmployeeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAsync(string id, EmployeeEditViewModel model)
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
            if (employee.Status != model.Status)
            {
                _service.ToggleStatus(employee.EmployeeId);
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
                        model.ImageFile.CopyToAsync(stream);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult ToggleStatus(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid employee ID.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
                }

                var employee = _service.GetById(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
                }

                _service.ToggleStatus(id);
                TempData["ToastMessage"] = "Employee status updated successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            }
            return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
        }
    }
}
