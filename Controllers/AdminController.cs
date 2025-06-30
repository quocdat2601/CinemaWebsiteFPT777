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
            IScheduleSeatRepository scheduleSeatRepository)
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
        }

        // GET: AdminController
        [Authorize(Roles = "Admin")]
        public IActionResult MainPage(string tab = "Dashboard")
        {
            ViewData["ActiveTab"] = tab;
            var model = GetDashboardViewModel();
            return View(model);
        }

        public IActionResult LoadTab(string tab, string keyword = null)
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
                    var versions = _movieService.GetAllVersions();
                    ViewBag.Versions = versions;
                    ViewBag.SeatTypes = seatTypes;
                    return PartialView("ShowroomMg", cinema);
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

        // [HttpGet]
        // /// Admin: Initiate ticket selling for a member
        // /// url: /Admin/InitiateTicketSellingForMember
        [Authorize(Roles = "Admin")]
        public IActionResult InitiateTicketSellingForMember(string id)
        {
            // Store the member's AccountId in TempData to use in the ticket selling process
            TempData["InitiateTicketSellingForMemberId"] = id;

            var returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            return RedirectToAction("Select", "Showtime", new { returnUrl = returnUrl });
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


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CheckMemberDetails([FromBody] MemberCheckRequest request)
        {
            var member = _memberRepository.GetByIdentityCard(request.MemberInput)
                ?? _memberRepository.GetByMemberId(request.MemberInput)
                ?? _memberRepository.GetByAccountId(request.MemberInput);

            if (member == null || member.Account == null)
            {
                return Json(new { success = false, message = "No member has found!" });
            }

            return Json(new
            {
                success = true,
                memberId = member.MemberId,
                fullName = member.Account.FullName,
                identityCard = member.Account.IdentityCard,
                phoneNumber = member.Account.PhoneNumber,
                memberScore = member.Score
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult TicketBookingConfirmed(string invoiceId)
        {
            if (string.IsNullOrEmpty(invoiceId))
                return View("TicketBookingConfirmed"); // fallback, but not recommended

            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
                return NotFound();

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            // Prepare seat details
            var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNames)
            {
                var trimmedSeatName = seatName.Trim();
                var seat = _seatService.GetSeatByName(trimmedSeatName);
                if (seat == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TicketBookingConfirmed] Seat not found: '{trimmedSeatName}'");
                }
                SeatType seatType = null;
                if (seat != null && seat.SeatTypeId.HasValue)
                {
                    seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                }
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = trimmedSeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = seatType?.PricePercent ?? 0
                });
            }

            // Calculate tickets converted by score
            int ticketsConverted = 0;
            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
            {
                // Sort seats by price descending and count how many could be converted by the used score
                var sortedSeats = seats.OrderByDescending(s => s.Price).ToList();
                decimal runningScore = invoice.UseScore.Value;
                foreach (var seat in sortedSeats)
                {
                    if (runningScore >= seat.Price)
                    {
                        ticketsConverted++;
                        runningScore -= seat.Price;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieId = invoice.MovieShow.MovieId,
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                CinemaRoomName = invoice.MovieShow.CinemaRoom.CinemaRoomName,
                ShowDate = invoice.MovieShow.ShowDate,
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null
            };

            // Determine return URL based on user role
            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = member?.MemberId,
                MemberEmail = member?.Account?.Email,
                MemberIdentityCard = member?.Account?.IdentityCard,
                MemberPhone = member?.Account?.PhoneNumber
            };

            return View("TicketBookingConfirmed", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult CheckScoreForConversion([FromBody] ScoreConversionRequest request)
        {
            var prices = request.TicketPrices.OrderByDescending(p => p).ToList(); // Convert most expensive first
            if (request.TicketsToConvert > prices.Count)
                return Json(new { success = false, message = "Not enough tickets selected." });

            var selected = prices.Take(request.TicketsToConvert).ToList();
            var totalNeeded = (int)selected.Sum();

            if (request.MemberScore >= totalNeeded)
            {
                return Json(new { success = true, ticketsConverted = request.TicketsToConvert, scoreNeeded = totalNeeded, tickets = selected });
            }
            else
            {
                return Json(new { success = false, message = "Member score is not enough to convert into ticket", scoreNeeded = totalNeeded });
            }
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult TicketInfo(string invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
                return NotFound();

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            // Prepare seat details
            var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNames)
            {
                // Fetch seat details using new method
                var seat = _seatService.GetSeatByName(seatName);
                SeatType seatType = null;
                if (seat != null && seat.SeatTypeId.HasValue)
                {
                    seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                }
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = seatType?.PricePercent ?? 0
                });
            }

            // Calculate tickets converted by score
            int ticketsConverted = 0;
            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
            {
                // Sort seats by price descending and count how many could be converted by the used score
                var sortedSeats = seats.OrderByDescending(s => s.Price).ToList();
                decimal runningScore = invoice.UseScore.Value;
                foreach (var seat in sortedSeats)
                {
                    if (runningScore >= seat.Price)
                    {
                        ticketsConverted++;
                        runningScore -= seat.Price;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieId = invoice.MovieShow.MovieId,
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                CinemaRoomName = invoice.MovieShow.CinemaRoom.CinemaRoomName,
                ShowDate = invoice.MovieShow.ShowDate,
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null
            };

            // Determine return URL based on user role
            string returnUrl;
            if (User.IsInRole("Admin"))
            {
                returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            }
            else
            {
                returnUrl = Url.Action("MainPage", "Employee", new { tab = "BookingMg" });
            }

            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = member?.MemberId,
                MemberEmail = member?.Account?.Email,
                MemberIdentityCard = member?.Account?.IdentityCard,
                MemberPhone = member?.Account?.PhoneNumber
            };

            return View("TicketBookingConfirmed", viewModel);
        }

        public class MemberCheckRequest
        {
            public string MemberInput { get; set; }
        }

        public class ScoreConversionRequest
        {
            public List<decimal> TicketPrices { get; set; }
            public int TicketsToConvert { get; set; }
            public int MemberScore { get; set; }
        }
        private AdminDashboardViewModel GetDashboardViewModel()
        {
            var today = DateTime.Today;
            var allInvoices = _invoiceService.GetAll().ToList();

            // Only "completed" and "cancelled"
            var completed = allInvoices.Where(i => i.Status == 1).ToList();
            var cancelled = allInvoices.Where(i => i.Status == 0).ToList();

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

            // 3) 7-day trends
            var last7 = Enumerable.Range(0, 7)
                           .Select(i => today.AddDays(-i))
                           .Reverse()
                           .ToList();
            var revTrend = last7
                .Select(d => allInvoices
                    .Where(inv => inv.BookingDate?.Date == d && inv.Status == 1)
                    .Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();
            var bookTrend = last7
                .Select(d => allInvoices
                    .Count(inv => inv.BookingDate?.Date == d && inv.Status == 1))
                .ToList();

            // 4) Top 5 movies & members
            var topMovies = completed
                .GroupBy(i => i.MovieShow.Movie.MovieNameEnglish)
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
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
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
                RecentMembers = recentMembers,
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
            var repo = HttpContext.RequestServices.GetService(typeof(IMovieRepository)) as MovieRepository;
            if (repo == null)
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
    }
}
