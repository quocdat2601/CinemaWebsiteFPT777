using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using MovieTheater.Models;
using System.Security.Claims;
using System.Linq;

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
        private readonly IBookingService _bookingService;
        private readonly ISeatService _seatService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IMovieService movieService, IEmployeeService employeeService, IPromotionService promotionService, ICinemaService cinemaService, ISeatTypeService seatTypeService, IMemberRepository memberRepository, IAccountService accountService, IBookingService bookingService, ISeatService seatService, ILogger<AdminController> logger)
        {
            _movieService = movieService;
            _employeeService = employeeService;
            _promotionService = promotionService;
            _cinemaService = cinemaService;
            _seatTypeService = seatTypeService;
            _memberRepository = memberRepository;
            _accountService = accountService;
            _bookingService = bookingService;
            _seatService = seatService;
            _logger = logger;
        }

        // GET: AdminController
        [Authorize(Roles = "Admin")]
        public IActionResult MainPage(string tab = "Dashboard")
        {
            ViewData["ActiveTab"] = tab;
            return View();
        }

        public IActionResult LoadTab(string tab,string keyword = null)
        {
            switch (tab)
            {
                case "Dashboard":
                    return PartialView("Dashboard");
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
        public IActionResult InitiateTicketSellingForMember(string id)
        {
            // Store the member's AccountId in TempData to use in the ticket selling process
            TempData["InitiateTicketSellingForMemberId"] = id;

            // Redirect to the start of the ticket selling process (ShowtimeController's Select action)
            var returnUrl = Url.Action("ConfirmTicketForAdmin", "Admin");
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
        [HttpGet]
        public async Task<IActionResult> ConfirmTicketForAdmin(string movieId, DateTime showDate, string showTime, List<int>? selectedSeatIds)
        {
            if (selectedSeatIds == null || selectedSeatIds.Count == 0)
            {
                TempData["ErrorMessage"] = "No seats were selected.";
                return RedirectToAction("MainPage", new { tab = "TicketSellingMg" });
            }

            var movie = _bookingService.GetById(movieId);
            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            var seatTypes = await _seatService.GetSeatTypesAsync();
            var seats = new List<SeatDetailViewModel>();

            foreach (var id in selectedSeatIds)
            {
                var seat = await _seatService.GetSeatByIdAsync(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;

                seats.Add(new SeatDetailViewModel
                {
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = price
                });
            }

            var totalPrice = seats.Sum(s => s.Price);

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = "Room " + movie.CinemaRoomId,
                ShowDate = showDate,
                ShowTime = showTime,
                SelectedSeats = seats,
                TotalPrice = totalPrice,
                PricePerTicket = seats.Any() ? totalPrice / seats.Count : 0
            };

            var adminConfirmUrl = Url.Action("ConfirmTicketForAdmin", "Admin");
            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = Url.Action("Select", "Seat", new {
                    movieId = movieId,
                    date = showDate.ToString("yyyy-MM-dd"),
                    time = showTime,
                    returnUrl = adminConfirmUrl
                })
            };

            return View("ConfirmTicketAdmin", viewModel);
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
        [HttpPost]
        public async Task<IActionResult> ConfirmTicketForAdmin([FromBody] ConfirmTicketAdminViewModel model)
        {
            if (model.BookingDetails == null || model.BookingDetails.SelectedSeats == null)
            {
                return Json(new { success = false, message = "Booking details or selected seats are missing." });
            }

            if (string.IsNullOrEmpty(model.MemberId))
            {
                return Json(new { success = false, message = "Member check is required before confirming." });
            }

            try
            {
                // Retrieve the member again to ensure latest score if conversion is involved
                Member member = null;
                if (!string.IsNullOrEmpty(model.MemberId))
                {
                    member = _memberRepository.GetByMemberId(model.MemberId);
                    if (member == null)
                    {
                        return Json(new { success = false, message = "Member not found. Please check member details again." });
                    }
                }

                decimal discount = 0;
                int scoreUsed = 0;
                List<int> convertedTicketIndexes = new List<int>();
                if (member != null && model.BookingDetails.SelectedSeats != null && model.BookingDetails.SelectedSeats.Count > 0 && model.TicketsToConvert > 0)
                {
                    // Sort tickets by price descending and take the number to convert
                    var sortedSeats = model.BookingDetails.SelectedSeats
                        .OrderByDescending(s => s.Price)
                        .Take(model.TicketsToConvert)
                        .ToList();

                    var totalScoreNeeded = (int)sortedSeats.Sum(s => s.Price);

                    if (member.Score >= totalScoreNeeded)
                    {
                        discount = sortedSeats.Sum(s => s.Price);
                        scoreUsed = totalScoreNeeded;
                        convertedTicketIndexes = sortedSeats.Select(s => model.BookingDetails.SelectedSeats.IndexOf(s)).ToList();
                        member.Score -= scoreUsed;
                        _memberRepository.Update(member);
                    }
                    else
                    {
                        // Not enough score, handle error (shouldn't happen if frontend check is correct)
                        return Json(new { success = false, message = "Member score is not enough to convert into ticket" });
                    }
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUser = _accountService.GetById(currentUserId);
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = member?.Account?.AccountId ?? currentUserId, // Use member's AccountId for DB FK
                    AddScore = (int)((model.BookingDetails.TotalPrice - discount) * 0.1m), // Add score based on discounted price
                    BookingDate = DateTime.Now,
                    MovieName = model.BookingDetails.MovieName,
                    ScheduleShow = model.BookingDetails.ShowDate,
                    ScheduleShowTime = model.BookingDetails.ShowTime,
                    Status = 1,
                    TotalMoney = model.BookingDetails.TotalPrice - discount,
                    UseScore = scoreUsed,
                    Seat = string.Join(",", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                    RoleId = currentUser?.RoleId // Set RoleId to the current user's role
                };

                await _bookingService.SaveInvoiceAsync(invoice);

                // Prepare data for confirmation view
                TempData["BookingConfirmedMessage"] = "Ticket booking successful!";
                TempData["MovieName"] = model.BookingDetails.MovieName;
                TempData["ShowDate"] = model.BookingDetails.ShowDate.ToString("yyyy-MM-dd");
                TempData["ShowTime"] = model.BookingDetails.ShowTime;
                TempData["Seats"] = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName));
                TempData["TotalPrice"] = (model.BookingDetails.TotalPrice - discount).ToString();
                TempData["BookingTime"] = DateTime.Now.ToString("g");
                TempData["InvoiceId"] = invoice.InvoiceId;
                TempData["ScoreUsed"] = scoreUsed;
                TempData["ConvertedTicketIndexes"] = string.Join(",", convertedTicketIndexes);
                
                // Always pass member information
                TempData["MemberId"] = member?.MemberId;
                TempData["MemberName"] = member?.Account?.FullName;
                TempData["MemberIdentityCard"] = member?.Account?.IdentityCard;
                TempData["MemberPhone"] = member?.Account?.PhoneNumber;
                TempData["Screen"] = model.BookingDetails.CinemaRoomName;
                TempData["MemberEmail"] = member?.Account?.Email;

                // Prepare seat table rows for ticket info page
                string seatTableRows = string.Join("", model.BookingDetails.SelectedSeats.Select(s => $"<tr><td>{s.SeatName}</td><td>{s.SeatType}</td><td>{s.Price:N0} VND</td></tr>"));
                TempData["SeatTableRows"] = seatTableRows;
                TempData["TicketsConverted"] = convertedTicketIndexes?.Count > 0 ? convertedTicketIndexes.Count.ToString() : null;

                return Json(new { success = true, redirectUrl = Url.Action("TicketBookingConfirmed", "Admin") });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Exception during admin ticket confirmation.");
                return Json(new { success = false, message = "Booking failed. Please try again later." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult TicketBookingConfirmed()
        {
            return View();
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
    }
}
