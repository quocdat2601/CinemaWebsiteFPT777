using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

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
        private readonly IPromotionService _promotionService;
        private readonly IVoucherService _voucherService;
        private readonly MovieTheater.Models.MovieTheaterContext _context;

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
                         IRankService rankService,
                         IPromotionService promotionService,
                        
                         IVoucherService voucherService,
                        
                         MovieTheater.Models.MovieTheaterContext context)
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
            _voucherService = voucherService;
            _promotionService = promotionService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> TicketBooking(string movieId = null)
        {
            var movies = await _bookingService.GetAvailableMoviesAsync();
            ViewBag.MovieList = movies;
            ViewBag.SelectedMovieId = movieId;

           if (!string.IsNullOrEmpty(movieId))
           {
               // Get movie shows for the selected movie
               var movieShows = _movieService.GetMovieShows(movieId);
                
               // Group by date and time
               var showsByDate = movieShows
                   .Where(ms => ms.Schedule != null && ms.Schedule.ScheduleTime.HasValue)
                   .GroupBy(ms => ms.ShowDate.ToString("dd/MM/yyyy"))
                   .ToDictionary(
                       g => g.Key,
                       g => g.Select(ms => ms.Schedule.ScheduleTime.Value.ToString("HH:mm"))
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

        //GET: /api/booking/getversions
        /// <summary>
        /// Trả về danh sách các phiên bản của phim.
        /// </summary>
        /// <param name="movieId">Id của phim.</param>
        /// <param name="date">Ngày chiếu.</param>
        /// <returns>Json danh sách phiên bản.</returns>
        [HttpGet]
        public IActionResult GetVersions(string movieId, string date)
        {
            if (!DateTime.TryParse(date, out var showDate))
                return Json(new List<object>());

            var movieShows = _movieService.GetMovieShows(movieId)
                .Where(ms => ms.ShowDate == DateOnly.FromDateTime(showDate))
                .ToList();

            var versions = movieShows
                .Where(ms => ms.Version != null)
                .Select(ms => new { versionId = ms.Version.VersionId, versionName = ms.Version.VersionName })
                .Distinct()
                .ToList();

            return Json(versions);
        }

       //GET: /api/booking/gettimes
       /// <summary>
       /// Trả về các khung giờ chiếu của phim trong một ngày.
       /// </summary>
       /// <param name="movieId">Id của phim.</param>
       /// <param name="date">Ngày chiếu.</param>
        /// <param name="versionId">Id của phiên bản.</param>
       /// <returns>Json danh sách giờ chiếu.</returns>
       [HttpGet]
        public IActionResult GetTimes(string movieId, string date, int versionId)
        {
            if (!DateTime.TryParse(date, out var showDate))
                return Json(new List<object>());

            var movieShows = _movieService.GetMovieShows(movieId)
                .Where(ms => ms.ShowDate == DateOnly.FromDateTime(showDate) && ms.VersionId == versionId && ms.Schedule != null && ms.Schedule.ScheduleTime.HasValue)
                .ToList();

            var times = movieShows
                .Select(ms => ms.Schedule.ScheduleTime.Value.ToString("HH:mm"))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

           return Json(times);
       }
       //GET: /api/booking/information
       /// <summary>
       /// Hiển thị thông tin xác nhận đặt vé.
       /// </summary>
       /// <param name="movieId">Id phim được chọn.</param>
       /// <param name="showDate">Ngày chiếu.</param>
       /// <param name="showTime">Giờ chiếu.</param>
       /// <param name="selectedSeatIds">Danh sách ghế đã chọn.</param>
       /// <param name="movieShowId">Id của suất chiếu cụ thể.</param>
       /// <returns>View xác nhận đặt vé.</returns>
       [HttpGet]
       public async Task<IActionResult> Information(string movieId, DateOnly showDate, string showTime, List<int>? selectedSeatIds, int movieShowId)
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

           // Get the specific movie show by ID
           var movieShow = _movieService.GetMovieShowById(movieShowId);
           if (movieShow == null)
           {
               return NotFound("Movie show not found.");
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

            // Get best promotion for this show date
            var bestPromotion = _promotionService.GetBestPromotionForShowDate(showDate);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;

            var seats = new List<SeatDetailViewModel>();
            foreach (var id in selectedSeatIds)
            {
                var seat = await _seatService.GetSeatByIdAsync(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion
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
                RankDiscountPercent = rankDiscountPercent,
                VersionName = movieShow.Version.VersionName
                MovieShowId = movieShowId,
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
                var originalTotal = model.SelectedSeats.Sum(s => s.Price);
                
                // 1. Apply rank discount first
                decimal rankDiscount = 0;
                if (userAccount?.Rank != null)
                {
                    var rankDiscountPercent = userAccount.Rank.DiscountPercentage ?? 0;
                    rankDiscount = originalTotal * (rankDiscountPercent / 100m);
                }
                var afterRank = originalTotal - rankDiscount;
                if (afterRank < 0) afterRank = 0;

                // 2. Apply voucher (after rank)
                decimal voucherAmount = 0;
                if (!string.IsNullOrEmpty(model.SelectedVoucherId))
                {
                    var voucher = _voucherService.GetById(model.SelectedVoucherId);
                    if (voucher != null && voucher.AccountId == userId && (voucher.IsUsed == null || voucher.IsUsed == false) && voucher.ExpiryDate > DateTime.Now)
                    {
                        voucherAmount = Math.Min(voucher.Value, afterRank);
                    }
                }
                var afterVoucher = afterRank - voucherAmount;
                if (afterVoucher < 0) afterVoucher = 0;

                // 3. Apply promotion (get discount level from best promotion)
                decimal promotionDiscountLevel = 0;
                var bestPromotion = _promotionService.GetBestPromotionForShowDate(model.ShowDate);
                if (bestPromotion != null && bestPromotion.DiscountLevel.HasValue)
                {
                    promotionDiscountLevel = bestPromotion.DiscountLevel.Value;
                }

                // 4. Apply points
                model.UseScore = Math.Min(model.UseScore, (int)(afterVoucher / 1000));
                var pointsValue = model.UseScore * 1000;
                var finalPrice = afterVoucher - pointsValue;
                if (finalPrice < 0) finalPrice = 0;

                var seatNames = model.SelectedSeats.Select(s => s.SeatName);
                string seatList = string.Join(",", seatNames);

                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = userId,
                    BookingDate = DateTime.Now,
                    
                    Status = InvoiceStatus.Incomplete,
                    TotalMoney = finalPrice,
                    UseScore = model.UseScore,
                    Seat = seatList,
                    VoucherId = !string.IsNullOrEmpty(model.SelectedVoucherId) ? model.SelectedVoucherId : null,
                    PromotionDiscount = (int?)promotionDiscountLevel,
                    MovieShowId = model.MovieShowId
                };

                // Calculate earning rate from user rank
                decimal earningRate = 1;
                if (userAccount?.Rank != null)
                {
                    earningRate = userAccount.Rank.PointEarningPercentage ?? 1;
                }
                // Calculate points to earn using final price after all discounts
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalPrice, earningRate);
                invoice.AddScore = pointsToEarn;

                await _bookingService.SaveInvoiceAsync(invoice);
                _accountService.CheckAndUpgradeRank(userId);

                // Update voucher if used
                if (voucherAmount > 0 && !string.IsNullOrEmpty(model.SelectedVoucherId))
                {
                    var voucher = _voucherService.GetById(model.SelectedVoucherId);
                    if (voucher != null)
                    {
                        voucher.IsUsed = true;
                        _voucherService.Update(voucher);
                    }
                }

                var movieShow = _movieService.GetMovieShows(model.MovieId)
                    .FirstOrDefault(ms =>
                        ms.ShowDate?.ShowDate1 == DateOnly.FromDateTime(model.ShowDate) &&
                        ms.Schedule?.ScheduleTime == model.ShowTime);

                if (movieShow == null)
                {
                    return Json(new { success = false, message = "Movie show not found for the specified date and time." });
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

                    // Tạo ScheduleSeat sau khi đã cập nhật status
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

                    TempData["MovieName"] = model.MovieName;
                    TempData["ShowDate"] = model.ShowDate.ToString();
                    TempData["ShowTime"] = model.ShowTime;
                    TempData["Seats"] = seatList;
                    TempData["CinemaRoomName"] = model.CinemaRoomName;
                    TempData["InvoiceId"] = invoice.InvoiceId;
                    TempData["BookingTime"] = invoice.BookingDate.ToString();
                    TempData["OriginalPrice"] = originalTotal.ToString();
                    TempData["UsedScore"] = model.UseScore.ToString();
                    TempData["FinalPrice"] = finalPrice.ToString();
                    return RedirectToAction("Success");
                }

                // Tạo ScheduleSeat cho trường hợp thanh toán bình thường
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

                // Always set CinemaRoomName in TempData before redirect
                TempData["CinemaRoomName"] = model.CinemaRoomName;

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

                // Get seat details from session first
                var seats = new List<SeatDetailViewModel>();
                var sessionKey = "ConfirmedSeats_" + invoiceId;
                var seatsJson = HttpContext.Session.GetString(sessionKey);
                
                if (!string.IsNullOrEmpty(seatsJson))
                {
                    seats = JsonConvert.DeserializeObject<List<SeatDetailViewModel>>(seatsJson);
                }
                else
                {
                    // Fallback to building seat details from invoice
                    var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
                    
                    foreach (var seatName in seatNamesArr)
                    {
                        var seat = _seatService.GetSeatByName(seatName);
                        if (seat == null) continue;
                        
                        SeatType seatType = null;
                        if (seat.SeatTypeId.HasValue)
                        {
                            seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                        }

                        decimal originalPrice = seatType?.PricePercent ?? 0;
                        decimal seatPromotionDiscount = invoice.PromotionDiscount ?? 0;
                        decimal priceAfterPromotion = originalPrice;

                        if (seatPromotionDiscount > 0)
                        {
                            priceAfterPromotion = originalPrice * (1 - seatPromotionDiscount / 100m);
                        }

                        seats.Add(new SeatDetailViewModel
                        {
                            SeatId = seat.SeatId,
                            SeatName = seat.SeatName,
                            SeatType = seatType?.TypeName ?? "N/A",
                            Price = priceAfterPromotion,
                            OriginalPrice = originalPrice,
                            PromotionDiscount = seatPromotionDiscount,
                            PriceAfterPromotion = priceAfterPromotion
                        });
                    }
                }

                ViewBag.SeatDetails = seats;
                
                // Calculate subtotal from prices after promotion
                decimal subtotal = seats.Sum(s => s.PriceAfterPromotion ?? s.Price);

                // Calculate rank discount
                decimal rankDiscount = 0;
                if (invoice.Account?.Rank != null && invoice.Account.Rank.DiscountPercentage.HasValue)
                {
                    var rankDiscountPercent = invoice.Account.Rank.DiscountPercentage.Value;
                    rankDiscount = subtotal * (rankDiscountPercent / 100m);
                }

                // Apply points used
                decimal usedScoreValue = (invoice.UseScore ?? 0) * 1000m;
                decimal totalPrice = subtotal - rankDiscount - usedScoreValue;
                if (totalPrice < 0) totalPrice = 0;

                ViewBag.Subtotal = subtotal;
                ViewBag.RankDiscount = rankDiscount;
                ViewBag.UsedScore = invoice.UseScore ?? 0;
                ViewBag.UsedScoreValue = usedScoreValue;
                ViewBag.AddScore = invoice.AddScore ?? 0;
                ViewBag.AddedScoreValue = (invoice.AddScore ?? 0) * 1000;
                ViewBag.TotalPrice = totalPrice;
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

                // Lấy lại invoice để truyền TempData cho Price Breakdown
                var invoice = _invoiceService.GetById(model.InvoiceId);
                if (invoice != null)
                {
                    TempData["MovieName"] = invoice.MovieName;
                    TempData["ShowDate"] = invoice.ScheduleShow?.ToString();
                    TempData["ShowTime"] = invoice.ScheduleShowTime;
                    TempData["Seats"] = invoice.Seat;
                    TempData["CinemaRoomName"] = invoice.ScheduleSeats.FirstOrDefault()?.MovieShow?.CinemaRoom?.CinemaRoomName;
                    TempData["InvoiceId"] = invoice.InvoiceId;
                    TempData["BookingTime"] = invoice.BookingDate?.ToString();

                    // Tính subtotal đúng từ SeatType
                    var scheduleSeats = invoice.ScheduleSeats?.ToList() ?? new List<ScheduleSeat>();
                    decimal subtotal = 0;
                    foreach (var ss in scheduleSeats)
                    {
                        if (ss.SeatId.HasValue)
                        {
                            var seat = _seatService.GetSeatById(ss.SeatId.Value);
                            if (seat != null && seat.SeatTypeId.HasValue)
                            {
                                var seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                                subtotal += seatType?.PricePercent ?? 0;
                            }
                        }
                    }
                    TempData["OriginalPrice"] = subtotal;
                    TempData["UsedScore"] = invoice.UseScore ?? 0;
                    TempData["FinalPrice"] = invoice.TotalMoney ?? 0;
                    // Tính lại rank discount nếu có
                    decimal usedScoreValue = (invoice.UseScore ?? 0) * 1000;
                    decimal totalPriceValue = invoice.TotalMoney ?? 0;
                    decimal rankDiscount = subtotal - usedScoreValue - totalPriceValue;
                    TempData["RankDiscount"] = rankDiscount;
                }
                return RedirectToAction("Failed");
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
                    _context.Invoices.Update(invoice);
                    _context.SaveChanges();
                }
                // Lấy danh sách ghế chi tiết
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
                ViewBag.SeatDetails = seats;
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

           var movieShow = _movieService.GetMovieShowById(movieShowId);
           if (movieShow == null)
           {
               return NotFound("Movie show not found.");
           }

           var movie = movieShow.Movie;
           var cinemaRoom = movieShow.CinemaRoom;
           var seatTypes = await _seatService.GetSeatTypesAsync();
           var seats = new List<SeatDetailViewModel>();

            // Get best promotion for this show date
            var bestPromotion = _promotionService.GetBestPromotionForShowDate(showDate);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;

            foreach (var id in selectedSeatIds)
            {
                var seat = _seatService.GetSeatById(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion
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
               MovieId = movie.MovieId,
               MovieName = movie.MovieNameEnglish,
               CinemaRoomName = cinemaRoom.CinemaRoomName,
               ShowDate = movieShow.ShowDate,
               ShowTime = movieShow.Schedule?.ScheduleTime?.ToString("HH:mm"),
               SelectedSeats = seats,
               TotalPrice = totalPrice,
               PricePerTicket = seats.Any() ? totalPrice / seats.Count : 0,
                MovieShowId = movieShowId,
                VersionName = movieShow.Version.VersionName
           };

           var adminConfirmUrl = Url.Action("ConfirmTicketForAdmin", "Admin");
           var viewModel = new ConfirmTicketAdminViewModel
           {
               BookingDetails = bookingDetails,
               MemberCheckMessage = "",
               ReturnUrl = Url.Action("Select", "Seat", new
               {
                   movieId = movie.MovieId,
                   date = movieShow.ShowDate.ToString("yyyy-MM-dd"),
                   time = movieShow.Schedule?.ScheduleTime?.ToString("HH:mm")
               }),
               MovieShowId = movieShowId
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
            try
            {
                if (model.BookingDetails == null || model.BookingDetails.SelectedSeats == null)
                {
                    return Json(new { success = false, message = "Booking details or selected seats are missing." });
                }

                if (string.IsNullOrEmpty(model.MemberId))
                {
                    return Json(new { success = false, message = "Member check is required before confirming." });
                }

                Member member = null;
                if (!string.IsNullOrEmpty(model.MemberId))
                {
                    member = _memberRepository.GetByMemberId(model.MemberId);
                    if (member == null)
                    {
                        return Json(new { success = false, message = "Member not found. Please check member details again." });
                    }
                }

                // Calculate subtotal from original seat prices
                decimal subtotal = model.BookingDetails.SelectedSeats.Sum(s => s.Price);

                // 1. Apply rank discount first
                decimal rankDiscount = 0;
                if (member?.Account?.Rank != null)
                {
                    var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                    rankDiscount = subtotal * (rankDiscountPercent / 100m);
                }
                decimal afterRank = subtotal - rankDiscount;
                if (afterRank < 0) afterRank = 0;

                // 2. Apply voucher (after rank)
                decimal voucherAmount = 0;
                if (!string.IsNullOrEmpty(model.SelectedVoucherId))
                {
                    var voucher = _voucherService.GetById(model.SelectedVoucherId);
                    if (voucher != null && voucher.AccountId == member.Account.AccountId && (voucher.IsUsed == null || voucher.IsUsed == false) && voucher.ExpiryDate > DateTime.Now)
                    {
                        voucherAmount = Math.Min(voucher.Value, afterRank);
                    }
                }
                decimal afterVoucher = afterRank - voucherAmount;
                if (afterVoucher < 0) afterVoucher = 0;

                // 3. Get promotion discount level from best promotion
                decimal promotionDiscountLevel = 0;
                var bestPromotion = _promotionService.GetBestPromotionForShowDate(model.BookingDetails.ShowDate);
                if (bestPromotion != null && bestPromotion.DiscountLevel.HasValue)
                {
                    promotionDiscountLevel = bestPromotion.DiscountLevel.Value;
                }

                // 4. Apply points
                int usedScore = model.UsedScore;
                decimal usedScoreValue = Math.Min(usedScore * 1000, afterVoucher); // Cap at price after discount
                decimal finalPrice = afterVoucher - usedScoreValue;
                if (finalPrice < 0) finalPrice = 0;

                // Calculate points to earn using the same logic as user booking
                decimal earningRate = member?.Account?.Rank?.PointEarningPercentage ?? 1;
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalPrice, earningRate);
                int addedScore = pointsToEarn;
                int addedScoreValue = addedScore * 1000;

                // Create invoice
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = member.Account.AccountId,
                    AddScore = pointsToEarn,
                    BookingDate = DateTime.Now,
                    MovieName = model.BookingDetails.MovieName,
                    ScheduleShow = model.BookingDetails.ShowDate,
                    ScheduleShowTime = model.BookingDetails.ShowTime,
                    Status = InvoiceStatus.Completed,
                    TotalMoney = finalPrice,
                    UseScore = usedScore,
                    Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                    VoucherId = !string.IsNullOrEmpty(model.SelectedVoucherId) ? model.SelectedVoucherId : null,
                    PromotionDiscount = (int?)promotionDiscountLevel,
                    MovieShowId = model.MovieShowId
                };

                // Save invoice
                await _bookingService.SaveInvoiceAsync(invoice);
                _accountService.CheckAndUpgradeRank(member.AccountId);

                // Update voucher if used
                if (voucherAmount > 0 && !string.IsNullOrEmpty(model.SelectedVoucherId))
                {
                    var voucher = _voucherService.GetById(model.SelectedVoucherId);
                    if (voucher != null)
                    {
                        voucher.IsUsed = true;
                        _voucherService.Update(voucher);
                    }
                }

                // Update seat statuses
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

                // Combine booking success and rank upgrade messages if both exist
                var messages = new List<string> { "Movie booked successfully!" };
                var rankUpMsg = HttpContext.Session.GetString("RankUpToastMessage");
                if (!string.IsNullOrEmpty(rankUpMsg))
                {
                    messages.Add(rankUpMsg);
                    HttpContext.Session.Remove("RankUpToastMessage");
                }
                TempData["ToastMessage"] = string.Join("<br/>", messages);

                // Store seat information in session for the confirmation view
                HttpContext.Session.SetString("ConfirmedSeats_" + invoice.InvoiceId, JsonConvert.SerializeObject(model.BookingDetails.SelectedSeats));
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
            {
                return NotFound();
            }

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
               VersionName = invoice.MovieShow.Version?.VersionName ?? "N/A",
               ShowDate = invoice.MovieShow.ShowDate,
               ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
               SelectedSeats = seats,
               TotalPrice = invoice.TotalMoney ?? 0,
               PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
               InvoiceId = invoice.InvoiceId,
               ScoreUsed = invoice.UseScore ?? 0,
               TicketsConverted = ticketsConverted > 0 ? ticketsConverted.ToString() : null
           };

            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            // Use TempData if present, otherwise recalculate
            decimal subtotal = TempData["Subtotal"] != null ? Convert.ToDecimal(TempData["Subtotal"]) : seats.Sum(s => s.Price);
            decimal rankDiscount = TempData["RankDiscount"] != null ? Convert.ToDecimal(TempData["RankDiscount"]) : 0;
            if (rankDiscount == 0 && member?.Account?.Rank != null)
            {
                var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            int usedScore = invoice.UseScore ?? 0;
            int usedScoreValue = TempData["UsedScoreValue"] != null ? Convert.ToInt32(TempData["UsedScoreValue"]) : (invoice.UseScore ?? 0) * 1000;
            int addedScore = TempData["AddedScore"] != null ? Convert.ToInt32(TempData["AddedScore"]) : (invoice.AddScore ?? 0);
            int addedScoreValue = TempData["AddedScoreValue"] != null ? Convert.ToInt32(TempData["AddedScoreValue"]) : (invoice.AddScore ?? 0) * 1000;
            // Calculate total price based on seat price after discount
            decimal voucherAmount = TempData["VoucherAmount"] != null ? Convert.ToDecimal(TempData["VoucherAmount"]) : 0;
            
            // If voucher amount is not in TempData, try to get it from the invoice's voucher
            if (voucherAmount == 0 && !string.IsNullOrEmpty(invoice.VoucherId))
            {
                var voucher = _voucherService.GetById(invoice.VoucherId);
                if (voucher != null)
                {
                    voucherAmount = voucher.Value;
                }
            }
            
            decimal totalPrice = subtotal - rankDiscount - voucherAmount - usedScoreValue;
            string memberId = TempData["MemberId"] as string ?? member?.MemberId;
            string memberEmail = TempData["MemberEmail"] as string ?? member?.Account?.Email;
            string memberIdentityCard = TempData["MemberIdentityCard"] as string ?? member?.Account?.IdentityCard;
            string memberPhone = TempData["MemberPhone"] as string ?? member?.Account?.PhoneNumber;

            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = memberId,
                MemberEmail = memberEmail,
                MemberIdentityCard = memberIdentityCard,
                MemberPhone = memberPhone,
                UsedScore = usedScore,
                UsedScoreValue = usedScoreValue,
                AddedScore = addedScore,
                AddedScoreValue = addedScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                VoucherAmount = voucherAmount,
                TotalPrice = totalPrice
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

            // Use robust navigation: get schedule seats and their related MovieShow and CinemaRoom
            var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();

            string roomName = "N/A";
            if (scheduleSeats.Any())
            {
                var movieShow = scheduleSeats.First().MovieShow;
                if (movieShow != null && movieShow.CinemaRoom != null)
                {
                    roomName = movieShow.CinemaRoom.CinemaRoomName;
                }
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);
            
            // Prepare seat details
            List<SeatDetailViewModel> seats = null;
            var sessionKey = "ConfirmedSeats_" + invoiceId;
            var seatsJson = HttpContext.Session.GetString(sessionKey);
            
            if (!string.IsNullOrEmpty(seatsJson))
            {
                seats = JsonConvert.DeserializeObject<List<SeatDetailViewModel>>(seatsJson);
            }
            else if (TempData["ConfirmedSeats"] != null)
            {
                seats = JsonConvert.DeserializeObject<List<SeatDetailViewModel>>(TempData["ConfirmedSeats"].ToString());
            }
            else
            {
                var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
                seats = new List<SeatDetailViewModel>();
                foreach (var seatName in seatNamesArr)
                {
                    var trimmedSeatName = seatName.Trim();
                    var seat = _seatService.GetSeatByName(seatName);
                    if (seat == null)
                    {
                        continue;
                    }
                    SeatType seatType = null;
                    if (seat.SeatTypeId.HasValue)
                    {
                        seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                    }

                    decimal originalPrice = seatType?.PricePercent ?? 0;
                    decimal promotionDiscount = 0;
                    decimal priceAfterPromotion = originalPrice;

                    // Calculate promotion discount if it exists in the invoice
                    if (invoice.PromotionDiscount.HasValue && invoice.PromotionDiscount.Value > 0)
                    {
                        promotionDiscount = Math.Round(originalPrice * (invoice.PromotionDiscount.Value / 100m));
                        priceAfterPromotion = originalPrice - promotionDiscount;
                    }

                    seats.Add(new SeatDetailViewModel
                    {
                        SeatId = seat.SeatId,
                        SeatName = trimmedSeatName,
                        SeatType = seatType?.TypeName ?? "N/A",
                        Price = priceAfterPromotion,
                        OriginalPrice = originalPrice,
                        PromotionDiscount = promotionDiscount,
                        PriceAfterPromotion = priceAfterPromotion
                    });
                }

                // Store the reconstructed seats in session for future use
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(seats));
            }

            // Calculate subtotal based on seat prices after promotion
            decimal subtotal = seats.Sum(s => s.Price);

            // Calculate rank discount
            decimal rankDiscount = 0;
            if (member?.Account?.Rank != null)
            {
                var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }

            // Get voucher amount if used
            decimal voucherAmount = 0;
            if (!string.IsNullOrEmpty(invoice.VoucherId))
            {
                var voucher = _voucherService.GetById(invoice.VoucherId);
                if (voucher != null)
                {
                    voucherAmount = voucher.Value;
                }
            }

            // Calculate final price
            int usedScore = invoice.UseScore ?? 0;
            int usedScoreValue = usedScore * 1000;
            int addedScore = invoice.AddScore ?? 0;
            int addedScoreValue = addedScore * 1000;
            decimal totalPrice = subtotal - rankDiscount - voucherAmount - usedScoreValue;
            if (totalPrice < 0) totalPrice = 0;

            string memberId = member?.MemberId;
            string memberEmail = member?.Account?.Email;
            string memberIdentityCard = member?.Account?.IdentityCard;
            string memberPhone = member?.Account?.PhoneNumber;
            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = invoice.MovieName,
                CinemaRoomName = roomName,
                ShowDate = invoice.ScheduleShow ?? DateTime.Now,
                ShowTime = invoice.ScheduleShowTime,
                SelectedSeats = seats,
                TotalPrice = totalPrice,
                PricePerTicket = seats.Any() ? totalPrice / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = usedScore,
                Status = invoice.Status ?? InvoiceStatus.Incomplete,
                AddScore = addedScore
            };

            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = memberId,
                MemberEmail = memberEmail,
                MemberIdentityCard = memberIdentityCard,
                MemberPhone = memberPhone,
                UsedScore = usedScore,
                UsedScoreValue = usedScoreValue,
                AddedScore = addedScore,
                AddedScoreValue = addedScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                VoucherAmount = voucherAmount,
                TotalPrice = totalPrice
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
                    account = new
                    {
                        fullName = m.Account?.FullName,
                        accountId = m.Account?.AccountId,
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

        [Authorize]
        [HttpGet]
        public IActionResult TicketDetails(string invoiceId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null || invoice.AccountId != userId)
                return NotFound();

            // Use robust navigation: get schedule seats and their related MovieShow and CinemaRoom
            var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();

            string roomName = "N/A";
            if (scheduleSeats.Any())
            {
                var movieShow = scheduleSeats.First().MovieShow;
                if (movieShow != null && movieShow.CinemaRoom != null)
                {
                    roomName = movieShow.CinemaRoom.CinemaRoomName;
                }
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);
            // Prepare seat details
            var seatNamesArr = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            var seats = new List<SeatDetailViewModel>();
            foreach (var seatName in seatNamesArr)
            {
                var trimmedSeatName = seatName.Trim();
                var seat = _seatService.GetSeatByName(seatName);
                if (seat == null)
                {
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

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                CinemaRoomName = invoice.MovieShow.CinemaRoom.CinemaRoomName,
                ShowDate = invoice.MovieShow.ShowDate,
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                Status = invoice.Status ?? InvoiceStatus.Incomplete,
                AddScore = invoice.AddScore ?? 0
            };

            string returnUrl = Url.Action("Index", "Ticket");

            // Calculate values
            decimal subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (member?.Account?.Rank != null)
            {
                var rankDiscountPercent = member.Account.Rank.DiscountPercentage ?? 0;
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            int usedScore = invoice.UseScore ?? 0;
            int usedScoreValue = usedScore * 1000;
            int addedScore = invoice.AddScore ?? 0;
            int addedScoreValue = addedScore * 1000;
            decimal totalPrice = invoice.TotalMoney ?? 0;
            string memberId = member?.MemberId;
            string memberEmail = member?.Account?.Email;
            string memberIdentityCard = member?.Account?.IdentityCard;
            string memberPhone = member?.Account?.PhoneNumber;

            var viewModel = new ConfirmTicketAdminViewModel
            {
                BookingDetails = bookingDetails,
                MemberCheckMessage = "",
                ReturnUrl = returnUrl,
                MemberId = memberId,
                MemberEmail = memberEmail,
                MemberIdentityCard = memberIdentityCard,
                MemberPhone = memberPhone,
                UsedScore = usedScore,
                UsedScoreValue = usedScoreValue,
                AddedScore = addedScore,
                AddedScoreValue = addedScoreValue,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                TotalPrice = totalPrice
            };

            return View("TicketDetails", viewModel);
        }
    }
}
