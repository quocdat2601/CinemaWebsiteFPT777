using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Data;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    //ADmin
    public class AdminController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IEmployeeService _employeeService;
        private readonly IPromotionService _promotionService;
        private readonly ICinemaService _cinemaService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly IMemberRepository _memberRepository;
        private readonly IAccountService _accountService;
        private readonly IInvoiceService _invoiceService;
        private readonly ISeatService _seatService;
        private readonly IFoodService _foodService;
        private readonly IVoucherService _voucherService;
        private readonly IRankService _rankService;
        private readonly IVersionRepository _versionRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IDashboardService _dashboardService;
        public AdminController(
            IMovieService movieService,
            IEmployeeService employeeService,
            IPromotionService promotionService,
            ICinemaService cinemaService,
            ISeatTypeService seatTypeService,
            IMemberRepository memberRepository,
            IAccountService accountService,
            ISeatService seatService,
            IInvoiceService invoiceService,
            IFoodService foodService, IPersonRepository personRepository,
            IVoucherService voucherService,
            IRankService rankService, IVersionRepository versionRepository,
            IDashboardService dashboardService)
        {
            _movieService = movieService;
            _employeeService = employeeService;
            _promotionService = promotionService;
            _cinemaService = cinemaService;
            _seatTypeService = seatTypeService;
            _memberRepository = memberRepository;
            _accountService = accountService;
            _invoiceService = invoiceService;
            _seatService = seatService;
            _voucherService = voucherService;
            _foodService = foodService;
            _rankService = rankService;
            _versionRepository = versionRepository;
            _personRepository = personRepository;
            _dashboardService = dashboardService;
        }

        // GET: AdminController
        [Authorize(Roles = "Admin")]
        public IActionResult MainPage(string tab = "Dashboard", string range = "weekly")
        {
            ViewData["ActiveTab"] = tab;
            ViewData["DashboardRange"] = range;
            int days = range == "monthly" ? 30 : 7;
            var model = _dashboardService.GetDashboardViewModel(days);
            return View(model);
        }

        public async Task<IActionResult> LoadTab(string tab, string keyword = null, string statusFilter = null, string range = "weekly", string bookingTypeFilter = null)
        {
            switch (tab)
            {
                case "Dashboard":
                    int days = range == "monthly" ? 30 : 7;
                    var dashModel = _dashboardService.GetDashboardViewModel(days);
                    ViewData["DashboardRange"] = range;
                    return PartialView("Dashboard", dashModel);
                case "MemberMg":
                    var members = _memberRepository.GetAll();
                    return PartialView("MemberMg", members);
                case "EmployeeMg":
                    var employees = _employeeService.GetAll();

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        ViewBag.Keyword = keyword;
                        keyword = keyword.Trim().ToLower();

                        employees = employees.Where(e =>
                            (!string.IsNullOrEmpty(e.Account?.FullName) && e.Account.FullName.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(e.Account?.IdentityCard) && e.Account.IdentityCard.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(e.Account?.Email) && e.Account.Email.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(e.Account?.PhoneNumber) && e.Account.PhoneNumber.ToLower().Contains(keyword)) ||
                            (!string.IsNullOrEmpty(e.Account?.Address) && e.Account.Address.ToLower().Contains(keyword))
                        ).ToList();
                    }

                    return PartialView("EmployeeMg", employees);
                case "MovieMg":
                    var movies = _movieService.GetAll();
                    return PartialView("MovieMg", movies);
                case "ShowroomMg":
                    var allCinemaRooms = _cinemaService.GetAll();
                    var versions = _movieService.GetAllVersions();

                    ViewBag.Versions = versions;
                    ViewBag.ActiveRooms = allCinemaRooms.Where(c => c.StatusId == 1).ToList();
                    ViewBag.HiddenRooms = allCinemaRooms.Where(c => c.StatusId == 3).ToList();

                    return PartialView("ShowroomMg");
                case "VersionMg":
                    var seatTypes = _seatTypeService.GetAll();
                    ViewBag.SeatTypes = seatTypes;
                    var versionMg = _versionRepository.GetAll();
                    var seatTypesForVersion = _seatTypeService.GetAll();
                    ViewBag.SeatTypes = seatTypesForVersion;
                    return PartialView("VersionMg", versionMg);
                case "PromotionMg":
                    var promotions = _promotionService.GetAll();
                    return PartialView("PromotionMg", promotions);
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
                    bool? foodStatusFilter = null;
                    if (!string.IsNullOrEmpty(statusFilterStr))
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
                    ViewBag.StatusFilter = statusFilterStr;

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
                case "RankMg":
                    var ranks = _rankService.GetAllRanks();
                    return PartialView("RankMg", ranks);
                case "CastMg":
                    var persons = _personRepository.GetAll();
                    ViewBag.Persons = persons;
                    ViewBag.Actors = persons.Where(c => c.IsDirector == false).ToList();
                    ViewBag.Directors = persons.Where(c => c.IsDirector == true).ToList();
                    return PartialView("CastMg");
                case "QRCode":
                    return PartialView("~/Views/QRCode/Scanner.cshtml");
                default:
                    return Content("Tab not found.");
            }
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

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            var account = _accountService.GetById(id); // Use AccountService to get the Account by AccountId
            if (account == null)
            {
                return NotFound(); // Or redirect to an error page
            }

            var viewModel = new RegisterViewModel
            {
                AccountId = account.AccountId,
                Username = account.Username,
                FullName = account.FullName,
                DateOfBirth = account.DateOfBirth,
                Gender = account.Gender,
                IdentityCard = account.IdentityCard,
                Email = account.Email,
                Address = account.Address,
                PhoneNumber = account.PhoneNumber,
                Image = account.Image,
                // Password and ConfirmPassword are not populated for security reasons
                Password = null,
                ConfirmPassword = null
            };

            return View("EditMember", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, RegisterViewModel model)
        {
            if (id != model.AccountId)
            {
                return BadRequest(); // Ensure the ID in the route matches the model
            }

            // Remove password fields from model state validation if they are not being updated
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("ImageFile");

            if (!ModelState.IsValid)
            {
                // If validation fails, return the view with the model to display errors
                return View("EditMember", model);
            }

            try
            {
                var success = _accountService.Update(model.AccountId, model);

                if (!success)
                {
                    ModelState.AddModelError("", "Error updating member.");
                    return View("EditMember", model);
                }

                // Redirect back to the member list on success
                TempData["ToastMessage"] = "Member updated successfully!"; // Optional success message
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MemberMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MemberMg" });
            }
            catch (Exception)
            {
                // Log the exception (optional)
                // _logger.LogError(ex, "Error updating member with id {MemberId}", id);

                ModelState.AddModelError("", "An unexpected error occurred while updating the member.");
                return View("EditMember", model);
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult ShowtimeMg(string date)
        {
            DateOnly selectedDate;
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParseExact(date, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
            {
                // parsed successfully
            }
            else
            {
                selectedDate = DateOnly.FromDateTime(DateTime.Today);
            }

            var allMovieShows = _movieService.GetMovieShow();
            var filteredMovieShows = allMovieShows.Where(ms => ms.ShowDate == selectedDate).ToList();

            // Get summary for the month
            var repo = HttpContext.RequestServices.GetService(typeof(IMovieRepository)) as IMovieRepository;
            var summary = new Dictionary<DateOnly, List<string>>();
            if (repo is MovieRepository concreteRepo)
            {
                summary = concreteRepo.GetMovieShowSummaryByMonth(selectedDate.Year, selectedDate.Month);
            }
            ViewBag.MovieShowSummaryByDate = summary;

            var showtimeModel = new ShowtimeManagementViewModel
            {
                SelectedDate = selectedDate,
                AvailableSchedules = _movieService.GetAllSchedules(),
                MovieShows = filteredMovieShows
            };
            return View(showtimeModel);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult GetMovieShowSummary(int year, int month)
        {
            if (HttpContext.RequestServices.GetService(typeof(IMovieRepository)) is not MovieRepository repo)
            {
                return Json(new Dictionary<string, List<string>>());
            }

            var summary = repo.GetMovieShowSummaryByMonth(year, month);

            var jsonFriendlySummary = summary.ToDictionary(
                kvp => kvp.Key.ToString("yyyy-MM-dd"),
                kvp => kvp.Value
            );

            return Json(jsonFriendlySummary);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateRank()
        {
            return View("~/Views/Rank/Create.cshtml", new RankCreateViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRank(RankCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors below.";
                return View("~/Views/Rank/Create.cshtml", model);
            }
            try
            {
                // Map RankCreateViewModel to RankInfoViewModel for service
                var infoModel = new RankInfoViewModel
                {
                    CurrentRankName = model.CurrentRankName,
                    RequiredPointsForCurrentRank = model.RequiredPointsForCurrentRank,
                    CurrentDiscountPercentage = model.CurrentDiscountPercentage ?? 0,
                    CurrentPointEarningPercentage = model.CurrentPointEarningPercentage ?? 0,
                    ColorGradient = model.ColorGradient,
                    IconClass = model.IconClass
                };
                var result = _rankService.Create(infoModel);
                TempData["ToastMessage"] = "Rank created successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "RankMg" });

            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View("~/Views/Rank/Create.cshtml", model);
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult EditRank(int id)
        {
            var rank = _rankService.GetById(id);
            if (rank == null) return NotFound();
            // Map RankInfoViewModel to RankCreateViewModel
            var editModel = new RankCreateViewModel
            {
                CurrentRankName = rank.CurrentRankName,
                RequiredPointsForCurrentRank = rank.RequiredPointsForCurrentRank,
                CurrentDiscountPercentage = rank.CurrentDiscountPercentage,
                CurrentPointEarningPercentage = rank.CurrentPointEarningPercentage,
                ColorGradient = rank.ColorGradient,
                IconClass = rank.IconClass
            };
            ViewBag.RankId = rank.CurrentRankId;
            return View("~/Views/Rank/Edit.cshtml", editModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult EditRank(int id, RankCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RankId = id;
                TempData["ErrorMessage"] = "Please correct the errors below.";
                return View("~/Views/Rank/Edit.cshtml", model);
            }
            try
            {
                // Map RankCreateViewModel to RankInfoViewModel for service
                var infoModel = new RankInfoViewModel
                {
                    CurrentRankId = id,
                    CurrentRankName = model.CurrentRankName,
                    RequiredPointsForCurrentRank = model.RequiredPointsForCurrentRank,
                    CurrentDiscountPercentage = model.CurrentDiscountPercentage ?? 0,
                    CurrentPointEarningPercentage = model.CurrentPointEarningPercentage ?? 0,
                    ColorGradient = model.ColorGradient,
                    IconClass = model.IconClass
                };
                var result = _rankService.Update(infoModel);
                TempData["ToastMessage"] = "Rank updated successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "RankMg" });
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.RankId = id;
                TempData["ErrorMessage"] = ex.Message;
                return View("~/Views/Rank/Edit.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRank(int id)
        {
            _rankService.Delete(id);
            TempData["ToastMessage"] = "Rank deleted successfully!";
            return RedirectToAction("MainPage", "Admin", new { tab = "RankMg" });
        }
    }
}
