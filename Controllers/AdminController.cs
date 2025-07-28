using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.EntityFrameworkCore;

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
        private readonly MovieTheaterContext _context;

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
            IFoodService foodService,
            IVoucherService voucherService,
            IRankService rankService, IVersionRepository versionRepository,
            MovieTheaterContext context)
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
            _context = context;
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
                    var versions = _movieService.GetAllVersions();
                    ViewBag.Versions = versions;
                    return PartialView("ShowroomMg", cinema);
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

                    return PartialView("BookingMg", invoices);
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
                    var filter = new Service.VoucherFilterModel
                    {
                        Keyword = Request.Query["keyword"].ToString(),
                        StatusFilter = Request.Query["statusFilter"].ToString(),
                        ExpiryFilter = Request.Query["expiryFilter"].ToString()
                    };
                    var filteredVouchers = _voucherService.GetFilteredVouchers(filter);
                    ViewBag.Keyword = filter.Keyword;
                    ViewBag.StatusFilter = filter.StatusFilter;
                    ViewBag.ExpiryFilter = filter.ExpiryFilter;
                    return PartialView("VoucherMg", filteredVouchers);
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
            catch (Exception)
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
            var allInvoices = _invoiceService.GetAll().Where(i => i.Status == InvoiceStatus.Completed).ToList();
            var grossRevenue = allInvoices.Where(i => !i.Cancel).Sum(i => i.TotalMoney ?? 0m);
            var totalVouchersIssued = allInvoices.Where(i => i.Cancel).Sum(i => i.TotalMoney ?? 0m);
            var totalBookings = allInvoices.Where(i => !i.Cancel).Count();

            var todayInv = allInvoices.Where(i => i.BookingDate?.Date == today).ToList();

            // 1) Today's summary
            var revenueToday = todayInv.Where(i => !i.Cancel).Sum(i => i.TotalMoney ?? 0m);
            var bookingsToday = todayInv.Where(i => !i.Cancel).Count();
            var ticketsSoldToday = todayInv.Where(i => !i.Cancel).Sum(i => i.Seat?.Split(',').Length ?? 0);
            var vouchersToday = todayInv.Where(i => i.Cancel).Sum(i => i.TotalMoney ?? 0m);
            var netRevenue = grossRevenue - totalVouchersIssued;

            // 2) Occupancy 
            var allSeats = _seatService.GetAllSeatsAsync().Result;
            var totalSeats = allSeats.Count;
            var occupancyRate = totalSeats > 0
                ? Math.Round((decimal)ticketsSoldToday / totalSeats * 100, 1)
                : 0m;

            // 3) 7-day trends
            var last7 = Enumerable.Range(0, 7)
                           .Select(i => today.AddDays(-i))
                           .Reverse()
                           .ToList();
            var revTrend = last7
                .Select(d => allInvoices
                    .Where(inv => inv.BookingDate?.Date == d && !inv.Cancel)
                    .Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();
            var bookTrend = last7
                .Select(d => allInvoices
                    .Where(inv => inv.BookingDate?.Date == d && !inv.Cancel)
                    .Count())
                .ToList();
            var voucherTrend = last7
                .Select(d => allInvoices
                    .Where(inv => inv.BookingDate?.Date == d && inv.Cancel)
                    .Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();

            // 4) Top 5 movies & members
            var topMovies = allInvoices
                .Where(i => !i.Cancel)
                .GroupBy(i => i.MovieShow.Movie.MovieNameEnglish)
                .OrderByDescending(g => g.Sum(inv => inv.Seat?.Split(',').Length ?? 0))
                .Take(5)
                .Select(g => (MovieName: g.Key, TicketsSold: g.Sum(inv => inv.Seat?.Split(',').Length ?? 0)))
                .ToList();

            var topMembers = allInvoices
                .Where(i => i.Account != null && i.Account.RoleId == 3 && !i.Cancel)
                .GroupBy(i => i.Account.FullName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => (MemberName: g.Key, Bookings: g.Count()))
                .ToList();

            // 5) Recent bookings 
            var recentBookings = allInvoices
                .Where(i => !i.Cancel)
                .OrderByDescending(i => i.BookingDate)
                .Take(10)
                .Select(i => new RecentBookingInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
                    BookingDate = i.BookingDate ?? DateTime.MinValue,
                    Status = "Completed"
                })
                .ToList();

            var recentMovieBookings = allInvoices
                .Where(i => !i.Cancel)
                .OrderByDescending(i => i.BookingDate)
                .Take(10)
                .Select(i => new RecentMovieActivityInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
                    ActivityDate = i.BookingDate ?? DateTime.MinValue,
                    TotalAmount = i.TotalMoney ?? 0m
                })
                .ToList();

            var recentMovieCancellations = allInvoices
                .Where(i => i.Cancel)
                .OrderByDescending(i => i.CancelDate)
                .Take(10)
                .Select(i => new RecentMovieActivityInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
                    ActivityDate = i.CancelDate ?? DateTime.MinValue,
                    TotalAmount = i.TotalMoney ?? 0m
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

            // --- Food Analytics ---
            var foodInvoices = _context.FoodInvoices
                .Include(fi => fi.Food)
                .Include(fi => fi.Invoice)
                .ToList();

            // Only completed, not cancelled invoices for most stats
            var validFoodInvoices = foodInvoices.Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && !fi.Invoice.Cancel).ToList();

            // Recent food orders (from completed, not cancelled invoices)
            var recentFoodOrders = validFoodInvoices
                .OrderByDescending(fi => fi.Invoice.BookingDate)
                .Take(10)
                .Select(fi => new RecentFoodOrder
                {
                    Date = fi.Invoice.BookingDate ?? DateTime.MinValue,
                    FoodName = fi.Food.Name,
                    Quantity = fi.Quantity,
                    Price = fi.Price,
                    OrderTotal = fi.Price * fi.Quantity
                })
                .ToList();

            // Recent food cancels (from cancelled invoices)
            var recentFoodCancels = foodInvoices
                .Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && fi.Invoice.Cancel)
                .OrderByDescending(fi => fi.Invoice.CancelDate)
                .Take(10)
                .Select(fi => new RecentFoodOrder
                {
                    Date = fi.Invoice.CancelDate ?? fi.Invoice.BookingDate ?? DateTime.MinValue,
                    FoodName = fi.Food.Name,
                    Quantity = fi.Quantity,
                    Price = fi.Price,
                    OrderTotal = fi.Price * fi.Quantity
                })
                .ToList();

            // 1. FoodRevenue: total revenue from food
            var foodRevenue = validFoodInvoices.Sum(fi => fi.Price * fi.Quantity);

            // 2. Orders: number of unique invoices with food
            var foodOrders = validFoodInvoices.Select(fi => fi.InvoiceId).Distinct().Count();

            // 3. QuantitySold: total quantity of food items sold
            var quantitySold = validFoodInvoices.Sum(fi => fi.Quantity);

            // 4. AvgOrderValue: average food revenue per order
            var avgOrderValue = foodOrders > 0 ? Math.Round(foodRevenue / foodOrders, 0) : 0;

            // 5. RevenueByDayDates, RevenueByDayValues, OrdersByDayValues (last 7 days)
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).Reverse().ToList();
            var revenueByDayValues = last7Days
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day && !fi.Invoice.Cancel).Sum(fi => fi.Price * fi.Quantity))
                .ToList();
            var ordersByDayValues = last7Days
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day && !fi.Invoice.Cancel).Select(fi => fi.InvoiceId).Distinct().Count())
                .ToList();

            // 6. TopFoodItems: all food items with revenue and order count
            var topFoodItems = validFoodInvoices
                .GroupBy(fi => fi.Food.Name)
                .Select(g => new FoodItemQuantity {
                    FoodName = g.Key,
                    Quantity = g.Sum(fi => fi.Quantity),
                    Category = g.First().Food.Category,
                    Revenue = g.Sum(fi => fi.Price * fi.Quantity),
                    OrderCount = g.Select(fi => fi.InvoiceId).Distinct().Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // 7. SalesByCategory: revenue by food category
            var salesByCategory = validFoodInvoices
                .GroupBy(fi => fi.Food.Category)
                .Select(g => (Category: g.Key, Revenue: g.Sum(fi => fi.Price * fi.Quantity)))
                .ToList();

            // 8. SalesByHour: number of food orders per hour (0-23)
            var salesByHour = Enumerable.Range(0, 24)
                .Select(h => validFoodInvoices.Count(fi => fi.Invoice.BookingDate.HasValue && fi.Invoice.BookingDate.Value.Hour == h && !fi.Invoice.Cancel))
                .ToList();

            // 1. All food invoices on completed orders (whether cancelled or not)
            var allFoodInvoices = _context.FoodInvoices
                .Include(fi => fi.Invoice)
                .Where(fi => fi.Invoice.Status == InvoiceStatus.Completed)
                .ToList();

            // 2. Split into valid sales vs cancelled sales
            var validFoodSales = allFoodInvoices.Where(fi => !fi.Invoice.Cancel);

            // 3. Compute gross revenue only (no refunds for food)
            var foodGrossRevenue = validFoodSales.Sum(fi => fi.Price * fi.Quantity);

            var todayFoodInvoices = allFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).ToList();
            var todayFoodGross = todayFoodInvoices.Where(fi => !fi.Invoice.Cancel).Sum(fi => fi.Price * fi.Quantity);

            var todayValidFoodInvoices = todayFoodInvoices.Where(fi => !fi.Invoice.Cancel).ToList();
            var todayFoodRevenue = todayValidFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var todayFoodOrders = todayValidFoodInvoices.Select(fi => fi.InvoiceId).Distinct().Count();
            var todayQuantitySold = todayValidFoodInvoices.Sum(fi => fi.Quantity);
            var todayAvgOrderValue = todayFoodOrders > 0 ? Math.Round(todayFoodRevenue / todayFoodOrders, 0) : 0;

            // Calculate 7-day averages
            var sevenDayTotalRevenue = revenueByDayValues.Sum();
            var sevenDayTotalOrders = ordersByDayValues.Sum();
            var sevenDayAverageRevenue = sevenDayTotalRevenue / 7;
            var sevenDayAverageOrders = sevenDayTotalOrders / 7;

            var sevenDayTotalQuantity = last7Days
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day && !fi.Invoice.Cancel).Sum(fi => fi.Quantity))
                .Sum();
            var sevenDayAverageItemsPerOrder = sevenDayTotalOrders > 0 ? (decimal)sevenDayTotalQuantity / sevenDayTotalOrders : 0;


            return new AdminDashboardViewModel
            {
                RevenueToday = revenueToday,
                TotalBookings = totalBookings,
                BookingsToday = bookingsToday,
                TicketsSoldToday = ticketsSoldToday,
                OccupancyRateToday = occupancyRate,
                RevenueTrendDates = last7,
                RevenueTrendValues = revTrend,
                BookingTrendDates = last7,
                BookingTrendValues = bookTrend,
                VoucherTrendValues = voucherTrend,
                TopMovies = topMovies,
                TopMembers = topMembers,
                RecentBookings = recentBookings,
                MovieAnalytics = new MovieAnalyticsViewModel
                {
                    RecentBookings = recentMovieBookings,
                    RecentCancellations = recentMovieCancellations
                },
                RecentMembers = recentMembers,
                GrossRevenue = grossRevenue,
                NetRevenue = netRevenue,
                TotalVouchersIssued = totalVouchersIssued,
                VouchersToday = vouchersToday,
                FoodAnalytics = new FoodAnalyticsViewModel
                {
                    GrossRevenue = foodGrossRevenue,
                    GrossRevenueToday = todayFoodGross,
                    TotalOrders = foodOrders,
                    OrdersToday = todayFoodOrders,
                    QuantitySoldToday = todayQuantitySold,
                    AvgOrderValueToday = todayAvgOrderValue,
                    SevenDayAverageRevenue = sevenDayAverageRevenue,
                    SevenDayAverageOrders = sevenDayAverageOrders,
                    SevenDayAverageItemsPerOrder = sevenDayAverageItemsPerOrder,
                    FoodRevenueToday = todayFoodRevenue,
                    FoodRevenue = foodRevenue,
                    Orders = foodOrders,
                    QuantitySold = quantitySold,
                    AvgOrderValue = avgOrderValue,
                    RevenueByDayDates = last7Days,
                    RevenueByDayValues = revenueByDayValues,
                    OrdersByDayValues = ordersByDayValues,
                    TopFoodItems = topFoodItems,
                    SalesByCategory = salesByCategory,
                    SalesByHour = salesByHour,
                    RecentOrders = recentFoodOrders,
                    RecentCancels = recentFoodCancels
                }
            };
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
