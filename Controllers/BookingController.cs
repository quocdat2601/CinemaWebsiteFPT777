using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MovieTheater.Controllers
{
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

    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IMovieService _movieService;
        private readonly ISeatService _seatService;
        private readonly IAccountService _accountService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly IMemberRepository _memberRepository;
        private readonly IInvoiceService _invoiceService;
        private readonly ICinemaService _cinemaService;
        private readonly IScheduleSeatRepository _scheduleSeatRepository;
        private readonly ILogger<BookingController> _logger;
        private readonly VNPayService _vnPayService;

        public BookingController(IBookingService bookingService,
                         IMovieService movieService,
                         ISeatService seatService,
                         IAccountService accountService,
                         ISeatTypeService seatTypeService,
                         IMemberRepository memberRepository,
                         ILogger<BookingController> logger,
                         IInvoiceService invoiceService,
                         ICinemaService cinemaService,
                         IScheduleSeatRepository scheduleSeatRepository,
                         VNPayService vnPayService)
        {
            _bookingService = bookingService;
            _movieService = movieService;
            _seatService = seatService;
            _accountService = accountService;
            _seatTypeService = seatTypeService;
            _memberRepository = memberRepository;
            _logger = logger;
            _invoiceService = invoiceService;
            _cinemaService = cinemaService;
            _scheduleSeatRepository = scheduleSeatRepository;
            _vnPayService = vnPayService;
        }

        [HttpGet]
        public async Task<IActionResult> TicketBooking(string movieId = null)
        {
            var movies = await _bookingService.GetAvailableMoviesAsync();
            ViewBag.MovieList = movies;
            ViewBag.SelectedMovieId = movieId;

            if (!string.IsNullOrEmpty(movieId))
            {
                var movieShows = _movieService.GetMovieShows(movieId);
                var showsByDate = movieShows
                    .Where(ms => ms.ShowDate?.ShowDate1 != null && ms.Schedule?.ScheduleTime != null)
                    .GroupBy(ms => ms.ShowDate.ShowDate1.Value.ToString("dd/MM/yyyy"))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(ms => ms.Schedule.ScheduleTime)
                              .Distinct()
                              .OrderBy(t => t)
                              .ToList()
                    );
                ViewBag.ShowsByDate = showsByDate;
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDates(string movieId)
        {
            var dates = await _bookingService.GetShowDatesAsync(movieId);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpGet]
        public async Task<IActionResult> GetTimes(string movieId, DateTime date)
        {
            var times = await _bookingService.GetShowTimesAsync(movieId, date);
            return Json(times);
        }

        [HttpGet]
        public async Task<IActionResult> Information(string movieId, DateTime showDate, string showTime, List<int>? selectedSeatIds)
        {
            if (selectedSeatIds == null || selectedSeatIds.Count == 0)
            {
                TempData["BookingError"] = "No seats were selected.";
                return RedirectToAction("TicketBooking", new { movieId });
            }

            var movie = _bookingService.GetById(movieId);
            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            var movieShows = _movieService.GetMovieShows(movieId);
            var movieShow = movieShows.FirstOrDefault(ms =>
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(showDate) &&
                ms.Schedule?.ScheduleTime == showTime);

            if (movieShow == null)
            {
                return NotFound("Movie show not found for the specified date and time.");
            }

            var cinemaRoom = movieShow.CinemaRoom;
            if (cinemaRoom == null)
            {
                return NotFound("Cinema room not found for this movie show.");
            }

            var seatTypes = await _seatService.GetSeatTypesAsync();
            var currentUser = _accountService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var seats = new List<SeatDetailViewModel>();
            foreach (var id in selectedSeatIds)
            {
                var seat = await _seatService.GetSeatByIdAsync(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = price
                });
            }

            var totalPrice = seats.Sum(s => s.Price);

            var viewModel = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                SelectedSeats = seats,
                TotalPrice = totalPrice,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                IdentityCard = currentUser.IdentityCard,
                PhoneNumber = currentUser.PhoneNumber,
                CurrentScore = currentUser.Score
            };

            return View("ConfirmBooking", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var seatNames = model.SelectedSeats.Select(s => s.SeatName);
                string seatList = string.Join(",", seatNames);

                model.UseScore = Math.Min(model.UseScore, (int)model.TotalPrice);
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = userId,
                    AddScore = (int)(model.TotalPrice * 0.01m),
                    BookingDate = DateTime.Now,
                    MovieName = model.MovieName,
                    ScheduleShow = model.ShowDate,
                    ScheduleShowTime = model.ShowTime,
                    Status = InvoiceStatus.Incomplete,
                    TotalMoney = model.TotalPrice - model.UseScore,
                    UseScore = model.UseScore,
                    Seat = seatList
                };

                await _bookingService.SaveInvoiceAsync(invoice);

                var movieShow = _movieService.GetMovieShows(model.MovieId)
                    .FirstOrDefault(ms =>
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.ShowDate) &&
                        ms.Schedule?.ScheduleTime == model.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
                }

                var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
                {
                    MovieShowId = movieShow.MovieShowId,
                    InvoiceId = invoice.InvoiceId,
                    SeatId = (int)seat.SeatId,
                    SeatStatusId = 2
                });

                await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);

                if (model.UseScore > 0)
                {
                    await _accountService.DeductScoreAsync(userId, model.UseScore);
                }

                return RedirectToAction("Payment", new { invoiceId = invoice.InvoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during booking ticket.");
                TempData["ErrorMessage"] = "Booking failed. Please try again later.";
                return RedirectToAction("Information", new
                {
                    movieId = model.MovieId,
                    showDate = model.ShowDate.ToString("yyyy-MM-dd"),
                    showTime = model.ShowTime,
                    selectedSeatIds = model.SelectedSeats.Select(s => s.SeatName)
                });
            }
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Payment(string invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var viewModel = new PaymentViewModel
            {
                InvoiceId = invoice.InvoiceId,
                MovieName = invoice.MovieName,
                ShowDate = invoice.ScheduleShow ?? DateTime.MinValue,
                ShowTime = invoice.ScheduleShowTime,
                Seats = invoice.Seat,
                TotalAmount = invoice.TotalMoney ?? 0,
                OrderInfo = $"Payment for movie ticket {invoice.MovieName} - {invoice.Seat.Replace(",", " ")}"
            };

            return View("Payment", viewModel);
        }

        [HttpPost]
        public IActionResult ProcessPayment(PaymentViewModel model)
        {
            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    model.TotalAmount,
                    model.OrderInfo,
                    model.InvoiceId
                );

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment URL");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo URL thanh toán. Vui lòng thử lại sau.";
                return RedirectToAction("Payment", new { invoiceId = model.InvoiceId });
            }
        }

        [HttpGet]
        public IActionResult Failed()
        {
            var invoiceId = TempData["InvoiceId"] as string;
            if (!string.IsNullOrEmpty(invoiceId))
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status != InvoiceStatus.Incomplete)
                {
                    invoice.Status = InvoiceStatus.Incomplete;
                    invoice.UseScore = 0;
                    var context = new MovieTheater.Models.MovieTheaterContext();
                    context.Invoices.Update(invoice);
                    context.SaveChanges();
                }
            }
            return View();
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
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = price
                });
            }
            var movieShows = _movieService.GetMovieShows(movieId);
            var movieShow = movieShows.FirstOrDefault(ms =>
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(showDate) &&
                ms.Schedule?.ScheduleTime == showTime);

            if (movieShow == null)
            {
                return NotFound("Movie show not found for the specified date and time.");
            }

            var cinemaRoom = movieShow.CinemaRoom;
            if (cinemaRoom == null)
            {
                return NotFound("Cinema room not found for this movie show.");
            }
            var totalPrice = seats.Sum(s => s.Price);

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
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
                ReturnUrl = Url.Action("Select", "Seat", new
                {
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
                        return Json(new { success = false, message = "Member score is not enough to convert into ticket" });
                    }
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUser = _accountService.GetById(currentUserId);
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = member?.Account?.AccountId ?? currentUserId,
                    AddScore = (int)((model.BookingDetails.TotalPrice - discount) * 0.1m),
                    BookingDate = DateTime.Now,
                    MovieName = model.BookingDetails.MovieName,
                    ScheduleShow = model.BookingDetails.ShowDate,
                    ScheduleShowTime = model.BookingDetails.ShowTime,
                    Status = InvoiceStatus.Completed,
                    TotalMoney = model.BookingDetails.TotalPrice - discount,
                    UseScore = scoreUsed,
                    Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                    //RoleId = currentUser?.RoleId
                };

                string roomName = "N/A";
                if (!string.IsNullOrEmpty(model.BookingDetails.MovieId))
                {
                    var movie = _movieService.GetById(model.BookingDetails.MovieId);
                    if (movie != null && movie.CinemaRoomId.HasValue)
                    {
                        var room = _cinemaService.GetById(movie.CinemaRoomId.Value);
                        roomName = room?.CinemaRoomName ?? "N/A";
                    }
                }

                await _bookingService.SaveInvoiceAsync(invoice);

                var movieShow = _movieService.GetMovieShows(model.BookingDetails.MovieId)
                    .FirstOrDefault(ms => 
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.BookingDetails.ShowDate) && 
                        ms.Schedule?.ScheduleTime == model.BookingDetails.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
                }

                var scheduleSeats = model.BookingDetails.SelectedSeats.Select(seat => new ScheduleSeat
                {
                    MovieShowId = movieShow.MovieShowId,
                    InvoiceId = invoice.InvoiceId,
                    SeatId = (int)seat.SeatId,
                    SeatStatusId = 2
                });

                await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);

                TempData["ToastMessage"] = "Movie booked successfully!";

                return Json(new { success = true, redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { invoiceId = invoice.InvoiceId }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Booking failed. Please try again later." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult TicketBookingConfirmed(string invoiceId)
        {
            if (string.IsNullOrEmpty(invoiceId))
                return View("TicketBookingConfirmed");

            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
                return NotFound();

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            var allMovies = _movieService.GetAll();
            var movie = allMovies.FirstOrDefault(m =>
                string.Equals(m.MovieNameEnglish?.Trim(), invoice.MovieName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.MovieNameVn?.Trim(), invoice.MovieName?.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (movie == null)
            {
                return NotFound("Movie not found for this invoice.");
            }

            var movieShows = _movieService.GetMovieShows(movie.MovieId);
            var movieShow = movieShows.FirstOrDefault(ms =>
                ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(invoice.ScheduleShow ?? DateTime.Now) &&
                ms.Schedule?.ScheduleTime == invoice.ScheduleShowTime);

            if (movieShow == null)
            {
                return NotFound("Movie show not found for the specified date and time.");
            }

            var cinemaRoom = movieShow.CinemaRoom;
            if (cinemaRoom == null)
            {
                return NotFound("Cinema room not found for this movie show.");
            }
            // Prepare seat details
            var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNamesArr)
            {
                var seat = _seatService.GetSeatByName(seatName);
                SeatType seatType = null;
                if (seat != null && seat.SeatTypeId.HasValue)
                {
                    seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                }
                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = seatType?.PricePercent ?? 0
                });
            }

            int ticketsConverted = 0;
            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
            {
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
                MovieId = movie?.MovieId,
                MovieName = invoice.MovieName,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = invoice.ScheduleShow ?? DateTime.Now,
                ShowTime = invoice.ScheduleShowTime,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null
            };

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
            var prices = request.TicketPrices.OrderByDescending(p => p).ToList();
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
            if (string.IsNullOrEmpty(invoiceId))
                return NotFound();

            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found: {invoiceId}");
                return NotFound();
            }

            var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();

            var allMovies = _movieService.GetAll();
            var movie = allMovies.FirstOrDefault(m =>
                string.Equals(m.MovieNameEnglish?.Trim(), invoice.MovieName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.MovieNameVn?.Trim(), invoice.MovieName?.Trim(), StringComparison.OrdinalIgnoreCase)
            );
            if (movie == null)
            {
                return NotFound("Movie not found for this invoice.");
            }

            var movieShow = _movieService.GetMovieShows(movie.MovieId)
                .FirstOrDefault(ms => 
                    ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(invoice.ScheduleShow ?? DateTime.Now) && 
                    ms.Schedule?.ScheduleTime == invoice.ScheduleShowTime);

            if (movieShow == null)
            {
                return NotFound("Movie show not found for this invoice.");
            }

            var firstScheduleSeat = scheduleSeats.FirstOrDefault();
            if (firstScheduleSeat == null)
            {
                var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                var newScheduleSeats = new List<ScheduleSeat>();
                
                foreach (var seatName in seatNames)
                {
                    var trimmedSeatName = seatName.Trim();
                    var seat = _seatService.GetSeatByName(trimmedSeatName);
                    if (seat == null)
                    {
                        continue;
                    }

                    newScheduleSeats.Add(new ScheduleSeat
                    {
                        MovieShowId = movieShow.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = seat.SeatId,
                        SeatStatusId = 2
                    });
                }

                if (newScheduleSeats.Any())
                {
                    _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(newScheduleSeats).Wait();
                    scheduleSeats = newScheduleSeats;
                }
                else
                {
                    return NotFound("No valid seats found for this invoice.");
                }
            }

            var cinemaRoom = movieShow.CinemaRoom;
            if (cinemaRoom == null)
            {
                return NotFound("Cinema room not found.");
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);
            // Prepare seat details
            var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNamesArr)
            {
                var seat = _seatService.GetSeatByName(seatName);
                SeatType seatType = null;
                if (seat != null && seat.SeatTypeId.HasValue)
                {
                    seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                }

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = seatType?.PricePercent ?? 0
                });
            }

            int ticketsConverted = 0;
            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
            {
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
                MovieId = movieShow.MovieId,
                MovieName = invoice.MovieName,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = invoice.ScheduleShow ?? DateTime.Now,
                ShowTime = invoice.ScheduleShowTime,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null,
                FullName = member?.Account?.FullName,
                Email = member?.Account?.Email,
                IdentityCard = member?.Account?.IdentityCard,
                PhoneNumber = member?.Account?.PhoneNumber,
                CurrentScore = member?.Score ?? 0
            };

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
        [HttpGet]
        public IActionResult GetAllMembers()
        {
            var members = _memberRepository.GetAll()
                .Select(m => new {
                    memberId = m.MemberId,
                    score = m.Score,
                    account = new {
                        fullName = m.Account?.FullName,
                        identityCard = m.Account?.IdentityCard,
                        email = m.Account?.Email,
                        phoneNumber = m.Account?.PhoneNumber
                    }
                }).ToList();
            return Json(members);
        }
    }
}
