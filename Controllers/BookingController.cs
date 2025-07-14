using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using Microsoft.EntityFrameworkCore;

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
        private readonly MovieTheaterContext _context;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        private readonly IFoodService _foodService;
        private readonly IFoodInvoiceService _foodInvoiceService;

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
                         IHubContext<DashboardHub> dashboardHubContext,

                         MovieTheater.Models.MovieTheaterContext context,
                         IFoodService foodService,
                         IFoodInvoiceService foodInvoiceService)
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
            _dashboardHubContext = dashboardHubContext;
            _foodService = foodService;
            _foodInvoiceService = foodInvoiceService;
        }

        /// <summary>
        /// Trang chọn phim và suất chiếu để đặt vé
        /// </summary>
        /// <remarks>url: /Booking/TicketBooking (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> TicketBooking(string movieId = null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Get all movies with their shows
            var movies = await _bookingService.GetAvailableMoviesAsync();

            // Filter: Only movies with at least one show today or in the future
            var filteredMovies = movies
                .Where(m => _movieService.GetMovieShows(m.MovieId)
                    .Any(ms => ms.ShowDate >= today))
                .ToList();

            ViewBag.MovieList = filteredMovies;
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

        /// <summary>
        /// Lấy danh sách ngày chiếu cho một phim
        /// </summary>
        /// <remarks>url: /Booking/GetDates (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> GetDates(string movieId)
        {
            var dates = await _bookingService.GetShowDatesAsync(movieId);
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        /// <summary>
        /// Lấy danh sách giờ chiếu cho một phim vào ngày cụ thể
        /// </summary>
        /// <remarks>url: /Booking/GetTimes (GET)</remarks>
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
       public async Task<IActionResult> Information(string movieId, DateOnly showDate, string showTime, List<int>? selectedSeatIds, int movieShowId, List<int>? foodIds, List<int>? foodQtys)
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

           // Get the specific movie show by ID (fix: use movieShowId directly)
           var movieShow = _movieService.GetMovieShowById(movieShowId);
           if (movieShow == null)
           {
               return NotFound("Movie show not found for the specified ID.");
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
                var seat = _seatService.GetSeatById(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var basePrice = seatType?.PricePercent ?? 0;
                var versionMulti = movieShow.Version?.Multi ?? 1m;
                var price = basePrice * versionMulti;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;

                string promotionName = bestPromotion != null && promotionDiscountPercent > 0 ? bestPromotion.Title : null;

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    SeatTypeId = seatType?.SeatTypeId,
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = promotionName
                });
            }

            var originalTotal = seats.Sum(s => s.OriginalPrice ?? 0);
            var subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (userAccount?.Rank != null)
            {
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }
            var totalPriceSeats = subtotal - rankDiscount;

            // Xử lý food đã chọn
            List<FoodViewModel> selectedFoods = new List<FoodViewModel>();
            decimal totalFoodPrice = 0;
            if (foodIds != null && foodQtys != null && foodIds.Count == foodQtys.Count)
            {
                for (int i = 0; i < foodIds.Count; i++)
                {
                    var food = (await _foodService.GetByIdAsync(foodIds[i]));
                    if (food != null)
                    {
                        var foodClone = new FoodViewModel
                        {
                            FoodId = food.FoodId,
                            Name = food.Name,
                            Price = food.Price,
                            Image = food.Image,
                            Description = food.Description,
                            Category = food.Category,
                            Status = food.Status,
                            CreatedDate = food.CreatedDate,
                            UpdatedDate = food.UpdatedDate,
                            Quantity = foodQtys[i] // Số lượng món ăn đã chọn
                        };
                        selectedFoods.Add(foodClone);
                        totalFoodPrice += food.Price * foodQtys[i];
                    }
                }
            }

            ViewBag.OriginalTotal = originalTotal;
            ViewBag.Subtotal = subtotal;

            var viewModel = new ConfirmBookingViewModel
            {
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                CinemaRoomName = cinemaRoom.CinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                VersionName = movieShow.Version?.VersionName ?? "N/A",
                SelectedSeats = seats,
                Subtotal = subtotal,
                RankDiscount = rankDiscount,
                TotalPrice = totalPriceSeats + totalFoodPrice,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                IdentityCard = currentUser.IdentityCard,
                PhoneNumber = currentUser.PhoneNumber,
                CurrentScore = currentUser.Score,
                EarningRate = earningRate,
                RankDiscountPercent = rankDiscountPercent,
                MovieShowId = movieShowId,
                OriginalTotal = originalTotal
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };

           return View("ConfirmBooking", viewModel);
       }

        /// <summary>
        /// Xác nhận đặt vé (tính toán giá, lưu invoice, chuyển sang thanh toán)
        /// </summary>
        /// <remarks>url: /Booking/Confirm (POST)</remarks>
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
                var finalSeatPrice = afterVoucher - pointsValue;
                if (finalSeatPrice < 0) finalSeatPrice = 0;

                // Add food price to total
                decimal totalFoodPrice = model.SelectedFoods?.Sum(f => f.Price * f.Quantity) ?? 0;
                var finalPrice = finalSeatPrice + totalFoodPrice;

                var seatNames = model.SelectedSeats.Select(s => s.SeatName);
                string seatList = string.Join(",", seatNames);

                // Calculate points to earn using the same logic as user booking (only for seat price)
                decimal earningRate = userAccount?.Rank?.PointEarningPercentage ?? 1;
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalSeatPrice, earningRate);

                var movieShow = _movieService.GetMovieShowById(model.MovieShowId);
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = userId,
                    BookingDate = DateTime.Now,
                    Status = InvoiceStatus.Incomplete, // Chỉ set Incomplete khi vừa confirm
                    TotalMoney = finalPrice,
                    UseScore = model.UseScore,
                    Seat = string.Join(", ", model.SelectedSeats.Select(s => s.SeatName)),
                    VoucherId = !string.IsNullOrEmpty(model.SelectedVoucherId) ? model.SelectedVoucherId : null,
                    PromotionDiscount = (int?)promotionDiscountLevel,
                    RankDiscountPercentage = userAccount?.Rank?.DiscountPercentage ?? 0,
                    MovieShowId = model.MovieShowId,
                    MovieShow = movieShow
                };

                // Calculate earning rate from user rank
                decimal earningRateFromRank = 1;
                if (userAccount?.Rank != null)
                {
                    earningRateFromRank = userAccount.Rank.PointEarningPercentage ?? 1;
                }
                // Calculate points to earn using final price after all discounts
                int pointsToEarnFromRank = _pointService.CalculatePointsToEarn(finalPrice, earningRateFromRank);
                invoice.AddScore = pointsToEarnFromRank;

                await _bookingService.SaveInvoiceAsync(invoice);

                // Lưu MovieShowId vào TempData để PaymentController sử dụng
                TempData["MovieShowId"] = invoice.MovieShowId;

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
                            MovieShowId = invoice.MovieShowId,
                            InvoiceId = invoice.InvoiceId,
                            SeatId = (int)seat.SeatId,
                            SeatStatusId = 2,
                            BookedSeatTypeId = seat.SeatTypeId,
                            BookedPrice = seat.OriginalPrice
                        }).ToList();

                        // Log the values for debugging
                        foreach (var s in scheduleSeats)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] ScheduleSeat: SeatId={s.SeatId}, BookedSeatTypeId={s.BookedSeatTypeId}, BookedPrice={s.BookedPrice}");
                        }

                        await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                    }

                    await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");

                    TempData["MovieName"] = model.MovieName;
                    TempData["ShowDate"] = model.ShowDate.ToString();
                    TempData["ShowTime"] = model.ShowTime;
                    TempData["Seats"] = seatList;
                    TempData["CinemaRoomName"] = model.CinemaRoomName;
                    TempData["VersionName"] = invoice.MovieShow.Version.VersionName;
                    TempData["InvoiceId"] = invoice.InvoiceId;
                    TempData["BookingTime"] = invoice.BookingDate.ToString();
                    TempData["OriginalPrice"] = originalTotal.ToString();
                    TempData["UsedScore"] = model.UseScore.ToString();
                    TempData["FinalPrice"] = finalPrice.ToString();
                    return RedirectToAction("Success");
                }
                invoice = _invoiceService.GetById(invoice.InvoiceId);

                // Tạo ScheduleSeat cho trường hợp thanh toán bình thường
                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    var scheduleSeats = model.SelectedSeats.Select(seat => new ScheduleSeat
                    {
                        MovieShowId = invoice.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = (int)seat.SeatId,
                        SeatStatusId = 2,
                        BookedSeatTypeId = seat.SeatTypeId,
                        BookedPrice = seat.OriginalPrice * invoice.MovieShow.Version.Multi
                    }).ToList();

                    // Log the values for debugging
                    foreach (var s in scheduleSeats)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ScheduleSeat: SeatId={s.SeatId}, BookedSeatTypeId={s.BookedSeatTypeId}, BookedPrice={s.BookedPrice}");
                    }

                    await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
                }

                // After seat details and before returning the view, check for rank upgrade notification
                if (!string.IsNullOrEmpty(userId))
                {
                    var notificationMessage = _accountService.GetAndClearRankUpgradeNotification(userId);
                    if (!string.IsNullOrEmpty(notificationMessage))
                    {
                        var messages = new List<string>();
                        if (TempData["ToastMessage"] is string existingMessage)
                            messages.Add(existingMessage);
                        messages.Add(notificationMessage);
                        TempData["ToastMessage"] = string.Join("<br/>", messages);
                    }
                }

                // Save selected foods to session for payment
                if (model.SelectedFoods != null && model.SelectedFoods.Any())
                {
                    await _foodInvoiceService.SaveFoodOrderAsync(invoice.InvoiceId, model.SelectedFoods);
                }

                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
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

        /// <summary>
        /// Trang thông báo đặt vé thành công, cộng/trừ điểm
        /// </summary>
        /// <remarks>url: /Booking/Success (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var invoiceId = TempData["InvoiceId"] as string;
            if (!string.IsNullOrEmpty(invoiceId))
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status == InvoiceStatus.Completed)
                {
                    // --- Voucher and point logic after payment or test success ---
                    // Mark voucher as used
                    if (!string.IsNullOrEmpty(invoice.VoucherId))
                    {
                        var voucher = _voucherService.GetById(invoice.VoucherId);
                        if (voucher != null && (voucher.IsUsed == false))
                        {
                            voucher.IsUsed = true;
                            _voucherService.Update(voucher);
                        }
                    }
                    // Deduct used points
                    if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0)
                    {
                        await _accountService.DeductScoreAsync(invoice.AccountId, invoice.UseScore.Value);
                    }
                    // Add earned points
                    if (invoice.AddScore.HasValue && invoice.AddScore.Value > 0)
                    {
                        await _accountService.AddScoreAsync(invoice.AccountId, invoice.AddScore.Value);
                    }
                    _accountService.CheckAndUpgradeRank(userId);
                }

                // Get seat details from invoice.Seat_IDs (preferred) or fallback to seat names
                var seats = new List<SeatDetailViewModel>();
                var sessionKey = "ConfirmedSeats_" + invoiceId;
                var seatsJson = HttpContext.Session.GetString(sessionKey);

                if (!string.IsNullOrEmpty(seatsJson))
                {
                    seats = JsonConvert.DeserializeObject<List<SeatDetailViewModel>>(seatsJson);
                }

            else
                {
                    // Fallback: Use ScheduleSeat to get booked type/price
                    var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId)
                        .Select(ss => new {
                            ss.Seat,
                            BookedSeatType = ss.BookedSeatType ?? (ss.BookedSeatTypeId != null ? _seatTypeService.GetById(ss.BookedSeatTypeId.Value) : null),
                            ss.BookedPrice,
                            ss.SeatId
                        }).ToList();
                    foreach (var ss in scheduleSeats)
                    {
                        var seat = ss.Seat;
                        var seatType = ss.BookedSeatType;
                        decimal originalPrice = ss.BookedPrice ?? 0;
                        // Ensure originalPrice is version-multiplied if BookedPrice is missing or zero
                        if ((originalPrice == 0 || originalPrice == seatType?.PricePercent) && seatType != null && invoice?.MovieShow?.Version != null)
                        {
                            originalPrice = (decimal)(seatType.PricePercent * invoice.MovieShow.Version.Multi);
                        }
                        decimal seatPromotionDiscount = invoice.PromotionDiscount ?? 0;
                        decimal priceAfterPromotion = originalPrice;
                        if (seatPromotionDiscount > 0)
                        {
                            priceAfterPromotion = originalPrice * (1 - seatPromotionDiscount / 100m);
                        }
                        seats.Add(new SeatDetailViewModel
                        {
                            SeatId = seat?.SeatId ?? 0,
                            SeatName = seat?.SeatName ?? "",
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
                decimal subtotal = seats.Sum(s => s.OriginalPrice ?? 0);

                // Calculate rank discount
                decimal rankDiscount = 0;
                if (invoice.RankDiscountPercentage.HasValue && invoice.RankDiscountPercentage.Value > 0)
                {
                    var rankDiscountPercent = invoice.RankDiscountPercentage.Value;
                    rankDiscount = subtotal * (rankDiscountPercent / 100m);
                }

                decimal voucherAmount = 0;
                if (!string.IsNullOrEmpty(invoice.VoucherId))
                {
                    var voucher = _voucherService.GetById(invoice.VoucherId);
                    if (voucher != null)
                    {
                        voucherAmount = voucher.Value;
                    }
                }

                // Get food information from database
                var foodInvoicesList = await _context.FoodInvoices
                    .Include(fi => fi.Food)
                    .Where(fi => fi.InvoiceId == invoiceId)
                    .ToListAsync();

                var selectedFoodsList = foodInvoicesList.Select(fi => new FoodViewModel
                {
                    FoodId = fi.Food.FoodId,
                    Name = fi.Food.Name,
                    Price = fi.Price,
                    Quantity = fi.Quantity,
                    Image = fi.Food.Image,
                    Description = fi.Food.Description,
                    Category = fi.Food.Category,
                    Status = fi.Food.Status,
                    CreatedDate = fi.Food.CreatedDate,
                    UpdatedDate = fi.Food.UpdatedDate
                }).ToList();

                decimal totalFoodPrice = selectedFoodsList.Sum(f => f.Price * f.Quantity);

                ViewBag.SelectedFoods = selectedFoodsList;
                ViewBag.TotalFoodPrice = totalFoodPrice;

                // TotalPrice = subtotal - rankDiscount - voucherAmount + totalFoodPrice
                decimal totalPrice = subtotal - rankDiscount - voucherAmount + totalFoodPrice;
                if (totalPrice < 0) totalPrice = 0;
                ViewBag.TotalPrice = totalPrice;

                // Apply points used
                decimal usedScoreValue = (invoice.UseScore ?? 0) * 1000m;
                ViewBag.Subtotal = subtotal;
                ViewBag.RankDiscount = rankDiscount;
                ViewBag.UsedScore = invoice.UseScore ?? 0;
                ViewBag.UsedScoreValue = usedScoreValue;
                ViewBag.AddScore = invoice.AddScore ?? 0;
                ViewBag.AddedScoreValue = (invoice.AddScore ?? 0) * 1000;
                ViewBag.TotalPrice = totalPrice;
                ViewBag.PromotionDiscount = invoice.PromotionDiscount ?? 0;
                ViewBag.VoucherAmount = voucherAmount;
            

                // Get food information from database
                var foodInvoices = await _context.FoodInvoices
                    .Include(fi => fi.Food)
                    .Where(fi => fi.InvoiceId == invoiceId)
                    .ToListAsync();

                var selectedFoods = foodInvoices.Select(fi => new FoodViewModel
                {
                    FoodId = fi.Food.FoodId,
                    Name = fi.Food.Name,
                    Price = fi.Price,
                    Quantity = fi.Quantity,
                    Image = fi.Food.Image,
                    Description = fi.Food.Description,
                    Category = fi.Food.Category,
                    Status = fi.Food.Status,
                    CreatedDate = fi.Food.CreatedDate,
                    UpdatedDate = fi.Food.UpdatedDate
                }).ToList();

                ViewBag.SelectedFoods = selectedFoods;
                ViewBag.TotalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);
            }
            return View();
        }

        /// <summary>
        /// Trang thanh toán VNPay
        /// </summary>
        /// <remarks>url: /Booking/Payment (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> Payment(string invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            // Lấy food từ DB
            var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(invoiceId)).ToList();
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);
            decimal totalSeatPrice = invoice.TotalMoney ?? 0;
            decimal totalAmount = totalSeatPrice + totalFoodPrice;

            var sanitizedMovieName = Regex.Replace(invoice.MovieShow.Movie.MovieNameEnglish, @"[^a-zA-Z0-9\s]", "");
            var viewModel = new PaymentViewModel
            {
                InvoiceId = invoice.InvoiceId,
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                ShowDate = invoice.MovieShow.ShowDate,
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                Seats = invoice.Seat,
                TotalAmount = totalAmount,
                OrderInfo = $"Payment for movie ticket {sanitizedMovieName} - {invoice.Seat.Replace(",", " ")}",
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice,
                TotalSeatPrice = totalSeatPrice
            };

            return View("Payment", viewModel);
        }

        /// <summary>
        /// Xử lý tạo URL thanh toán VNPay
        /// </summary>
        /// <remarks>url: /Booking/ProcessPayment (POST)</remarks>
        [HttpPost]
        public IActionResult ProcessPayment(PaymentViewModel model)
        {
            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    (int)model.TotalAmount,
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
                    TempData["MovieName"] = invoice.MovieShow.Movie.MovieNameEnglish;
                    TempData["ShowDate"] = invoice.MovieShow.ShowDate;
                    TempData["ShowTime"] = invoice.MovieShow.Schedule.ScheduleTime.ToString();
                    TempData["Seats"] = invoice.Seat;
                    TempData["CinemaRoomName"] = invoice.ScheduleSeats.FirstOrDefault()?.MovieShow?.CinemaRoom?.CinemaRoomName;
                    TempData["VersionName"] = invoice.ScheduleSeats.FirstOrDefault()?.MovieShow?.Version?.VersionName;
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
                TempData["PromotionDiscount"] = invoice.PromotionDiscount ?? 0;
                decimal voucherAmount = 0;
                if (!string.IsNullOrEmpty(invoice.VoucherId))
                {
                    var voucher = _voucherService.GetById(invoice.VoucherId);
                    if (voucher != null)
                    {
                        voucherAmount = voucher.Value;
                    }
                }
                TempData["VoucherAmount"] = voucherAmount;
                return RedirectToAction("Failed");
            }
        }

        /// <summary>
        /// Trang thông báo thanh toán thất bại
        /// </summary>
        /// <remarks>url: /Booking/Failed (GET)</remarks>
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
                // Gửi realtime dashboard khi failed
                _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated").GetAwaiter().GetResult();
                // Giữ lại các trường cần thiết trong TempData để View sử dụng
                TempData.Keep("PromotionDiscount");
                TempData.Keep("VoucherAmount");
                TempData.Keep("RankDiscount");
                TempData.Keep("OriginalPrice");
                TempData.Keep("UsedScore");
                TempData.Keep("FinalPrice");
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

        /// <summary>
        /// Trang xác nhận bán vé cho admin (chọn ghế, nhập member...)
        /// </summary>
        /// <remarks>url: /Booking/ConfirmTicketForAdmin (GET)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ConfirmTicketForAdmin(int movieShowId, List<int>? selectedSeatIds, List<int>? foodIds, List<int>? foodQtys)
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
            var bestPromotion = _promotionService.GetBestPromotionForShowDate(movieShow.ShowDate);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;

            foreach (var id in selectedSeatIds)
            {
                var seat = _seatService.GetSeatById(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var basePrice = seatType?.PricePercent ?? 0;
                var versionMulti = movieShow.Version?.Multi ?? 1m;
                var price = basePrice * versionMulti;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;

                string promotionName = bestPromotion != null && promotionDiscountPercent > 0 ? bestPromotion.Title : null;

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    SeatTypeId = seatType?.SeatTypeId,
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = promotionName
                });
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
               VersionName = movieShow.Version.VersionName,
               VersionId = movieShow.Version.VersionId
           };

                       // Xử lý food đã chọn
            List<FoodViewModel> selectedFoods = new List<FoodViewModel>();
            decimal totalFoodPrice = 0;
            if (foodIds != null && foodQtys != null && foodIds.Count == foodQtys.Count)
            {
                for (int i = 0; i < foodIds.Count; i++)
                {
                    var food = (await _foodService.GetByIdAsync(foodIds[i]));
                    if (food != null)
                    {
                        var foodClone = new FoodViewModel
                        {
                            FoodId = food.FoodId,
                            Name = food.Name,
                            Price = food.Price,
                            Image = food.Image,
                            Description = food.Description,
                            Category = food.Category,
                            Status = food.Status,
                            CreatedDate = food.CreatedDate,
                            UpdatedDate = food.UpdatedDate,
                            Quantity = foodQtys[i] // Số lượng món ăn đã chọn
                        };
                        selectedFoods.Add(foodClone);
                        totalFoodPrice += food.Price * foodQtys[i];
                    }
                }
            }

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
               MovieShowId = movieShowId,
               SelectedFoods = selectedFoods,
               TotalFoodPrice = totalFoodPrice
           };
           ViewBag.MovieShow = movieShow;
           return View("ConfirmTicketAdmin", viewModel);
       }

        /// <summary>
        /// Kiểm tra thông tin member khi bán vé cho admin
        /// </summary>
        /// <remarks>url: /Booking/CheckMemberDetails (POST)</remarks>
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

        /// <summary>
        /// Xác nhận bán vé cho admin (lưu invoice, cập nhật điểm, trạng thái ghế...)
        /// </summary>
        /// <remarks>url: /Booking/ConfirmTicketForAdmin (POST)</remarks>
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

                // 4. Apply points (only for seats, not food)
                int usedScore = model.UsedScore;
                decimal usedScoreValue = Math.Min(usedScore * 1000, afterVoucher); // Cap at price after discount
                decimal finalSeatPrice = afterVoucher - usedScoreValue;
                if (finalSeatPrice < 0) finalSeatPrice = 0;

                // Add food price to total (food is not affected by any discounts)
                decimal totalFoodPrice = model.SelectedFoods?.Sum(f => f.Price * f.Quantity) ?? 0;
                var finalPrice = finalSeatPrice + totalFoodPrice;

                // Calculate points to earn using the same logic as user booking (only for seat price)
                decimal earningRate = member?.Account?.Rank?.PointEarningPercentage ?? 1;
                int pointsToEarn = _pointService.CalculatePointsToEarn(finalSeatPrice, earningRate);
                int addedScore = pointsToEarn;
                int addedScoreValue = addedScore * 1000;

                // Create invoice
                var movieShow = _movieService.GetMovieShowById(model.MovieShowId);
                var invoice = new Invoice
                {
                    InvoiceId = await _bookingService.GenerateInvoiceIdAsync(),
                    AccountId = member.Account.AccountId,
                    AddScore = pointsToEarn,
                    BookingDate = DateTime.Now,
                    Status = InvoiceStatus.Completed,
                    TotalMoney = finalSeatPrice, // Chỉ lưu seat price, không bao gồm food
                    UseScore = usedScore,
                    Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                    VoucherId = !string.IsNullOrEmpty(model.SelectedVoucherId) ? model.SelectedVoucherId : null,
                    PromotionDiscount = (int?)promotionDiscountLevel,
                    RankDiscountPercentage = member?.Account?.Rank?.DiscountPercentage ?? 0,
                    MovieShowId = model.MovieShowId,
                    MovieShow = movieShow
                };

                // Save invoice
                await _bookingService.SaveInvoiceAsync(invoice);
                // Update member's score
                if (pointsToEarn > 0) { 
                    await _accountService.AddScoreAsync(member.Account.AccountId, pointsToEarn);
                }
                if (usedScore > 0)
                {
                    await _accountService.DeductScoreAsync(member.Account.AccountId, usedScore);
                }
                _accountService.CheckAndUpgradeRank(member.AccountId);

                // Update voucher if used
                if (voucherAmount > 0 && !string.IsNullOrEmpty(model.SelectedVoucherId))
                {
                    var voucher = _voucherService.GetById(model.SelectedVoucherId);
                    if (voucher != null && (voucher.IsUsed == false))
                    {
                        voucher.IsUsed = true;
                        _voucherService.Update(voucher);
                    }
                }
                invoice = _invoiceService.GetById(invoice.InvoiceId);

                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    var scheduleSeats = model.BookingDetails.SelectedSeats.Select(seat => new ScheduleSeat
                    {
                        MovieShowId = invoice.MovieShowId,
                        InvoiceId = invoice.InvoiceId,
                        SeatId = (int)seat.SeatId,
                        SeatStatusId = 2,
                        BookedSeatTypeId = seat.SeatTypeId,
                        BookedPrice = seat.OriginalPrice
                    }).ToList();

                    // Log the values for debugging
                    foreach (var s in scheduleSeats)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ScheduleSeat: SeatId={s.SeatId}, BookedSeatTypeId={s.BookedSeatTypeId}, BookedPrice={s.BookedPrice}");
                    }

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

                // Save food orders if any
                if (model.SelectedFoods != null && model.SelectedFoods.Any())
                {
                    await _foodInvoiceService.SaveFoodOrderAsync(invoice.InvoiceId, model.SelectedFoods);
                }

                // Store seat information in session for the confirmation view
                HttpContext.Session.SetString("ConfirmedSeats_" + invoice.InvoiceId, JsonConvert.SerializeObject(model.BookingDetails.SelectedSeats));

                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                return Json(new { success = true, redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { invoiceId = invoice.InvoiceId }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Booking failed. Please try again later." });
            }
        }

        /// <summary>
        /// Trang xác nhận bán vé thành công cho admin
        /// </summary>
        /// <remarks>url: /Booking/TicketBookingConfirmed (GET)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> TicketBookingConfirmed(string invoiceId)
        {
            if (string.IsNullOrEmpty(invoiceId))
                return View("TicketBookingConfirmed");

            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            // Use navigation property to get MovieShow and related info
            var movieShow = invoice.MovieShow;
            if (movieShow == null)
            {
                return NotFound("Movie show not found for this invoice.");
            }

            var cinemaRoomName = movieShow.CinemaRoom?.CinemaRoomName ?? "N/A";
            var movieName = movieShow.Movie?.MovieNameEnglish ?? "N/A";
            var showDate = movieShow.ShowDate;
            var showTime = movieShow.Schedule?.ScheduleTime?.ToString() ?? "N/A";
            var versionName = movieShow.Version?.VersionName ?? "N/A";

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
                var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId)
                    .Select(ss => new {
                        ss.Seat,
                        BookedSeatType = ss.BookedSeatType ?? (ss.BookedSeatTypeId != null ? _seatTypeService.GetById(ss.BookedSeatTypeId.Value) : null),
                        ss.BookedPrice,
                        ss.SeatId
                    }).ToList();
                seats = new List<SeatDetailViewModel>();
                foreach (var ss in scheduleSeats)
                {
                    var seat = ss.Seat;
                    var seatType = ss.BookedSeatType;
                    decimal originalPrice = ss.BookedPrice ?? 0;
                    // Ensure originalPrice is version-multiplied if BookedPrice is missing or zero
                    if ((originalPrice == 0 || originalPrice == seatType?.PricePercent) && seatType != null && invoice?.MovieShow?.Version != null)
                    {
                        originalPrice = (decimal)(seatType.PricePercent * invoice.MovieShow.Version.Multi);
                    }
                    decimal seatPromotionDiscount = invoice.PromotionDiscount ?? 0;
                    decimal priceAfterPromotion = originalPrice;
                    if (seatPromotionDiscount > 0)
                    {
                        priceAfterPromotion = originalPrice * (1 - seatPromotionDiscount / 100m);
                    }
                    seats.Add(new SeatDetailViewModel
                    {
                        SeatId = seat?.SeatId ?? 0,
                        SeatName = seat?.SeatName ?? "",
                        SeatType = seatType?.TypeName ?? "N/A",
                        SeatTypeId = seatType?.SeatTypeId,
                        Price = priceAfterPromotion,
                        OriginalPrice = originalPrice,
                        PromotionDiscount = seatPromotionDiscount,
                        PriceAfterPromotion = priceAfterPromotion
                    });
                }
            }
            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = movieName,
                CinemaRoomName = cinemaRoomName,
                ShowDate = showDate,
                ShowTime = showTime,
                VersionName = versionName,
                SelectedSeats = seats,
                TotalPrice = invoice.TotalMoney ?? 0,
                PricePerTicket = seats.Any() ? (invoice.TotalMoney ?? 0) / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = invoice.UseScore ?? 0,
                Status = invoice.Status ?? InvoiceStatus.Incomplete, // Ensure status is set from DB
                AddScore = invoice.AddScore ?? 0
            };

            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            // Use TempData if present, otherwise recalculate
            decimal subtotal = TempData["Subtotal"] != null ? Convert.ToDecimal(TempData["Subtotal"]) : seats.Sum(s => s.OriginalPrice ?? 0);
            decimal rankDiscount = TempData["RankDiscount"] != null ? Convert.ToDecimal(TempData["RankDiscount"]) : 0;
            if (rankDiscount == 0 && invoice.RankDiscountPercentage.HasValue && invoice.RankDiscountPercentage.Value > 0)
            {
                var rankDiscountPercent = invoice.RankDiscountPercentage ?? 0;
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

            // Lấy thông tin food từ FoodInvoice
            var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(invoiceId)).ToList();
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);

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
                TotalPrice = totalPrice,
                RankDiscountPercent = invoice.RankDiscountPercentage ?? 0,
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };

            return View("TicketBookingConfirmed", viewModel);
        }

        /// <summary>
        /// Kiểm tra điểm để quy đổi vé cho admin
        /// </summary>
        /// <remarks>url: /Booking/CheckScoreForConversion (POST)</remarks>
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

        /// <summary>
        /// Xem thông tin vé (admin/employee)
        /// </summary>
        /// <remarks>url: /Booking/TicketInfo (GET)</remarks>
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> TicketInfo(string invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
                return NotFound();

            // Use robust navigation: get schedule seats and their related MovieShow and CinemaRoom
            var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();

            var member = _memberRepository.GetByAccountId(invoice.AccountId);

            // Prepare seat details using BookedPrice and BookedSeatType for historical accuracy
            var seats = scheduleSeats.Select(ss => {
                var seat = ss.Seat;
                var seatType = ss.BookedSeatType ?? (ss.BookedSeatTypeId != null ? _seatTypeService.GetById(ss.BookedSeatTypeId.Value) : null);
                decimal originalPrice = ss.BookedPrice ?? 0;
                // Only apply version multiplier if BookedPrice is null or 0
                if ((originalPrice == 0 || originalPrice == null) && seatType != null && invoice?.MovieShow?.Version != null)
                {
                    originalPrice = (decimal)(seatType.PricePercent * invoice.MovieShow.Version.Multi);
                }
                decimal seatPromotionDiscount = invoice.PromotionDiscount ?? 0;
                decimal priceAfterPromotion = originalPrice;
                if (seatPromotionDiscount > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - seatPromotionDiscount / 100m);
                }
                return new SeatDetailViewModel
                {
                    SeatId = seat?.SeatId ?? 0,
                    SeatName = seat?.SeatName ?? "",
                    SeatType = seatType?.TypeName ?? "N/A",
                    SeatTypeId = seatType?.SeatTypeId,
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = seatPromotionDiscount,
                    PriceAfterPromotion = priceAfterPromotion
                };
            }).ToList();

            // Calculate subtotal from original prices at booking
            decimal subtotal = seats.Sum(s => s.OriginalPrice ?? 0);

            // Calculate rank discount
            decimal rankDiscount = 0;
            if (member?.Account?.Rank != null)
            {
                var rankDiscountPercent = invoice.RankDiscountPercentage ?? 0;
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
            decimal totalPrice = invoice.TotalMoney ?? 0;

            string memberId = member?.MemberId;
            string memberEmail = member?.Account?.Email;
            string memberIdentityCard = member?.Account?.IdentityCard;
            string memberPhone = member?.Account?.PhoneNumber;
            string returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });

            var bookingDetails = new ConfirmBookingViewModel
            {
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                CinemaRoomName = invoice.MovieShow.CinemaRoom.CinemaRoomName,
                ShowDate = invoice.MovieShow.ShowDate,
                VersionName = invoice.MovieShow.Version?.VersionName ?? "N/A",
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                SelectedSeats = seats,
                TotalPrice = totalPrice,
                PricePerTicket = seats.Any() ? totalPrice / seats.Count : 0,
                InvoiceId = invoice.InvoiceId,
                ScoreUsed = usedScore,
                Status = invoice.Status ?? InvoiceStatus.Incomplete,
                AddScore = addedScore
            };

            // Lấy thông tin food từ FoodInvoice
            var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(invoiceId)).ToList();
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);

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
                TotalPrice = totalPrice,
                RankDiscountPercent = invoice.RankDiscountPercentage ?? 0,
                SelectedFoods = selectedFoods,
                TotalFoodPrice = totalFoodPrice
            };

            return View("TicketBookingConfirmed", viewModel);
        }

        /// <summary>
        /// Lấy danh sách member (admin)
        /// </summary>
        /// <remarks>url: /Booking/GetAllMembers (GET)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAllMembers()
        {
            var members = _memberRepository.GetAll()
                .Select(m => new
                {
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

        /// <summary>
        /// Khởi tạo bán vé cho member (admin)
        /// </summary>
        /// <remarks>url: /Booking/InitiateTicketSellingForMember/{id} (GET)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpGet("Booking/InitiateTicketSellingForMember/{id}")]
        public IActionResult InitiateTicketSellingForMember(string id)
        {
            // Store the member's AccountId in TempData to use in the ticket selling process
            TempData["InitiateTicketSellingForMemberId"] = id;

            var returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            return RedirectToAction("Select", "Showtime", new { returnUrl = returnUrl });
        }

        /// <summary>
        /// Lấy discount và earning rate của member (admin)
        /// </summary>
        /// <remarks>url: /Booking/GetMemberDiscount (GET)</remarks>
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

        /// <summary>
        /// Xem chi tiết vé của user
        /// </summary>
        /// <remarks>url: /Booking/TicketDetails (GET)</remarks>
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
            // Prepare seat details using BookedPrice and BookedSeatType for historical accuracy
            var seats = scheduleSeats.Select(ss => {
                var seat = ss.Seat;
                var seatType = ss.BookedSeatType ?? (ss.BookedSeatTypeId != null ? _seatTypeService.GetById(ss.BookedSeatTypeId.Value) : null);
                decimal originalPrice = ss.BookedPrice ?? 0;
                // Ensure originalPrice is version-multiplied if BookedPrice is missing or zero
                if ((originalPrice == 0 || originalPrice == seatType?.PricePercent) && seatType != null && invoice?.MovieShow?.Version != null)
                {
                    originalPrice = (decimal)(seatType.PricePercent * invoice.MovieShow.Version.Multi);
                }
                decimal seatPromotionDiscount = invoice.PromotionDiscount ?? 0;
                decimal priceAfterPromotion = originalPrice;
                if (seatPromotionDiscount > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - seatPromotionDiscount / 100m);
                }
                return new SeatDetailViewModel
                {
                    SeatId = seat?.SeatId ?? 0,
                    SeatName = seat?.SeatName ?? "",
                    SeatType = seatType?.TypeName ?? "N/A",
                    SeatTypeId = seatType?.SeatTypeId,
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = seatPromotionDiscount,
                    PriceAfterPromotion = priceAfterPromotion
                };
            }).ToList();

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
            decimal subtotal = seats.Sum(s => s.OriginalPrice ?? 0);
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
                TotalPrice = totalPrice,
                RankDiscountPercent = invoice.RankDiscountPercentage ?? 0
            };

            return View("TicketDetails", viewModel);
        }

        /// <summary>
        /// Lấy danh sách food
        /// </summary>
        /// <remarks>url: /Booking/GetFoods (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> GetFoods()
        {
            var foods = await _foodService.GetAllAsync();
            return Json(foods.Foods);
        }
    }
}
