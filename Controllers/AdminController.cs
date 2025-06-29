using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Controllers
{
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
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IBookingService _bookingService;
        private readonly ISeatService _seatService;
        private readonly IScheduleSeatRepository _scheduleSeatRepository;
        private readonly IFoodService _foodService;
        private readonly IVoucherService _voucherService;
        private readonly IRankService _rankService;

        public AdminController(
            IMovieService movieService,
            IEmployeeService employeeService,
            IPromotionService promotionService,
            ICinemaService cinemaService,
            ISeatTypeService seatTypeService,
            IMemberRepository memberRepository,
            IAccountService accountService,
            IBookingService bookingService,
            ISeatService seatService,
            IInvoiceService invoiceService,
            IScheduleRepository scheduleRepository,
            IScheduleSeatRepository scheduleSeatRepository,
            IFoodService foodService,
            IVoucherService voucherService,
            IRankService rankService)
        {
            _movieService = movieService;
            _employeeService = employeeService;
            _promotionService = promotionService;
            _cinemaService = cinemaService;
            _seatTypeService = seatTypeService;
            _memberRepository = memberRepository;
            _accountService = accountService;
            _invoiceService = invoiceService;
            _scheduleRepository = scheduleRepository;
            _bookingService = bookingService;
            _seatService = seatService;
            _scheduleSeatRepository = scheduleSeatRepository;
            _voucherService = voucherService;
            _foodService = foodService;
            _rankService = rankService;
        }

        // GET: AdminController
        [Authorize(Roles = "Admin")]
        public IActionResult MainPage(string tab = "Dashboard")
        {
            ViewData["ActiveTab"] = tab;
            var model = GetDashboardViewModel();
            return View(model);
        }

        public async Task<IActionResult> LoadTab(string tab, string keyword = null)
        {
            switch (tab)
            {
                case "Dashboard":
                    var dashModel = GetDashboardViewModel();
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
                    var cinema = _cinemaService.GetAll();
                    var seatTypes = _seatTypeService.GetAll();

                    ViewBag.SeatTypes = seatTypes;
                    return PartialView("ShowroomMg", cinema);
                case "ScheduleMg":
                    var scheduleMovies = _movieService.GetAll();
                    return PartialView("ScheduleMg", scheduleMovies);
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

                    return PartialView("BookingMg", invoices);
                case "ShowtimeMg":
                    var showtimeModel = new ShowtimeManagementViewModel
                    {
                        AvailableDates = _scheduleRepository.GetAllShowDates(),
                        SelectedDate = DateTime.Today,
                        AvailableSchedules = _movieService.GetAllSchedules(),
                        MovieShows = _movieService.GetMovieShow()
                    };
                    return PartialView("ShowtimeMg", showtimeModel);
                case "FoodMg":
                    // Sử dụng parameter keyword thay vì Request.Query["keyword"]
                    var searchKeyword = keyword ?? string.Empty;
                    var categoryFilter = Request.Query["categoryFilter"].ToString();
                    var statusFilterStr = Request.Query["statusFilter"].ToString();
                    bool? statusFilter = null;
                    if (!string.IsNullOrEmpty(statusFilterStr))
                        statusFilter = bool.Parse(statusFilterStr);

                    var foods = await _foodService.GetAllAsync(searchKeyword, categoryFilter, statusFilter);

                    ViewBag.Keyword = searchKeyword;
                    ViewBag.CategoryFilter = categoryFilter;
                    ViewBag.StatusFilter = statusFilter;

                    return PartialView("FoodMg", foods);
                case "VoucherMg":
                    var vouchers = _voucherService.GetAll();
                    return PartialView("VoucherMg", vouchers);
                case "RankMg":
                    var ranks = _rankService.GetAllRanks();
                    return PartialView("RankMg", ranks);
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
                return RedirectToAction("MainPage", "Admin", new { tab = "MemberMg" });
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                // _logger.LogError(ex, "Error updating member with id {MemberId}", id);

                ModelState.AddModelError("", "An unexpected error occurred while updating the member.");
                return View("EditMember", model);
            }
        }

        private AdminDashboardViewModel GetDashboardViewModel()
        {
            var today = DateTime.Today;
            var allInvoices = _invoiceService.GetAll().ToList();

            // Only "completed" and "cancelled"
            var completed = allInvoices.Where(i => i.Status == InvoiceStatus.Completed).ToList();
            var cancelled = allInvoices.Where(i => i.Status == InvoiceStatus.Incomplete).ToList();

            var todayInv = completed.Where(i => i.BookingDate?.Date == today).ToList();
            var todayCancelled = cancelled.Where(i => i.BookingDate?.Date == today).ToList();

            // 1) Today's summary
            var revenueToday = todayInv.Sum(i => i.TotalMoney ?? 0m);
            var bookingsToday = todayInv.Count;
            var ticketsSoldToday = todayInv.Sum(i => i.Seat?.Split(',').Length ?? 0);

            // 2) Occupancy 
            var allSeats = _seatService.GetAllSeatsAsync().Result;
            var totalSeats = allSeats.Count;
            var occupancyRate = totalSeats > 0
                ? Math.Round((decimal)ticketsSoldToday / totalSeats * 100, 1)
                : 0m;

            // 3) 7‑day trends
            var last7 = Enumerable.Range(0, 7)
                           .Select(i => today.AddDays(-i))
                           .Reverse()
                           .ToList();
            var revTrend = last7
                .Select(d => allInvoices
                    .Where(inv => inv.BookingDate?.Date == d && inv.Status == InvoiceStatus.Completed)
                    .Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();
            var bookTrend = last7
                .Select(d => allInvoices
                    .Count(inv => inv.BookingDate?.Date == d && inv.Status == InvoiceStatus.Completed))
                .ToList();

            // 4) Top 5 movies & members
            var topMovies = completed
                .GroupBy(i => i.MovieName)
                .OrderByDescending(g => g.Sum(inv => inv.Seat?.Split(',').Length ?? 0))
                .Take(5)
                .Select(g => (MovieName: g.Key, TicketsSold: g.Sum(inv => inv.Seat?.Split(',').Length ?? 0)))
                .ToList();

            var topMembers = completed
                .Where(i => i.Account != null && i.Account.RoleId == 3)
                .GroupBy(i => i.Account.FullName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => (MemberName: g.Key, Bookings: g.Count()))
                .ToList();

            // 5) Recent bookings 
            var recentBookings = completed
                .OrderByDescending(i => i.BookingDate)
                .Take(10)
                .Select(i => new RecentBookingInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieName,
                    BookingDate = i.BookingDate ?? DateTime.MinValue,
                    Status = "Completed"
                })
                .ToList();

            // 6) Recent members
            var recentMembers = _memberRepository.GetAll()
                .Where(m => m.Account?.RegisterDate != null)
                .OrderByDescending(m => m.Account!.RegisterDate)
                .Take(5)
                .Select(m => new RecentMemberInfo
                {
                    MemberId = m.MemberId,
                    FullName = m.Account!.FullName ?? "N/A",
                    Email = m.Account.Email ?? "N/A",
                    PhoneNumber = m.Account.PhoneNumber ?? "N/A",
                    JoinDate = m.Account.RegisterDate
                })
                .ToList();

            return new AdminDashboardViewModel
            {
                RevenueToday = revenueToday,
                BookingsToday = bookingsToday,
                TicketsSoldToday = ticketsSoldToday,
                OccupancyRateToday = occupancyRate,
                RevenueTrendDates = last7,
                RevenueTrendValues = revTrend,
                BookingTrendDates = last7,
                BookingTrendValues = bookTrend,
                TopMovies = topMovies,
                TopMembers = topMembers,
                RecentBookings = recentBookings,
                RecentMembers = recentMembers
            };
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
            if (result)
            {
                TempData["ToastMessage"] = "Rank created successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "RankMg" });
            }
            else
            {
                TempData["ErrorMessage"] = "A rank with the same required points already exists. Please choose a different value.";
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
            if (result)
            {
                TempData["ToastMessage"] = "Rank updated successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "RankMg" });
            }
            else
            {
                ViewBag.RankId = id;
                TempData["ErrorMessage"] = "A rank with the same required points already exists. Please choose a different value.";
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
