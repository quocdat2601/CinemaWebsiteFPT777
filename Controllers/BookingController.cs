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
using static MovieTheater.Service.PromotionService;

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

    public class ReloadWithMemberRequest
    {
        public int MovieShowId { get; set; }
        public List<int> SelectedSeatIds { get; set; }
        public List<int>? FoodIds { get; set; }
        public List<int>? FoodQtys { get; set; }
        public string MemberId { get; set; }
    }

    public class GetEligiblePromotionsRequest
    {
        public string MovieId { get; set; }
        public string ShowDate { get; set; }
        public string ShowTime { get; set; }
        public string MemberId { get; set; }
        public string AccountId { get; set; }
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
        private readonly ILogger<BookingController> _logger;
        private readonly IVNPayService _vnPayService;
        private readonly IVoucherService _voucherService;
        private readonly MovieTheaterContext _context;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        private readonly IFoodService _foodService;
        private readonly IFoodInvoiceService _foodInvoiceService;
        private readonly IBookingDomainService _bookingDomainService;
        private readonly IPromotionService _promotionService;
        public BookingController(
            IBookingService bookingService,
            IMovieService movieService,
            ISeatService seatService,
            IAccountService accountService,
            ISeatTypeService seatTypeService,
            IMemberRepository memberRepository,
            ILogger<BookingController> logger,
            IInvoiceService invoiceService,
            IVNPayService vnPayService,
            IVoucherService voucherService,
            IHubContext<DashboardHub> dashboardHubContext,
            MovieTheaterContext context,
            IFoodService foodService,
            IFoodInvoiceService foodInvoiceService,
            IBookingDomainService bookingDomainService,
            IPromotionService promotionService)
        {
            _bookingService = bookingService;
            _movieService = movieService;
            _seatService = seatService;
            _accountService = accountService;
            _seatTypeService = seatTypeService;
            _memberRepository = memberRepository;
            _logger = logger;
            _invoiceService = invoiceService;
            _vnPayService = vnPayService;
            _voucherService = voucherService;
            _dashboardHubContext = dashboardHubContext;
            _context = context;
            _foodService = foodService;
            _foodInvoiceService = foodInvoiceService;
            _bookingDomainService = bookingDomainService;
            _promotionService = promotionService;
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

           var userId = _accountService.GetCurrentUser()?.AccountId;
           if (userId == null)
               return RedirectToAction("Login", "Account");

            // Reload account with rank
            var userAccount = _accountService.GetById(userId);

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
            var movie = _movieService.GetById(movieId);
            var context = new PromotionCheckContext {
                MemberId = userId,
                SeatCount = selectedSeatIds.Count,
                MovieId = movieId,
                MovieName = movie.MovieNameEnglish,
                ShowDate = showDate.ToDateTime(TimeOnly.MinValue)
            };
            var bestPromotion = _promotionService.GetBestEligiblePromotionForBooking(context);
            decimal promotionDiscountPercent = bestPromotion?.DiscountLevel ?? 0;

            var seatTypes = await _seatService.GetSeatTypesAsync();
            var seats = new List<SeatDetailViewModel>();
            foreach (var id in selectedSeatIds)
            {
                var seat = _seatService.GetSeatById(id);
                if (seat == null) continue;

                var seatType = seatTypes.FirstOrDefault(t => t.SeatTypeId == seat.SeatTypeId);
                var price = seatType?.PricePercent ?? 0;
                decimal discount = Math.Round(price * (promotionDiscountPercent / 100m));
                decimal priceAfterPromotion = price - discount;

                string promotionName = bestPromotion != null && promotionDiscountPercent > 0 ? bestPromotion.Title : null;

                seats.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Standard",
                    Price = priceAfterPromotion,
                    OriginalPrice = price,
                    PromotionDiscount = discount,
                    PriceAfterPromotion = priceAfterPromotion,
                    PromotionName = promotionName
                });
            }

            var subtotal = seats.Sum(s => s.Price);
            decimal rankDiscount = 0;
            if (userAccount?.Rank != null)
            {
                rankDiscount = subtotal * (rankDiscountPercent / 100m);
            }

           var viewModel = await _bookingDomainService.BuildConfirmBookingViewModelAsync(
               movieId, showDate, showTime, selectedSeatIds, movieShowId, foodIds, foodQtys, userId);

           if (viewModel == null)
               return NotFound();

           return View("ConfirmBooking", viewModel);
       }

        /// <summary>
        /// Xác nhận đặt vé (tính toán giá, lưu invoice, chuyển sang thanh toán)
        /// </summary>
        /// <remarks>url: /Booking/Confirm (POST)</remarks>
        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model, string IsTestSuccess)
        {
            var userId = _accountService.GetCurrentUser()?.AccountId;
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var result = await _bookingDomainService.ConfirmBookingAsync(model, userId, IsTestSuccess);
            Console.WriteLine($"[Controller] Model.SelectedVoucherId: {model.SelectedVoucherId}, Model.VoucherAmount: {model.VoucherAmount}");

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage);
                return View("ConfirmBooking", model);
            }

            // Debug log for IsTestSuccess value
            Console.WriteLine($"[BookingController.Confirm] IsTestSuccess: '{IsTestSuccess}', TotalPrice: {result.TotalPrice}");

            if (IsTestSuccess == "true" || Math.Abs(result.TotalPrice) < 0.01m)
            {
                return RedirectToAction("Success", new { invoiceId = result.InvoiceId });
            }
            else
            {
                return RedirectToAction("Payment", new { invoiceId = result.InvoiceId });
            }
        }

        /// <summary>
        /// Trang thông báo đặt vé thành công, cộng/trừ điểm
        /// </summary>
        /// <remarks>url: /Booking/Success (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> Success(string invoiceId)
        {
            var userId = _accountService.GetCurrentUser()?.AccountId;
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Mark voucher as used if present and not already used (for 0 VND transactions)
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice != null)
            {
                // Ensure status is set to Completed for 0 VND bookings
                if (invoice.Status != InvoiceStatus.Completed)
                {
                    invoice.Status = InvoiceStatus.Completed;
                    _context.Invoices.Update(invoice);
                    _context.SaveChanges();
                }
                if (!string.IsNullOrEmpty(invoice.VoucherId))
                {
                    var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == invoice.VoucherId);
                    if (voucher != null && (voucher.IsUsed == false))
                    {
                        voucher.IsUsed = true;
                        _context.Vouchers.Update(voucher);
                        _context.SaveChanges();
                    }
                }
            }

            var viewModel = await _bookingDomainService.BuildSuccessViewModelAsync(invoiceId, userId);

            if (viewModel == null)
                return NotFound();

            return View("Success", viewModel);
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

            // Redirect to Success if total is 0 (for 0 VND bookings)   
            if ((invoice.TotalMoney ?? 0) == 0)
            {
                return RedirectToAction("Success", new { invoiceId = invoice.InvoiceId });
            }

            var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(invoiceId)).ToList();
            decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);
            decimal totalSeatPrice = (invoice.TotalMoney ?? 0) - totalFoodPrice;
            if (totalSeatPrice < 0) totalSeatPrice = 0; 
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
                int promotionDiscount = 0;
                if (!string.IsNullOrEmpty(invoice.PromotionDiscount) && invoice.PromotionDiscount != "0")
                {
                    try
                    {
                        var promoObj = JsonConvert.DeserializeObject<dynamic>(invoice.PromotionDiscount);
                        promotionDiscount = (int)(promoObj.seat ?? 0);
                    }
                    catch { promotionDiscount = 0; }
                }
                TempData["PromotionDiscount"] = promotionDiscount;
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
        public async Task<IActionResult> Failed()
        {
            var invoiceId = TempData["InvoiceId"] as string;
            var userId = _accountService.GetCurrentUser()?.AccountId;
            if (!string.IsNullOrEmpty(invoiceId) && userId != null)
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status != InvoiceStatus.Incomplete)
                {
                    invoice.Status = InvoiceStatus.Incomplete;
                    invoice.UseScore = 0;
                    _context.Invoices.Update(invoice);
                    _context.SaveChanges();
                }
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                var viewModel = await _bookingDomainService.BuildSuccessViewModelAsync(invoiceId, userId);
                return View(viewModel);
            }
            // Nếu không có invoiceId hoặc userId, truyền ViewModel rỗng để tránh null
            return View(new BookingSuccessViewModel());
        }

        /// <summary>
        /// Trang xác nhận bán vé cho admin (chọn ghế, nhập member...)
        /// </summary>
        /// <remarks>url: /Booking/ConfirmTicketForAdmin (GET)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ConfirmTicketForAdmin(int movieShowId, List<int>? selectedSeatIds, List<int>? foodIds = null, List<int>? foodQtys = null, string memberId = null, string accountId = null)
        {
            if (selectedSeatIds == null || selectedSeatIds.Count == 0)
            {
                TempData["ErrorMessage"] = "No seats were selected.";
                return RedirectToAction("MainPage", "Admin", new { tab = "TicketSellingMg" });
            }
            
            // Build lại ViewModel với memberId nếu có
            var viewModel = await _bookingDomainService.BuildConfirmTicketAdminViewModelAsync(
                movieShowId, selectedSeatIds, foodIds, foodQtys, memberId
            );
            
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Unable to build confirmation view.";
                return RedirectToAction("MainPage", "Admin", new { tab = "TicketSellingMg" });
            }
            
            // Nếu có memberId, cập nhật thông tin member
            if (!string.IsNullOrEmpty(memberId))
            {
                var member = _context.Members.Include(m => m.Account).FirstOrDefault(m => m.MemberId == memberId);
                if (member != null)
                {
                    viewModel.MemberId = member.MemberId;
                    viewModel.MemberFullName = member.Account?.FullName;
                    viewModel.MemberIdentityCard = member.Account?.IdentityCard;
                    viewModel.MemberPhoneNumber = member.Account?.PhoneNumber;
                    viewModel.MemberScore = member.Score ?? 0;
                    viewModel.MemberAccountId = member.AccountId;
                }
            }
            
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
            _logger.LogInformation("ConfirmTicketForAdmin called");
            var result = await _bookingDomainService.ConfirmTicketForAdminAsync(model);
            if (!result.Success)
            {
                _logger.LogWarning($"ConfirmTicketForAdmin failed: {result.ErrorMessage}");
                return Json(new { success = false, message = result.ErrorMessage });
            }
            
            _logger.LogInformation($"ConfirmTicketForAdmin successful, InvoiceId: {result.InvoiceId}");
            await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
            return Json(new { success = true, redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { invoiceId = result.InvoiceId }) });
        }



        /// <summary>
        /// Trang xác nhận bán vé thành công cho admin
        /// </summary>
        /// <remarks>url: /Booking/TicketBookingConfirmed (GET)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> TicketBookingConfirmed(string invoiceId)
        {
            _logger.LogInformation($"TicketBookingConfirmed called with invoiceId: {invoiceId}");
            
            if (string.IsNullOrEmpty(invoiceId))
            {
                _logger.LogWarning("InvoiceId is null or empty");
                TempData["ErrorMessage"] = "Không có thông tin invoice.";
                return RedirectToAction("MainPage", "Admin", new { tab = "BookingMg" });
            }
            
            _logger.LogInformation($"Building view model for invoiceId: {invoiceId}");
            var viewModel = await _bookingDomainService.BuildTicketBookingConfirmedViewModelAsync(invoiceId);
            if (viewModel == null)
            {
                _logger.LogWarning($"View model is null for invoiceId: {invoiceId}");
                TempData["ErrorMessage"] = "Không tìm thấy thông tin booking.";
                return RedirectToAction("MainPage", "Admin", new { tab = "BookingMg" });
            }
            
            _logger.LogInformation($"Successfully built view model for invoiceId: {invoiceId}");
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
            var viewModel = await _bookingDomainService.BuildTicketBookingConfirmedViewModelAsync(invoiceId);
            if (viewModel == null)
                return NotFound();
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
        /// Reload page với member đã chọn (admin)
        /// </summary>
        /// <remarks>url: /Booking/ReloadWithMember (POST)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ReloadWithMember([FromBody] ReloadWithMemberRequest request)
        {
            try
            {
                // Redirect đến ConfirmTicketForAdmin với memberId
                var url = Url.Action("ConfirmTicketForAdmin", "Booking", new
                {
                    movieShowId = request.MovieShowId,
                    selectedSeatIds = request.SelectedSeatIds,
                    foodIds = request.FoodIds,
                    foodQtys = request.FoodQtys,
                    memberId = request.MemberId
                });

                return Json(new { success = true, redirectUrl = url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReloadWithMember");
                return Json(new { success = false, message = "Error reloading page with member" });
            }
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

        /// <summary>
        /// Lấy danh sách promotion hợp lệ cho member
        /// </summary>
        /// <remarks>url: /Booking/GetEligiblePromotions (POST)</remarks>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> GetEligiblePromotions([FromBody] GetEligiblePromotionsRequest request)
        {
            try
            {
                // Sử dụng method hiện có để lấy promotion hợp lệ
                var promotionContext = new PromotionCheckContext
                {
                    MemberId = request.MemberId,
                    SeatCount = 1, // Giả sử 1 ghế để kiểm tra
                    MovieId = request.MovieId,
                    MovieName = "", // Sẽ được lấy từ movie service
                    ShowDate = DateTime.Parse(request.ShowDate)
                };

                // Lấy tất cả promotion và kiểm tra từng cái
                var allPromotions = _promotionService.GetAll().Where(p => p.IsActive).ToList();
                var eligiblePromotions = new List<object>();

                foreach (var promotion in allPromotions)
                {
                    // Kiểm tra xem promotion có hợp lệ không
                    var context = new PromotionCheckContext
                    {
                        MemberId = request.MemberId,
                        SeatCount = 1,
                        MovieId = request.MovieId,
                        MovieName = "",
                        ShowDate = DateTime.Parse(request.ShowDate)
                    };

                    // Sử dụng logic từ PromotionService để kiểm tra
                    if (IsPromotionEligible(promotion, context))
                    {
                        eligiblePromotions.Add(new
                        {
                            id = promotion.PromotionId,
                            name = promotion.Title,
                            value = promotion.DiscountLevel ?? 0,
                            type = "percentage",
                            expirationDate = promotion.EndTime?.ToString("dd/MM/yyyy") ?? "No expiration"
                        });
                    }
                }

                return Json(new { 
                    success = true, 
                    eligiblePromotions = eligiblePromotions 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting eligible promotions");
                return Json(new { 
                    success = false, 
                    message = "Error getting eligible promotions" 
                });
            }
        }

        private bool IsPromotionEligible(Promotion promotion, PromotionCheckContext context)
        {
            if (promotion.PromotionConditions == null || !promotion.PromotionConditions.Any()) 
                return true;

            foreach (var condition in promotion.PromotionConditions)
            {
                switch (condition.TargetField?.ToLower())
                {
                    case "seat":
                        if (!int.TryParse(condition.TargetValue, out int seatTarget)) return false;
                        switch (condition.Operator)
                        {
                            case ">=": if (!(context.SeatCount >= seatTarget)) return false; break;
                            case "==": case "=": if (!(context.SeatCount == seatTarget)) return false; break;
                            case "<=": if (!(context.SeatCount <= seatTarget)) return false; break;
                            case "<": if (!(context.SeatCount < seatTarget)) return false; break;
                            case "!=": if (!(context.SeatCount != seatTarget)) return false; break;
                            default: return false;
                        }
                        break;
                    case "accountid":
                        // Nếu chưa chọn member, loại bỏ promotion này
                        if (string.IsNullOrEmpty(context.MemberId)) return false;
                        // Lấy accountId từ memberId
                        var member = _context.Members.FirstOrDefault(m => m.MemberId == context.MemberId);
                        if (member == null || string.IsNullOrEmpty(member.AccountId)) return false;
                        var accountId = member.AccountId;
                        // Kiểm tra trong bảng Invoice
                        var invoices = _context.Invoices.Where(i => i.AccountId == accountId);
                        if (string.IsNullOrEmpty(condition.TargetValue))
                        {
                            // Nếu targetValue là null, kiểm tra có invoice nào có AccountId đúng bằng accountId không
                            if (invoices.Any(i => i.AccountId == accountId)) return false;
                            break;
                        }
                        switch (condition.Operator)
                        {
                            case "=": case "==":
                                if (!invoices.Any(i => i.AccountId != null && i.AccountId.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase))) return false;
                                break;
                            case "!=":
                                if (invoices.Any(i => i.AccountId != null && i.AccountId.Equals(condition.TargetValue, StringComparison.OrdinalIgnoreCase))) return false;
                                break;
                            default:
                                return false;
                        }
                        break;
                }
            }
            return true;
        }
    }
}
