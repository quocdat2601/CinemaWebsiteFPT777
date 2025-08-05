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
        public List<int> SelectedSeatIds { get; set; } = new List<int>();
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
            _foodService = foodService;
            _foodInvoiceService = foodInvoiceService;
            _bookingDomainService = bookingDomainService;
            _promotionService = promotionService;
        }
        public string role => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        
       //GET: /api/booking/information
       /// <summary>
       /// Hi?n th? th�ng tin x�c nh?n d?t v�.
       /// </summary>
       /// <param name="movieId">Id phim du?c ch?n.</param>
       /// <param name="showDate">Ng�y chi?u.</param>
       /// <param name="showTime">Gi? chi?u.</param>
       /// <param name="selectedSeatIds">Danh s�ch gh? d� ch?n.</param>
       /// <param name="movieShowId">Id c?a su?t chi?u c? th?.</param>
       /// <returns>View x�c nh?n d?t v�.</returns>
       [HttpGet]
       [Authorize]
       public async Task<IActionResult> Information(string movieId, DateOnly showDate, string showTime, List<int>? selectedSeatIds, int movieShowId, List<int>? foodIds, List<int>? foodQtys)
       {
           if (selectedSeatIds == null || selectedSeatIds.Count == 0)
           {
               TempData["BookingError"] = "No seats were selected.";
               return RedirectToAction("Index", "Home", new { fragment = "booking-widget" });
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

            // Th�m th�ng tin SeatType cho promotion context
            if (selectedSeatIds != null)
            {
                var selectedSeats = _seatService.GetSeatsWithTypeByIds(selectedSeatIds);
                context.SelectedSeatTypeIds = selectedSeats.Select(s => s.SeatTypeId ?? 0).Distinct().ToList();
                context.SelectedSeatTypeNames = selectedSeats.Select(s => s.SeatType?.TypeName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
                context.SelectedSeatTypePricePercents = selectedSeats.Select(s => s.SeatType?.PricePercent ?? 0).Distinct().ToList();
            }
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
        /// X�c nh?n d?t v� (t�nh to�n gi�, luu invoice, chuy?n sang thanh to�n)
        /// </summary>
        /// <remarks>url: /Booking/Confirm (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Confirm(ConfirmBookingViewModel model)
        {
            Console.WriteLine($"[BookingController.Confirm] Starting confirmation process");
            Console.WriteLine($"[BookingController.Confirm] ModelState.IsValid: {ModelState.IsValid}");
            
            // if (!ModelState.IsValid)
            // {
            //     Console.WriteLine($"[BookingController.Confirm] ModelState is invalid, returning to view");
            //     return View("ConfirmBooking", model);
            // }
            var userId = _accountService.GetCurrentUser()?.AccountId;
            if (userId == null)
                return RedirectToAction("Login", "Account");

           var result = await _bookingDomainService.ConfirmBookingAsync(model, userId);
           Console.WriteLine($"[Controller] Model.SelectedVoucherId: {model.SelectedVoucherId}, Model.VoucherAmount: {model.VoucherAmount}");

            if (!result.Success)
            {
                Console.WriteLine($"[BookingController.Confirm] Booking failed: {result.ErrorMessage}");
                ModelState.AddModelError("", result.ErrorMessage);
                return View("ConfirmBooking", model);
            }

            Console.WriteLine($"[BookingController.Confirm] InvoiceId: {result.InvoiceId}");

           if (Math.Abs(result.TotalPrice) < 0.01m)
           {
               Console.WriteLine($"[BookingController.Confirm] Redirecting to Success page");
               return RedirectToAction("Success", new { invoiceId = result.InvoiceId });
           }
           else
           {
               Console.WriteLine($"[BookingController.Confirm] Redirecting to Payment page");
               return RedirectToAction("Payment", new { invoiceId = result.InvoiceId });
           }
       }

        /// <summary>
        /// Trang th�ng b�o d?t v� th�nh c�ng, c?ng/tr? di?m
        /// </summary>
        /// <remarks>url: /Booking/Success (GET)</remarks>
        [HttpGet]
        [Authorize]
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
                   await _invoiceService.MarkInvoiceAsCompletedAsync(invoiceId);
               }
               if (!string.IsNullOrEmpty(invoice.VoucherId))
               {
                   await _invoiceService.MarkVoucherAsUsedAsync(invoice.VoucherId);
               }
           }

           var viewModel = await _bookingDomainService.BuildSuccessViewModelAsync(invoiceId, userId);

           if (viewModel == null)
               return NotFound();

           return View("Success", viewModel);
       }

        /// <summary>
        /// Trang thanh to�n VNPay
        /// </summary>
        /// <remarks>url: /Booking/Payment (GET)</remarks>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Payment(string invoiceId)
        {
            Console.WriteLine($"[BookingController.Payment] Starting payment process for invoiceId: {invoiceId}");
            
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                Console.WriteLine($"[BookingController.Payment] Invoice not found for id: {invoiceId}");
                return NotFound();
            }

            Console.WriteLine($"[BookingController.Payment] Invoice found - TotalMoney: {invoice.TotalMoney}, Status: {invoice.Status}");

           // Redirect to Success if total is 0 (for 0 VND bookings)   
           if ((invoice.TotalMoney ?? 0) == 0)
           {
               Console.WriteLine($"[BookingController.Payment] TotalMoney is 0, redirecting to Success");
               return RedirectToAction("Success", new { invoiceId = invoice.InvoiceId });
           }

           Console.WriteLine($"[BookingController.Payment] Proceeding with payment page display");

           var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(invoiceId)).ToList();
           decimal totalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);

           // L?y promotion discount percent t? invoice
           int promotionDiscount = 0;
           if (!string.IsNullOrEmpty(invoice.PromotionDiscount) && invoice.PromotionDiscount != "0")
           {
               try
               {
                   var promoObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(invoice.PromotionDiscount);
                   promotionDiscount = (int)(promoObj.seat ?? 0);
               }
               catch { promotionDiscount = 0; }
           }

           // L?y version multiplier
           decimal versionMulti = 1;
           if (invoice.MovieShow?.Version != null)
           {
               versionMulti = (decimal)invoice.MovieShow.Version.Multi;
           }

           // T�nh l?i gi� seat sau gi?m (rank, voucher, points, promotion, version)
           var scheduleSeats = invoice.ScheduleSeats?.ToList() ?? new List<ScheduleSeat>();
           decimal subtotal = 0;
           if (scheduleSeats.Count == 0 && !string.IsNullOrEmpty(invoice.SeatIds))
           {
               var seatIds = invoice.SeatIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(id => int.Parse(id.Trim()))
                   .ToList();
               foreach (var seatId in seatIds)
               {
                   var seat = _seatService.GetSeatById(seatId);
                   if (seat != null && seat.SeatTypeId.HasValue)
                   {
                       var seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                       decimal basePrice = (seatType?.PricePercent ?? 0) * versionMulti;
                       if (promotionDiscount > 0)
                       {
                           decimal discount = Math.Round(basePrice * (promotionDiscount / 100m));
                           basePrice -= discount;
                       }
                       subtotal += basePrice;
                   }
               }
           }
           else
           {
               foreach (var ss in scheduleSeats)
               {
                   if (ss.SeatId.HasValue)
                   {
                       var seat = _seatService.GetSeatById(ss.SeatId.Value);
                       if (seat != null && seat.SeatTypeId.HasValue)
                       {
                           var seatType = _seatTypeService.GetById(seat.SeatTypeId.Value);
                           decimal basePrice = (seatType?.PricePercent ?? 0) * versionMulti;
                           if (promotionDiscount > 0)
                           {
                               decimal discount = Math.Round(basePrice * (promotionDiscount / 100m));
                               basePrice -= discount;
                           }
                           subtotal += basePrice;
                       }
                   }
               }
           }
           decimal rankDiscountPercent = invoice.RankDiscountPercentage ?? 0;
           decimal rankDiscount = subtotal * (rankDiscountPercent / 100m);
           decimal voucherAmount = 0;
           if (!string.IsNullOrEmpty(invoice.VoucherId))
           {
               var voucher = _voucherService.GetById(invoice.VoucherId);
               if (voucher != null)
               {
                   voucherAmount = voucher.Value;
               }
           }
           int usedScore = invoice.UseScore ?? 0;
           decimal usedScoreValue = usedScore * 1000;
           decimal seatAfterDiscounts = subtotal - rankDiscount - voucherAmount - usedScoreValue;
           if (seatAfterDiscounts < 0) seatAfterDiscounts = 0;
           decimal totalAmount = seatAfterDiscounts + totalFoodPrice;

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
               TotalSeatPrice = seatAfterDiscounts,
               Subtotal = subtotal,
               RankDiscount = rankDiscount,
               VoucherAmount = voucherAmount,
               UsedScoreValue = usedScoreValue
           };

           return View("Payment", viewModel);
       }

        /// <summary>
        /// X? l� t?o URL thanh to�n VNPay
        /// </summary>
        /// <remarks>url: /Booking/ProcessPayment (POST)</remarks>
        [HttpPost]
        [Authorize]
        public IActionResult ProcessPayment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Failed");
            }
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
               TempData["ErrorMessage"] = "C� l?i x?y ra khi t?o URL thanh to�n. Vui l�ng th? l?i sau.";

               // L?y l?i invoice d? truy?n TempData cho Price Breakdown
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

                   // T�nh subtotal d�ng t? SeatType
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
                   // T�nh l?i rank discount n?u c�
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
               TempData["VoucherAmount"] = voucherAmount.ToString();
               return RedirectToAction("Failed");
           }
       }

        /// <summary>
        /// Trang th�ng b�o thanh to�n th?t b?i
        /// </summary>
        /// <remarks>url: /Booking/Failed (GET)</remarks>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Failed()
        {
            var invoiceId = TempData["InvoiceId"] as string;
            var userId = _accountService.GetCurrentUser()?.AccountId;
            if (!string.IsNullOrEmpty(invoiceId) && userId != null)
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice != null && invoice.Status != InvoiceStatus.Incomplete)
                {
                    await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, InvoiceStatus.Incomplete);
                }
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                var viewModel = await _bookingDomainService.BuildSuccessViewModelAsync(invoiceId, userId);
                return View(viewModel);
            }
            // N?u kh�ng c� invoiceId ho?c userId, truy?n ViewModel r?ng d? tr�nh null
            return View(new BookingSuccessViewModel());
        }

       /// <summary>
       /// Trang x�c nh?n b�n v� cho admin (ch?n gh?, nh?p member...)
       /// </summary>
       /// <remarks>url: /Booking/ConfirmTicketForAdmin (GET)</remarks>
       [Authorize(Roles = "Admin, Employee")]
       [HttpGet]
       public async Task<IActionResult> ConfirmTicketForAdmin(int movieShowId, List<int>? selectedSeatIds, List<int>? foodIds = null, List<int>? foodQtys = null, string memberId = null, string accountId = null)
       {
           if (selectedSeatIds == null || selectedSeatIds.Count == 0)
           {
               TempData["ErrorMessage"] = "No seats were selected.";

               if (role == "Admin")
               return RedirectToAction("MainPage", "Admin", new { tab = "TicketSellingMg" });
               else
                   return RedirectToAction("MainPage", "Employee", new { tab = "TicketSellingMg" });
           }
           
           // Build l?i ViewModel v?i memberId n?u c�
           var viewModel = await _bookingDomainService.BuildConfirmTicketAdminViewModelAsync(
               movieShowId, selectedSeatIds, foodIds, foodQtys, memberId
           );
           
           if (viewModel == null)
           {
               TempData["ErrorMessage"] = "Unable to build confirmation view.";
               return RedirectToAction("MainPage", "Admin", new { tab = "TicketSellingMg" });
           }
           
           // N?u c� memberId, c?p nh?t th�ng tin member
           if (!string.IsNullOrEmpty(memberId))
           {
               var member = _memberRepository.GetByIdWithAccount(memberId);
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
       /// Ki?m tra th�ng tin member khi b�n v� cho admin
       /// </summary>
       /// <remarks>url: /Booking/CheckMemberDetails (POST)</remarks>
       [Authorize(Roles = "Admin, Employee")]
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
       /// X�c nh?n b�n v� cho admin (luu invoice, c?p nh?t di?m, tr?ng th�i gh?...)
       /// </summary>
       /// <remarks>url: /Booking/ConfirmTicketForAdmin (POST)</remarks>
       [Authorize(Roles = "Admin, Employee")]
       [HttpPost]
       public async Task<IActionResult> ConfirmTicketForAdmin([FromBody] ConfirmTicketAdminViewModel model)
       {
           _logger.LogInformation("ConfirmTicketForAdmin called");
           var result = await _bookingDomainService.ConfirmTicketForAdminAsync(model);
           if (!result.Success)
           {
               _logger.LogWarning("ConfirmTicketForAdmin failed: {ErrorMessage}", result.ErrorMessage);
               return Json(new { success = false, message = result.ErrorMessage });
           }
           
                       _logger.LogInformation("ConfirmTicketForAdmin successful, InvoiceId: {InvoiceId}", result.InvoiceId);
           await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
           return Json(new { success = true, redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { invoiceId = result.InvoiceId }) });
       }



       /// <summary>
       /// Trang x�c nh?n b�n v� th�nh c�ng cho admin
       /// </summary>
       /// <remarks>url: /Booking/TicketBookingConfirmed (GET)</remarks>
       [Authorize(Roles = "Admin, Employee")]
       [HttpGet]
       public async Task<IActionResult> TicketBookingConfirmed(string invoiceId)
       {
           //_logger.LogInformation($"TicketBookingConfirmed called with invoiceId: {invoiceId}");
           
           if (string.IsNullOrEmpty(invoiceId))
           {
               _logger.LogWarning("InvoiceId is null or empty");
               TempData["ErrorMessage"] = "Kh�ng c� th�ng tin invoice.";
               return RedirectToAction("MainPage", "Admin", new { tab = "BookingMg" });
           }
           
           //_logger.LogInformation($"Building view model for invoiceId: {invoiceId}");
           var viewModel = await _bookingDomainService.BuildTicketBookingConfirmedViewModelAsync(invoiceId);
           if (viewModel == null)
           {
               //_logger.LogWarning($"View model is null for invoiceId: {invoiceId}");
               TempData["ErrorMessage"] = "Kh�ng t�m th?y th�ng tin booking.";
               return RedirectToAction("MainPage", "Admin", new { tab = "BookingMg" });
           }
           
           //_logger.LogInformation($"Successfully built view model for invoiceId: {invoiceId}");
           return View("TicketBookingConfirmed", viewModel);
       }

       /// <summary>
       /// Ki?m tra di?m d? quy d?i v� cho admin
       /// </summary>
       /// <remarks>url: /Booking/CheckScoreForConversion (POST)</remarks>
       [Authorize(Roles = "Admin, Employee")]
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
      /// Xem th�ng tin v� (admin/employee)
      /// </summary>
      /// <remarks>url: /Booking/TicketInfo (GET)</remarks>
      [Authorize(Roles = "Admin, Employee")]
      [HttpGet]
      public async Task<IActionResult> TicketInfo(string invoiceId)
      {
          var viewModel = await _bookingDomainService.BuildTicketBookingConfirmedViewModelAsync(invoiceId);
          if (viewModel == null)
              return NotFound();
          return View("TicketBookingConfirmed", viewModel);
      }

      /// <summary>
      /// L?y danh s�ch member (admin)
      /// </summary>
      /// <remarks>url: /Booking/GetAllMembers (GET)</remarks>
      [Authorize(Roles = "Admin, Employee")]
      [HttpGet]
      public IActionResult GetAllMembers() // NOSONAR - GET methods don't require ModelState.IsValid check
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
        /// Kh?i t?o b�n v� cho member (admin)
        /// </summary>
        /// <remarks>url: /Booking/InitiateTicketSellingForMember/{id} (GET)</remarks>
        [Authorize(Roles = "Admin, Employee")]
        [HttpGet("Booking/InitiateTicketSellingForMember/{id}")]
        public IActionResult InitiateTicketSellingForMember(string id)
        {
            // Store the member's AccountId in TempData to use in the ticket selling process
            TempData["InitiateTicketSellingForMemberId"] = id;
            string returnUrl;
            if (role == "Admin")
            {
                returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            }
            else
            {
                returnUrl = Url.Action("MainPage", "Employee", new { tab = "TicketSellingMg" });
            }
            return RedirectToAction("Select", "Showtime", new { returnUrl = returnUrl, isAdminSell = "true" });
        }

        /// <summary>
        /// Trang booking cho admin/employee v?i Date, Version, Time selection (Quick Book)
        /// </summary>
        /// <remarks>url: /Booking/TicketBookingAdmin (GET)</remarks>
        [Authorize(Roles = "Admin, Employee")]
        [HttpGet]
        public IActionResult TicketBookingAdmin(string returnUrl) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            // Get all currently showing movies
            var currentlyShowingMovies = _movieService.GetCurrentlyShowingMovies();
            
            var viewModel = new TicketBookingAdminViewModel
            {
                Movies = currentlyShowingMovies,
                ReturnUrl = returnUrl
            };
            
            return View(viewModel);
        }

        /// <summary>
        /// Quick Book action cho admin/employee
        /// </summary>
        /// <remarks>url: /Booking/QuickBook (GET)</remarks>
        [Authorize(Roles = "Admin, Employee")]
        [HttpGet]
        public IActionResult QuickBook() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            string returnUrl;
            if (role == "Admin")
            {
                returnUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" });
            }
            else
            {
                returnUrl = Url.Action("MainPage", "Employee", new { tab = "TicketSellingMg" });
            }
            return RedirectToAction("TicketBookingAdmin", "Booking", new { returnUrl = returnUrl });
        }

        /// <summary>
        /// L?y discount v� earning rate c?a member (admin)
        /// </summary>
        /// <remarks>url: /Booking/GetMemberDiscount (GET)</remarks>
        [Authorize(Roles = "Admin, Employee")]
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
                _logger.LogInformation("GetMemberDiscount: memberId={MemberId}, discountPercent={DiscountPercent}, earningRate={EarningRate}", 
                    memberId, discountPercent, earningRate);
            }
            else
            {
                _logger.LogWarning("GetMemberDiscount: member or rank not found for memberId={MemberId}", memberId);
            }
            return Json(new { discountPercent, earningRate });
        }

      /// <summary>
      /// Reload page v?i member d� ch?n (admin)
      /// </summary>
      /// <remarks>url: /Booking/ReloadWithMember (POST)</remarks>
      [Authorize(Roles = "Admin, Employee")]
      [HttpPost]
      public async Task<IActionResult> ReloadWithMember([FromBody] ReloadWithMemberRequest request)
      {
          try
          {
              // Redirect d?n ConfirmTicketForAdmin v?i memberId
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
      /// L?y danh s�ch food
      /// </summary>
      /// <remarks>url: /Booking/GetFoods (GET)</remarks>
      [HttpGet]
      public async Task<IActionResult> GetFoods()
      {
          var foods = await _foodService.GetAllAsync();
          return Json(foods.Foods);
      }

      /// <summary>
      /// L?y danh s�ch promotion h?p l? cho member
      /// </summary>
      /// <remarks>url: /Booking/GetEligiblePromotions (POST)</remarks>
      [Authorize(Roles = "Admin, Employee")]
      [HttpPost]
      public async Task<IActionResult> GetEligiblePromotions([FromBody] GetEligiblePromotionsRequest request)
      {
          try
          {
              // L?y th�ng tin gh? d� ch?n t? request
              var selectedSeatIds = request.SelectedSeatIds ?? new List<int>();
              
              // L?y th�ng tin SeatType t? c�c gh? d� ch?n
              var selectedSeats = _seatService.GetSeatsWithTypeByIds(selectedSeatIds);

              var promotionContext = new PromotionCheckContext
              {
                  MemberId = request.MemberId,
                  SeatCount = selectedSeatIds.Count,
                  MovieId = request.MovieId,
                  MovieName = "", // S? du?c l?y t? movie service
                  ShowDate = DateTime.Parse(request.ShowDate),
                  SelectedSeatTypeIds = selectedSeats.Select(s => s.SeatTypeId ?? 0).Distinct().ToList(),
                  SelectedSeatTypeNames = selectedSeats.Select(s => s.SeatType?.TypeName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList(),
                  SelectedSeatTypePricePercents = selectedSeats.Select(s => s.SeatType?.PricePercent ?? 0).Distinct().ToList()
              };

              // L?y t?t c? promotion v� ki?m tra t?ng c�i
              var allPromotions = _promotionService.GetAll().Where(p => p.IsActive).ToList();
              var eligiblePromotions = new List<object>();

              foreach (var promotion in allPromotions)
              {
                  // Ki?m tra xem promotion c� h?p l? kh�ng
                  var context = new PromotionCheckContext
                  {
                      MemberId = request.MemberId,
                      SeatCount = selectedSeatIds.Count,
                      MovieId = request.MovieId,
                      MovieName = "",
                      ShowDate = DateTime.Parse(request.ShowDate),
                      SelectedSeatTypeIds = selectedSeats.Select(s => s.SeatTypeId ?? 0).Distinct().ToList(),
                      SelectedSeatTypeNames = selectedSeats.Select(s => s.SeatType?.TypeName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList(),
                      SelectedSeatTypePricePercents = selectedSeats.Select(s => s.SeatType?.PricePercent ?? 0).Distinct().ToList()
                  };

                  // S? d?ng logic t? PromotionService d? ki?m tra
                  if (_promotionService.IsPromotionEligible(promotion, context))
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

      [HttpGet]
      public IActionResult GetVersions(string movieId, string date) // NOSONAR - GET methods don't require ModelState.IsValid check
      {
          try
          {
              if (string.IsNullOrEmpty(movieId) || string.IsNullOrEmpty(date))
              {
                  return Json(new List<object>());
              }

              // Parse the date from YYYY-MM-DD format
              if (!DateOnly.TryParse(date, out DateOnly showDate))
              {
                  return Json(new List<object>());
              }

              // Get movie shows for the specific movie and date
              var movieShows = _movieService.GetMovieShowsByMovieId(movieId)
                  .Where(ms => ms.ShowDate == showDate)
                  .ToList();

              // Get unique versions from the movie shows
              var versions = movieShows
                  .Where(ms => ms.Version != null)
                  .Select(ms => new
                  {
                      versionId = ms.VersionId,
                      versionName = ms.Version.VersionName
                  })
                  .Distinct()
                  .OrderBy(v => v.versionName)
                  .ToList();

              return Json(versions);
          }
          catch (Exception ex)
          {
              _logger.LogError(ex, "Error getting versions for movie {MovieId} on date {Date}", movieId, date);
              return Json(new List<object>());
          }
      }

      [HttpGet]
      public IActionResult GetTimes(string movieId, string date, int versionId) // NOSONAR - GET methods don't require ModelState.IsValid check
      {
          try
          {
              if (string.IsNullOrEmpty(movieId) || string.IsNullOrEmpty(date) || versionId <= 0)
              {
                  return Json(new List<object>());
              }

              // Parse the date from YYYY-MM-DD format
              if (!DateOnly.TryParse(date, out DateOnly showDate))
              {
                  return Json(new List<object>());
              }

              // Get movie shows for the specific movie, date, and version
              var movieShows = _movieService.GetMovieShowsByMovieId(movieId)
                  .Where(ms => ms.ShowDate == showDate && ms.VersionId == versionId)
                  .ToList();

              // Get unique times from the movie shows
              var times = movieShows
                  .Where(ms => ms.Schedule?.ScheduleTime.HasValue == true)
                  .Select(ms => ms.Schedule.ScheduleTime.Value.ToString("HH:mm"))
                  .Distinct()
                  .OrderBy(t => t)
                  .ToList();

              return Json(times);
          }
          catch (Exception ex)
          {
              _logger.LogError(ex, "Error getting times for movie {MovieId} on date {Date} with version {VersionId}", movieId, date, versionId);
              return Json(new List<object>());
          }
      }
  }
}
