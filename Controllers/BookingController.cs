using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
        private readonly IPointService _pointService;
        private readonly IRankService _rankService;

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
                         VNPayService vnPayService,
                         IPointService pointService,
                         IRankService rankService)
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
            _pointService = pointService;
            _rankService = rankService;
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

            // Reload account with rank
            var userAccount = _accountService.GetById(currentUser.AccountId);

            decimal earningRate = 1;
            decimal rankDiscountPercent = 0;
            if (userAccount?.Rank != null)
            {
                earningRate = userAccount.Rank.PointEarningPercentage ?? 1;
                rankDiscountPercent = userAccount.Rank.DiscountPercentage ?? 0;
            }
            ViewBag.EarningRate = earningRate;
            ViewBag.RankDiscountPercent = rankDiscountPercent;

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

            var subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (userAccount?.Rank != null)
            {
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }

            var totalPrice = subtotal - rankDiscount;

            var viewModel = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                SelectedSeats = seats,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                TotalPrice = totalPrice,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                IdentityCard = currentUser.IdentityCard,
                PhoneNumber = currentUser.PhoneNumber,
                CurrentScore = currentUser.Score,
                EarningRate = earningRate,
                RankDiscountPercent = rankDiscountPercent
            };

            return View("ConfirmBooking", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model, string IsTestSuccess)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var userAccount = _accountService.GetById(userId);

                // Recalculate prices to prevent tampering
                var subtotal = model.SelectedSeats.Sum(s => s.Price);
                decimal rankDiscount = 0;
                if (userAccount?.Rank != null)
                {
                    var rankDiscountPercent = userAccount.Rank.DiscountPercentage ?? 0;
                    rankDiscount = subtotal * (rankDiscountPercent / 100m);
                }

                var priceAfterDiscount = subtotal - rankDiscount;

                model.UseScore = Math.Min(model.UseScore, (int)(priceAfterDiscount / 1000));
                
                var finalPrice = priceAfterDiscount - (model.UseScore * 1000);

                var seatNames = model.SelectedSeats.Select(s => s.SeatName);
                string seatList = string.Join(",", seatNames);

                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = userId,
                    BookingDate = DateTime.Now,
                    MovieName = model.MovieName,
                    ScheduleShow = model.ShowDate,
                    ScheduleShowTime = model.ShowTime,
                    Status = InvoiceStatus.Incomplete,
                    TotalMoney = finalPrice,
                    UseScore = model.UseScore,
                    Seat = seatList
                };

                // Calculate earning rate from user rank
                decimal earningRate = 1;
                if (userAccount?.Rank != null)
                {
                    earningRate = userAccount.Rank.PointEarningPercentage ?? 1;
                }
                // Calculate points to earn using the same logic as admin
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalPrice, earningRate);
                invoice.AddScore = pointsToEarn;

                await _bookingService.SaveInvoiceAsync(invoice);

                var movieShow = _movieService.GetMovieShows(model.MovieId)
                    .FirstOrDefault(ms =>
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.ShowDate) &&
                        ms.Schedule?.ScheduleTime == model.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
                }

                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
                    {
                        MovieShowId = movieShow.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = (int)seat.SeatId,
                        SeatStatusId = 2
                    });

                    await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                }

                if (model.UseScore > 0)
                {
                    await _accountService.DeductScoreAsync(userId, model.UseScore);
                }

                // Lưu MovieShowId vào TempData để PaymentController sử dụng
                TempData["MovieShowId"] = movieShow.MovieShowId;

                // Nếu là test success thì bỏ qua thanh toán, chuyển thẳng sang trang Success
                if (!string.IsNullOrEmpty(IsTestSuccess) && IsTestSuccess == "true")
                {
                    // Cập nhật status thành Completed cho test success
                    invoice.Status = InvoiceStatus.Completed;
                    await _bookingService.UpdateInvoiceAsync(invoice);
                    
                    TempData["MovieName"] = model.MovieName;
                    TempData["ShowDate"] = model.ShowDate.ToString();
                    TempData["ShowTime"] = model.ShowTime;
                    TempData["Seats"] = seatList;
                    TempData["CinemaRoomName"] = model.CinemaRoomName;
                    TempData["InvoiceId"] = invoice.InvoiceId;
                    TempData["BookingTime"] = invoice.BookingDate.ToString();
                    TempData["OriginalPrice"] = subtotal.ToString();
                    TempData["UsedScore"] = model.UseScore.ToString();
                    TempData["FinalPrice"] = finalPrice.ToString();
                    return RedirectToAction("Success");
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
        public async Task<IActionResult> Success()
        {
            var invoiceId = TempData["InvoiceId"] as string;
            if (!string.IsNullOrEmpty(invoiceId))
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status == InvoiceStatus.Completed && invoice.AddScore.HasValue && invoice.AddScore.Value > 0)
                {
                    await _accountService.AddScoreAsync(invoice.AccountId, invoice.AddScore.Value);
                }
            }
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

            var sanitizedMovieName = Regex.Replace(invoice.MovieName, @"[^a-zA-Z0-9\s]", "");
            var viewModel = new PaymentViewModel
            {
                InvoiceId = invoice.InvoiceId,
                MovieName = invoice.MovieName,
                ShowDate = invoice.ScheduleShow ?? DateTime.MinValue,
                ShowTime = invoice.ScheduleShowTime,
                Seats = invoice.Seat,
                TotalAmount = invoice.TotalMoney ?? 0,
                OrderInfo = $"Payment for movie ticket {sanitizedMovieName} - {invoice.Seat.Replace(",", " ")}"
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

                // Get member's current points and rank earning rate/discount
                int memberPoints = member?.Score ?? 0;
                decimal earningRate = 0;
                decimal discountPercent = 0;
                if (member?.Account?.RankId != null)
                {
                    var rank = member.Account.Rank;
                    earningRate = rank?.PointEarningPercentage ?? 0;
                    discountPercent = rank?.DiscountPercentage ?? 0;
                }

                decimal originalTotal = model.BookingDetails.TotalPrice;
                decimal discountAmount = originalTotal * (discountPercent / 100m);
                decimal discountedTotal = originalTotal - discountAmount;

                // Validate and calculate point usage on discounted total
                int requestedPoints = model.UsedScore;
                decimal pointDiscount = 0;
                int scoreUsed = 0;
                if (requestedPoints > 0) {
                    var pointValidation = _pointService.ValidatePointUsage(requestedPoints, discountedTotal, memberPoints);
                    if (pointValidation.ValidationErrors.Count > 0)
                    {
                        return Json(new { success = false, message = string.Join(" ", pointValidation.ValidationErrors) });
                    }
                    pointDiscount = pointValidation.DiscountAmount;
                    scoreUsed = pointValidation.PointsToUse;
                }

                // Calculate points to earn (on discounted total after points used)
                int pointsToEarn = _pointService.CalculatePointsToEarn(discountedTotal - pointDiscount, earningRate);

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUser = _accountService.GetById(currentUserId);
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = member?.Account?.AccountId ?? currentUserId,
                    AddScore = pointsToEarn,
                    BookingDate = DateTime.Now,
                    MovieName = model.BookingDetails.MovieName,
                    ScheduleShow = model.BookingDetails.ShowDate,
                    ScheduleShowTime = model.BookingDetails.ShowTime,
                    Status = InvoiceStatus.Completed,
                    TotalMoney = discountedTotal - pointDiscount,
                    UseScore = scoreUsed,
                    Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
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

                // Deduct score if used
                if (scoreUsed > 0 && member != null)
                {
                    await _accountService.DeductScoreAsync(member.Account.AccountId, scoreUsed);
                }
                // Add points (using new calculation)
                if (pointsToEarn > 0)
                {
                    await _accountService.AddScoreAsync(invoice.AccountId, pointsToEarn);
                }

                var movieShow = _movieService.GetMovieShows(model.BookingDetails.MovieId)
                    .FirstOrDefault(ms => 
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.BookingDetails.ShowDate) && 
                        ms.Schedule?.ScheduleTime == model.BookingDetails.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
                }

                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    var scheduleSeats = model.BookingDetails.SelectedSeats.Select(seat => new ScheduleSeat
                    {
                        MovieShowId = movieShow.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = (int)seat.SeatId,
                        SeatStatusId = 2
                    });

                    await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                }

                TempData["ToastMessage"] = "Movie booked successfully!";

                var subtotal = model.BookingDetails.SelectedSeats.Sum(s => s.Price);
                decimal rankDiscount = 0;
                if (member?.Account?.Rank != null)
                {
                    var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                    rankDiscount = subtotal * (rankDiscountPercent / 100m);
                }
                var viewModel = new ConfirmTicketAdminViewModel
                {
                    BookingDetails = model.BookingDetails,
                    MemberCheckMessage = "",
                    ReturnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" }),
                    MemberId = member?.MemberId,
                    MemberEmail = member?.Account?.Email,
                    MemberIdentityCard = member?.Account?.IdentityCard,
                    MemberPhone = member?.Account?.PhoneNumber,
                    UsedScore = invoice.UseScore ?? 0,
                    UsedScoreValue = (invoice.UseScore ?? 0) * 1000,
                    AddedScore = invoice.AddScore ?? 0,
                    AddedScoreValue = (invoice.AddScore ?? 0) * 1000,
                    Subtotal = subtotal,
                    RankDiscount = rankDiscount,
                    TotalPrice = invoice.TotalMoney ?? 0
                };

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
            var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNamesArr)
            {
                var seat = _seatService.GetSeatByName(seatName);
                if (seat == null)
                {
                    // Nếu không tìm thấy seat, bỏ qua và tiếp tục
                    continue;
                }
                SeatType seatType = null;
                if (seat.SeatTypeId.HasValue)
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

            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0 && seats.Count > 0)
            {
                var sortedSeats = seats.OrderByDescending(s => s.Price).ToList();
                decimal runningScore = invoice.UseScore.Value;
                foreach (var seat in sortedSeats)
                {
                    if (runningScore >= seat.Price)
                    {
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
                Status = InvoiceStatus.Completed,
                AddScore = invoice.AddScore ?? 0
            };

            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            var subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (member?.Account?.Rank != null)
            {
                var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = member?.MemberId,
                MemberEmail = member?.Account?.Email,
                MemberIdentityCard = member?.Account?.IdentityCard,
                MemberPhone = member?.Account?.PhoneNumber,
                UsedScore = invoice.UseScore ?? 0,
                UsedScoreValue = (invoice.UseScore ?? 0) * 1000,
                AddedScore = invoice.AddScore ?? 0,
                AddedScoreValue = (invoice.AddScore ?? 0) * 1000,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                TotalPrice = invoice.TotalMoney ?? 0
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
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
                return NotFound();

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            // Get room name
            string roomName = "N/A";
            var allMovies = _movieService.GetAll();
            var movie = allMovies.FirstOrDefault(m => m.MovieNameEnglish == invoice.MovieName || m.MovieNameVn == invoice.MovieName);
            if (movie != null && movie.CinemaRoomId.HasValue)
            {
                var room = _cinemaService.GetById(movie.CinemaRoomId.Value);
                roomName = room?.CinemaRoomName ?? "N/A";
            }

            // Get movie show
            var movieShow = _movieService.GetMovieShows(movie?.MovieId ?? "")
                .FirstOrDefault(ms =>
                    ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(invoice.ScheduleShow ?? DateTime.Now) &&
                    ms.Schedule?.ScheduleTime == invoice.ScheduleShowTime);

            // Prepare seat details
            var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNames)
            {
                var trimmedSeatName = seatName.Trim();
                var seat = _seatService.GetSeatByName(trimmedSeatName);
                if (seat == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TicketInfo] Seat not found: '{trimmedSeatName}'");
                    continue;
                }
                SeatType seatType = null;
                if (seat.SeatTypeId.HasValue)
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
                MovieId = movie?.MovieId,
                MovieName = invoice.MovieName,
                CinemaRoomName = roomName,
                ShowDate = invoice.ScheduleShow ?? DateTime.Now,
                ShowTime = invoice.ScheduleShowTime,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                Status = invoice.Status ?? InvoiceStatus.Incomplete,
                AddScore = invoice.AddScore ?? 0
            };

            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            var subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (member?.Account?.Rank != null)
            {
                var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = member?.MemberId,
                MemberEmail = member?.Account?.Email,
                MemberIdentityCard = member?.Account?.IdentityCard,
                MemberPhone = member?.Account?.PhoneNumber,
                UsedScore = invoice.UseScore ?? 0,
                UsedScoreValue = (invoice.UseScore ?? 0) * 1000,
                AddedScore = invoice.AddScore ?? 0,
                AddedScoreValue = (invoice.AddScore ?? 0) * 1000,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                TotalPrice = invoice.TotalMoney ?? 0
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

        [Authorize(Roles = "Admin")]
        [HttpGet("Booking/InitiateTicketSellingForMember/{id}")]
        public IActionResult InitiateTicketSellingForMember(string id)
        {
            // Store the member's AccountId in TempData to use in the ticket selling process
            TempData["InitiateTicketSellingForMemberId"] = id;

            var returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            return RedirectToAction("Select", "Showtime", new { returnUrl = returnUrl });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult GetMemberDiscount(string memberId)
        {
            if (string.IsNullOrEmpty(memberId))
                return Json(new { discountPercent = 0, earningRate = 0 });
            var member = _memberRepository.GetByMemberId(memberId);
            decimal discountPercent = 0;
            decimal earningRate = 0;
            if (member?.Account?.Rank != null)
            {
                discountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                earningRate = member.Account.Rank.PointEarningPercentage ?? 0;
            }
            return Json(new { discountPercent, earningRate });
        }
    }
}
