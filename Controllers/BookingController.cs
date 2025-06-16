using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MovieTheater.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _service;
        private readonly ISeatService _seatService;
        private readonly IAccountService _accountService;
        private readonly IScheduleSeatRepository _scheduleSeatRepository;
        private readonly ILogger<BookingController> _logger;
        private readonly IMovieService _movieService;
        private readonly VNPayService _vnPayService;

        public BookingController(
            IBookingService service, 
            ISeatService seatService, 
            IAccountService accountService, 
            IScheduleSeatRepository scheduleSeatRepository,
            ILogger<BookingController> logger, 
            IMovieService movieService,
      
            VNPayService vnPayService)
        {
            _service = service;
            _seatService = seatService;
            _accountService = accountService;
            _scheduleSeatRepository = scheduleSeatRepository;
            _logger = logger;
            _movieService = movieService;
            _vnPayService = vnPayService;
        }

        [HttpGet]
        public IActionResult TicketBooking(string movieId = null)
        {
            var movies = _service.GetAvailableMovies();
            ViewBag.MovieList = movies;
            ViewBag.SelectedMovieId = movieId;

            if (!string.IsNullOrEmpty(movieId))
            {
                // Get movie shows for the selected movie
                var movieShows = _movieService.GetMovieShows(movieId);
                
                // Group by date and time
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
            var dates = await _service.GetShowDates(movieId);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpGet]
        public async Task<IActionResult> GetTimes(string movieId, DateTime date)
        {
            var times = _service.GetShowTimes(movieId, date);
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

            var movie = _service.GetById(movieId);
            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            // Get the movie show ID based on the selected date and time
            var movieShows = _movieService.GetMovieShows(movieId);
            var selectedMovieShow = movieShows.FirstOrDefault(ms => 
                ms.ShowDate?.ShowDate1?.ToString("yyyy-MM-dd") == showDate.ToString("yyyy-MM-dd") &&
                ms.Schedule?.ScheduleTime == showTime);

            if (selectedMovieShow == null)
            {
                return NotFound("Movie show not found for the selected date and time.");
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
                CinemaRoomName = "Room " + movie.CinemaRoomId,
                ShowDate = showDate,
                ShowTime = showTime,
                SelectedSeats = seats,
                TotalPrice = totalPrice,
                MovieShowId = selectedMovieShow.MovieShowId,

                FullName = currentUser.FullName,
                Email = currentUser.Email,
                IdentityCard = currentUser.IdentityCard,
                PhoneNumber = currentUser.PhoneNumber,
                CurrentScore = currentUser.Score
            };

            return View("ConfirmBooking", viewModel);
        }


        //CONFIRM BOOK TICKET -> SAVE INVOICE TO DB
        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model)
        {
            try
            {
                //TEST FAILED CASE
                //throw new Exception("Test exception");

                // Lấy Account ID từ JWT claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Tạo danh sách tên ghế
                var seatNames = model.SelectedSeats.Select(s => s.SeatName);
                string seatList = string.Join(" ", seatNames);

                model.UseScore = Math.Min(model.UseScore, (int)model.TotalPrice);
                
                // 1. First create and save the Invoice
                var invoice = new Invoice
                {
                    InvoiceId = await _service.GenerateInvoiceIdAsync(),
                    AccountId = userId,
                    AddScore = (int)(model.TotalPrice * 0.01m),
                    BookingDate = DateTime.Now,
                    MovieName = model.MovieName,
                    ScheduleShow = model.ShowDate,
                    ScheduleShowTime = model.ShowTime,
                    Status = 1,
                    TotalMoney = model.TotalPrice - model.UseScore,
                    UseScore = model.UseScore,
                    Seat = seatList
                };

                // Save Invoice to DB
                await _service.SaveInvoiceAsync(invoice);

                // 2. Then create ScheduleSeat records with the InvoiceId
                var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
                {
                    MovieShowId = model.MovieShowId,
                    InvoiceId = invoice.InvoiceId,
                    SeatId = (int)seat.SeatId,
                    SeatStatusId = 2
                });

                await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);

                // GIẢM ĐIỂM NẾU USESCORE > 0
                if (model.UseScore > 0)
                {
                    await _accountService.DeductScoreAsync(userId, model.UseScore);
                }

                // Chuyển hướng đến trang thanh toán
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


        // GET: Trang sau khi đặt vé thành công
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Payment(string invoiceId)
        {
            var invoice = _service.GetInvoiceById(invoiceId);
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
                OrderInfo = $"Thanh toan ve xem phim {invoice.MovieName} - {invoice.Seat}"
            };

            return View(viewModel);
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
    }
}
