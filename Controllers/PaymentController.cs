using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly IVNPayService _vnPayService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IAccountService _accountService;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        private readonly IFoodInvoiceService _foodInvoiceService;
        private readonly IInvoiceService _invoiceService;
        private readonly IVoucherService _voucherService;
        private readonly ISeatService _seatService;
        private readonly IScheduleSeatService _scheduleSeatService;
        private readonly IMemberService _memberService;

        public PaymentController(
            IVNPayService vnPayService,
            ILogger<PaymentController> logger,
            IAccountService accountService,
            IHubContext<DashboardHub> dashboardHubContext,
            IFoodInvoiceService foodInvoiceService,
            IInvoiceService invoiceService,
            IVoucherService voucherService,
            ISeatService seatService,
            IScheduleSeatService scheduleSeatService,
            IMemberService memberService
        )
        {
            _vnPayService = vnPayService;
            _logger = logger;
            _accountService = accountService;
            _dashboardHubContext = dashboardHubContext;
            _foodInvoiceService = foodInvoiceService;
            _invoiceService = invoiceService;
            _voucherService = voucherService;
            _seatService = seatService;
            _scheduleSeatService = scheduleSeatService;
            _memberService = memberService;
        }

        /// <summary>
        /// Tạo URL thanh toán VNPay
        /// </summary>
        /// <remarks>url: /api/Payment/create-payment (POST)</remarks>
        /// <param name="request">Thông tin thanh toán</param>
        /// <returns>URL thanh toán VNPay</returns>
        /// <response code="200">Trả về URL thanh toán</response>
        /// <response code="400">Nếu có lỗi xảy ra</response>
        [HttpPost("create-payment")]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public IActionResult CreatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    request.Amount,
                    request.OrderInfo,
                    request.OrderId
                );

                return Ok(new { paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xử lý kết quả thanh toán từ VNPay
        /// </summary>
        /// <remarks>url: /api/Payment/vnpay-return (GET)</remarks>
        /// <returns>Kết quả thanh toán</returns>
        /// <response code="200">Thanh toán thành công</response>
        /// <response code="400">Thanh toán thất bại hoặc chữ ký không hợp lệ</response>
        [HttpGet("vnpay-return")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> VNPayReturn([FromQuery] VnPayReturnModel model)
        {
            // Kiểm tra null cho model
            if (model == null)
            {
                _logger.LogError("VnPayReturnModel is null");
                return RedirectToAction("Failed", "Booking");
            }

            int? movieShowId = null; // Khai báo duy nhất ở đây
            var invoice = _invoiceService.GetById(model.vnp_TxnRef);
            if (model.vnp_ResponseCode == "00")
            {
                // Thanh toán thành công
                if (invoice != null)
                {
                    invoice.Status = Models.InvoiceStatus.Completed;
                    // Mark voucher as used if present and not already used
                    if (!string.IsNullOrEmpty(invoice.VoucherId))
                    {
                        await _voucherService.MarkVoucherAsUsedAsync(invoice.VoucherId);
                    }
                    // If no ScheduleSeat records exist, create them (like admin flow)
                    var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                    var allSeats = _seatService.GetSeatsByNames(seatNames);
                    var existingScheduleSeats = _scheduleSeatService.GetByInvoiceId(invoice.InvoiceId)
                        .Select(ss => ss.SeatId)
                        .ToHashSet();

                    var newScheduleSeats = allSeats
                        .Where(seat => !existingScheduleSeats.Contains(seat.SeatId))
                        .Select(seat =>
                        {
                            var seatType = seat.SeatType;
                            decimal basePrice = seatType?.PricePercent ?? 0;
                            if (invoice.MovieShow?.Version != null)
                                basePrice *= (decimal)invoice.MovieShow.Version.Multi;
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
                            decimal discount = Math.Round(basePrice * (promotionDiscount / 100m));
                            decimal priceAfterPromotion = basePrice - discount;
                            return new MovieTheater.Models.ScheduleSeat
                            {
                                MovieShowId = invoice.MovieShowId,
                                InvoiceId = invoice.InvoiceId,
                                SeatId = seat.SeatId,
                                SeatStatusId = 2
                            };
                        }).ToList();

                    if (newScheduleSeats.Any())
                    {
                        await _scheduleSeatService.CreateMultipleScheduleSeatsAsync(newScheduleSeats);
                    }
                    // Update BookedPrice for all ScheduleSeat records after VNPay payment
                    var scheduleSeats = _scheduleSeatService.GetByInvoiceId(invoice.InvoiceId);
                    if (scheduleSeats != null && scheduleSeats.Any())
                    {
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
                        foreach (var scheduleSeat in scheduleSeats)
                        {
                            var seatType = scheduleSeat.Seat.SeatType;
                            if (seatType != null)
                            {
                                decimal basePrice = seatType.PricePercent;
                                if (invoice.MovieShow?.Version != null)
                                    basePrice *= (decimal)invoice.MovieShow.Version.Multi;
                                decimal discount = Math.Round(basePrice * (promotionDiscount / 100m));
                                decimal priceAfterPromotion = basePrice - discount;
                                scheduleSeat.BookedPrice = priceAfterPromotion;
                                _scheduleSeatService.Update(scheduleSeat);
                            }
                        }
                        _scheduleSeatService.Save();
                    }
                    if (invoice.AddScore == 0)
                    {
                        // Fetch member's earning rate
                        var member = _memberService.GetByIdWithAccountAndRank(invoice.AccountId);
                        decimal earningRate = 1;
                        if (member?.Account?.Rank != null)
                            earningRate = member.Account.Rank.PointEarningPercentage ?? 1;

                        // Calculate points based on seat price only (not including food)
                        var selectedFoodsList = new List<FoodViewModel>();
                        var foodsJson = HttpContext.Session.GetString("SelectedFoods_" + invoice.InvoiceId);
                        if (!string.IsNullOrEmpty(foodsJson))
                        {
                            try
                            {
                                selectedFoodsList = JsonConvert.DeserializeObject<List<FoodViewModel>>(foodsJson) ?? new List<FoodViewModel>();
                            }
                            catch
                            {
                                selectedFoodsList = new List<FoodViewModel>();
                            }
                        }

                        decimal totalFoodPrice = selectedFoodsList.Sum(f => f.Price * f.Quantity);
                        decimal seatOnlyPrice = (invoice.TotalMoney ?? 0) - totalFoodPrice;
                        int addScore = new MovieTheater.Service.PointService().CalculatePointsToEarn(seatOnlyPrice, earningRate);
                        invoice.AddScore = addScore;
                    }
                    _invoiceService.Update(invoice);
                    _invoiceService.Save();
                    _accountService.CheckAndUpgradeRank(invoice.AccountId);
                    _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated").GetAwaiter().GetResult();
                }
                if (invoice != null)
                {
                    // Cập nhật trạng thái ghế thành booked (SeatStatusId = 2)
                    await _scheduleSeatService.UpdateScheduleSeatsStatusAsync(invoice.InvoiceId, 2);

                    var seatHubContext = (IHubContext<MovieTheater.Hubs.SeatHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<MovieTheater.Hubs.SeatHub>));
                    var scheduleSeats = _scheduleSeatService.GetByInvoiceId(invoice.InvoiceId);
                    foreach (var scheduleSeat in scheduleSeats)
                    {
                        // Gửi thông báo realtime cập nhật trạng thái ghế
                        if (seatHubContext != null && scheduleSeat.SeatId.HasValue)
                        {
                            await seatHubContext.Clients.Group(invoice.MovieShowId.ToString()).SendAsync("SeatStatusChanged", scheduleSeat.SeatId.Value, 2);
                        }
                    }
                }

                // --- Lưu food orders nếu có ---
                var selectedFoods = HttpContext.Session.GetString("SelectedFoods_" + invoice.InvoiceId);
                if (!string.IsNullOrEmpty(selectedFoods))
                {
                    try
                    {
                        var foods = JsonConvert.DeserializeObject<List<FoodViewModel>>(selectedFoods);
                        if (foods != null && foods.Any())
                        {
                            await _foodInvoiceService.SaveFoodOrderAsync(invoice.InvoiceId, foods);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving food orders for invoice {InvoiceId}", invoice.InvoiceId);
                    }
                }

                TempData["InvoiceId"] = model.vnp_TxnRef;
                TempData["MovieName"] = invoice?.MovieShow?.Movie?.MovieNameEnglish ?? "";
                TempData["ShowDate"] = invoice?.MovieShow?.ShowDate.ToString("dd/MM/yyyy") ?? "N/A";
                TempData["ShowTime"] = invoice?.MovieShow?.Schedule?.ScheduleTime?.ToString() ?? "N/A";
                TempData["Seats"] = invoice?.Seat ?? "N/A";
                TempData["CinemaRoomName"] = invoice?.MovieShow?.CinemaRoom?.CinemaRoomName ?? "N/A";
                TempData["VersionName"] = invoice?.MovieShow?.Version?.VersionName ?? "N/A";
                TempData["BookingTime"] = invoice?.BookingDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
                // Build seat details for display in Success.cshtml
                if (invoice != null && !string.IsNullOrEmpty(invoice.SeatIds))
                {
                    var seatIds = invoice.SeatIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                    var allSeats = _seatService.GetSeatsWithTypeByIds(seatIds);
                    var seatDetails = allSeats.Select(seat =>
                    {
                        var seatType = seat.SeatType;
                        decimal basePrice = seatType?.PricePercent ?? 0;
                        if (invoice.MovieShow?.Version != null)
                            basePrice *= (decimal)invoice.MovieShow.Version.Multi;
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
                        decimal discount = Math.Round(basePrice * (promotionDiscount / 100m));
                        decimal priceAfterPromotion = basePrice - discount;
                        return new MovieTheater.ViewModels.SeatDetailViewModel
                        {
                            SeatId = seat.SeatId,
                            SeatName = seat.SeatName,
                            SeatType = seatType?.TypeName ?? "N/A",
                            Price = priceAfterPromotion,
                            OriginalPrice = basePrice,
                            PromotionDiscount = promotionDiscount,
                            PriceAfterPromotion = priceAfterPromotion
                        };
                    }).ToList();
                    TempData["SeatDetails"] = JsonConvert.SerializeObject(seatDetails);
                }
                TempData["OriginalPrice"] = invoice?.ScheduleSeats?.Sum(ss => ss.BookedPrice ?? 0).ToString() ?? "0";
                TempData["UsedScore"] = invoice?.UseScore ?? 0;
                TempData["FinalPrice"] = (invoice?.TotalMoney ?? 0).ToString();
                return RedirectToAction("Success", "Booking", new { invoiceId = model.vnp_TxnRef });
            }
            else
            {
                if (invoice != null)
                {
                    invoice.Status = MovieTheater.Models.InvoiceStatus.Incomplete;
                    _invoiceService.Update(invoice);
                    _invoiceService.Save();
                }
                TempData["InvoiceId"] = model.vnp_TxnRef;
                TempData["MovieName"] = invoice?.MovieShow?.Movie?.MovieNameEnglish ?? "";
                TempData["ShowDate"] = invoice?.MovieShow?.ShowDate.ToString("dd/MM/yyyy") ?? "N/A";
                TempData["ShowTime"] = invoice?.MovieShow?.Schedule?.ScheduleTime.ToString() ?? "N/A";
                TempData["Seats"] = invoice?.Seat ?? "N/A";
                TempData["BookingTime"] = invoice?.BookingDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                return RedirectToAction("Failed", "Booking");
            }
        }

        /// <summary>
        /// Nhận callback IPN (server-to-server) từ VNPay
        /// </summary>
        /// <remarks>url: /api/Payment/vnpay-ipn (GET)</remarks>
        /// <returns>Kết quả xử lý IPN</returns>
        [HttpGet("vnpay-ipn")]
        public IActionResult VNPayIpn()
        {
            var vnpResponse = HttpContext.Request.Query;
            var vnpayData = vnpResponse
                .Where(x => x.Key.StartsWith("vnp_"))
                .ToDictionary(x => x.Key, x => x.Value.ToString());

            var vnpSecureHash = vnpResponse["vnp_SecureHash"].ToString();

            // Validate signature
            bool checkSignature = _vnPayService.ValidateSignature(vnpayData, vnpSecureHash);

            if (!checkSignature)
                return Content("97"); // Sai chữ ký

            // TODO: Xử lý logic cập nhật DB, kiểm tra trạng thái đơn hàng, v.v.
            // Nếu thành công:
            return Content("00");
            // Nếu lỗi khác:
            // return Content("99");
        }
    }

    public class PaymentRequest
    {
        /// <summary>
        /// Số tiền thanh toán (VND)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Thông tin đơn hàng
        /// </summary>
        public string OrderInfo { get; set; }

        /// <summary>
        /// Mã đơn hàng
        /// </summary>
        public string OrderId { get; set; }
    }
}