using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
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
        private readonly ILogger<BookingController> _logger;
        private readonly VNPayService _vnPayService;

        public BookingController(
            IBookingService service, 
            ISeatService seatService, 
            IAccountService accountService, 
            ILogger<BookingController> logger,
            VNPayService vnPayService)
        {
            _service = service;
            _seatService = seatService;
            _accountService = accountService;
            _logger = logger;
            _vnPayService = vnPayService;
        }

        [HttpGet]
        public async Task<IActionResult> TicketBooking(string movieId = null)
        {
            var movies = await _service.GetAvailableMoviesAsync();
            ViewBag.MovieList = movies;
            ViewBag.SelectedMovieId = movieId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDates(string movieId)
        {
            var dates = await _service.GetShowDatesAsync(movieId);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpGet]
        public async Task<IActionResult> GetTimes(string movieId, DateTime date)
        {
            var times = await _service.GetShowTimesAsync(movieId, date);
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

                foreach (var seat in model.SelectedSeats)
                {
                    _seatService.UpdateSeatStatus(seat.SeatId);
                }
                model.UseScore = Math.Min(model.UseScore, (int)model.TotalPrice); //GIỚI HẠN USE SCORE = TOTAL PRICE
                // Tạo đối tượng Invoice
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

                // Lưu vào DB
                await _service.SaveInvoiceAsync(invoice);

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
