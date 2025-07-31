using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using MovieTheater.Models;
using MovieTheater.Hubs;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QRPaymentController : Controller
    {
        private readonly IQRPaymentService _qrPaymentService;
        private readonly IGuestInvoiceService _guestInvoiceService;
        private readonly IBookingService _bookingService;
        private readonly ILogger<QRPaymentController> _logger;
        private readonly MovieTheaterContext _context;
        private readonly IHubContext<SeatHub> _seatHubContext;

        public QRPaymentController(IQRPaymentService qrPaymentService, IGuestInvoiceService guestInvoiceService, IBookingService bookingService, ILogger<QRPaymentController> logger, MovieTheaterContext context, IHubContext<SeatHub> seatHubContext)
        {
            _qrPaymentService = qrPaymentService;
            _guestInvoiceService = guestInvoiceService;
            _bookingService = bookingService;
            _logger = logger;
            _context = context;
            _seatHubContext = seatHubContext;
        }

        /// <summary>
        /// Tạo QR code cho thanh toán Guest
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateQRCode(string modelData)
        {
            try
            {
                var model = JsonSerializer.Deserialize<ConfirmTicketAdminViewModel>(modelData);
                var context = (MovieTheaterContext)HttpContext.RequestServices.GetService(typeof(MovieTheaterContext));
                var bookingService = (IBookingService)HttpContext.RequestServices.GetService(typeof(IBookingService));

                // Ensure GUEST account exists
                var guestAccount = context.Accounts.FirstOrDefault(a => a.AccountId == "GUEST");
                if (guestAccount == null)
                {
                    guestAccount = new Account
                    {
                        AccountId = "GUEST",
                        Email = "guest@movietheater.com",
                        FullName = "Khách vãng lai",
                        Password = "guest",
                        Address = "Không xác định",
                    };
                    context.Accounts.Add(guestAccount);
                    context.SaveChanges();
                }

                // 1. Sinh mã OrderId duy nhất cho QR Payment (DH = Đơn Hàng)
                string invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                while (context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                }

                // Tổng tiền = sử dụng giá đã được tính toán từ frontend
                decimal totalAmount = model.TotalPrice > 0 ? model.TotalPrice : (model.BookingDetails.TotalPrice + model.TotalFoodPrice);
                var orderInfo = $"Ve xem phim - {model.BookingDetails.MovieName}";

                // 2. Lưu invoice vào DB
                if (!context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    var invoice = new Invoice
                    {
                        InvoiceId = invoiceId,
                        AccountId = "GUEST",
                        AddScore = 0,
                        BookingDate = DateTime.Now,
                        Status = InvoiceStatus.Incomplete, // Pending
                        TotalMoney = totalAmount,
                        UseScore = 0,
                        Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                        SeatIds = string.Join(",", model.BookingDetails.SelectedSeats.Select(s => s.SeatId)),
                        MovieShowId = model.BookingDetails.MovieShowId,
                        PromotionDiscount = "0",
                        VoucherId = null, // Guest không có voucher
                        RankDiscountPercentage = 0
                    };
                    context.Invoices.Add(invoice);
                    context.SaveChanges();
                }

                // 3. Tạo QR code với reference là invoiceId
                var qrData = _qrPaymentService.GenerateQRCodeData(totalAmount, orderInfo, invoiceId);
                var qrImage = _qrPaymentService.GetQRCodeImage(qrData);

                // Tạo ScheduleSeat records và extend hold time for selected seats when creating QR code
                var movieShowId = model.BookingDetails.MovieShowId;
                var accountId = "GUEST"; // Since this is guest booking
                foreach (var seat in model.BookingDetails.SelectedSeats)
                {
                    if (seat.SeatId.HasValue)
                    {
                        // Tạo hoặc cập nhật ScheduleSeat record
                        var existingScheduleSeat = context.ScheduleSeats
                            .FirstOrDefault(ss => ss.MovieShowId == movieShowId && ss.SeatId == seat.SeatId.Value);
                        
                        if (existingScheduleSeat != null)
                        {
                            // Cập nhật existing record
                            existingScheduleSeat.InvoiceId = invoiceId;
                            existingScheduleSeat.SeatStatusId = 1; // 1 = Held
                            context.ScheduleSeats.Update(existingScheduleSeat);
                        }
                        else
                        {
                            // Tạo mới ScheduleSeat record
                            var scheduleSeat = new ScheduleSeat
                            {
                                MovieShowId = movieShowId,
                                InvoiceId = invoiceId,
                                SeatId = seat.SeatId.Value,
                                SeatStatusId = 1, // 1 = Held
                                BookedPrice = seat.Price
                            };
                            context.ScheduleSeats.Add(scheduleSeat);
                        }
                        
                        // Extend hold time
                        SeatHub.ExtendHoldTime(movieShowId, seat.SeatId.Value, accountId);
                    }
                }
                
                // Save changes for ScheduleSeat records
                context.SaveChanges();

                var viewModel = new QRPaymentViewModel
                {
                    OrderId = invoiceId,
                    Amount = totalAmount,
                    OrderInfo = orderInfo,
                    QRCodeData = qrData,
                    QRCodeImage = qrImage,
                    ExpiredTime = DateTime.Now.AddMinutes(15),
                    CustomerName = model.MemberFullName,
                    CustomerPhone = model.MemberPhoneNumber,
                    MovieName = model.BookingDetails.MovieName,
                    ShowTime = $"{model.BookingDetails.ShowDate:dd/MM/yyyy} {model.BookingDetails.ShowTime}",
                    SeatInfo = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName))
                };
                return RedirectToAction("DisplayQR", "QRPayment", new {
                    orderId = invoiceId,
                    amount = totalAmount,
                    customerName = model.MemberFullName,
                    customerPhone = model.MemberPhoneNumber,
                    movieName = model.BookingDetails.MovieName,
                    showTime = $"{model.BookingDetails.ShowDate:dd/MM/yyyy} {model.BookingDetails.ShowTime}",
                    seatInfo = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR code");
                return Json(new { success = false, message = "Error creating QR code: " + ex.Message });
            }
        }

        /// <summary>
        /// Tạo QR code cho thanh toán Member
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateQRCodeForMember(string modelData)
        {
            try
            {
                var model = JsonSerializer.Deserialize<ConfirmTicketAdminViewModel>(modelData);
                var context = (MovieTheaterContext)HttpContext.RequestServices.GetService(typeof(MovieTheaterContext));
                var bookingService = (IBookingService)HttpContext.RequestServices.GetService(typeof(IBookingService));

                _logger.LogInformation("CreateQRCodeForMember called with AccountId: {AccountId}, SelectedVoucherId: {SelectedVoucherId}", 
                    model.AccountId, model.SelectedVoucherId);
                _logger.LogInformation("TotalPrice from frontend: {TotalPrice}", model.TotalPrice);
                _logger.LogInformation("BookingDetails.TotalPrice: {BookingTotalPrice}", model.BookingDetails?.TotalPrice);
                _logger.LogInformation("TotalFoodPrice: {TotalFoodPrice}", model.TotalFoodPrice);
                _logger.LogInformation("AddedScore from frontend: {AddedScore}", model.AddedScore);
                _logger.LogInformation("UsedScore from frontend: {UsedScore}", model.UsedScore);

                // Validate AccountId is not null or empty
                if (string.IsNullOrEmpty(model.AccountId))
                {
                    _logger.LogWarning("AccountId is null or empty");
                    return Json(new { success = false, message = "Account ID is required" });
                }

                // Validate member exists
                var memberAccount = context.Accounts.FirstOrDefault(a => a.AccountId == model.AccountId);
                if (memberAccount == null)
                {
                    _logger.LogWarning("Member account not found for AccountId: {AccountId}", model.AccountId);
                    return Json(new { success = false, message = "Member account not found" });
                }

                // Validate voucher if provided
                if (!string.IsNullOrEmpty(model.SelectedVoucherId) && model.SelectedVoucherId.Trim() != "")
                {
                    var voucher = context.Vouchers.FirstOrDefault(v => v.VoucherId == model.SelectedVoucherId);
                    if (voucher == null)
                    {
                        _logger.LogWarning("Voucher not found for VoucherId: {VoucherId}", model.SelectedVoucherId);
                        return Json(new { success = false, message = "Selected voucher not found" });
                    }
                }

                // Validate MovieShow exists
                var movieShow = context.MovieShows.FirstOrDefault(ms => ms.MovieShowId == model.BookingDetails.MovieShowId);
                if (movieShow == null)
                {
                    _logger.LogWarning("MovieShow not found for MovieShowId: {MovieShowId}", model.BookingDetails.MovieShowId);
                    return Json(new { success = false, message = "Movie show not found" });
                }

                // Validate UsedScore is not negative
                if (model.UsedScore < 0)
                {
                    _logger.LogWarning("UsedScore is negative: {UsedScore}", model.UsedScore);
                    return Json(new { success = false, message = "Used score cannot be negative" });
                }

                // Validate RankDiscountPercent is not negative
                if (model.RankDiscountPercent < 0)
                {
                    _logger.LogWarning("RankDiscountPercent is negative: {RankDiscountPercent}", model.RankDiscountPercent);
                    return Json(new { success = false, message = "Rank discount percent cannot be negative" });
                }

                // Validate SelectedSeats is not null or empty
                if (model.BookingDetails?.SelectedSeats == null || !model.BookingDetails.SelectedSeats.Any())
                {
                    _logger.LogWarning("SelectedSeats is null or empty");
                    return Json(new { success = false, message = "No seats selected" });
                }

                // Validate TotalPrice is not negative
                if (model.BookingDetails.TotalPrice < 0)
                {
                    _logger.LogWarning("TotalPrice is negative: {TotalPrice}", model.BookingDetails.TotalPrice);
                    return Json(new { success = false, message = "Total price cannot be negative" });
                }
                
                // Validate TotalPrice is not zero
                if (model.BookingDetails.TotalPrice <= 0)
                {
                    _logger.LogWarning("BookingDetails.TotalPrice is zero or negative: {TotalPrice}", model.BookingDetails.TotalPrice);
                    return Json(new { success = false, message = "Booking total price must be greater than zero" });
                }

                // Validate TotalFoodPrice is not negative
                if (model.TotalFoodPrice < 0)
                {
                    _logger.LogWarning("TotalFoodPrice is negative: {TotalFoodPrice}", model.TotalFoodPrice);
                    return Json(new { success = false, message = "Total food price cannot be negative" });
                }

                // Validate TotalFoodDiscount is not greater than TotalFoodPrice
                if (model.TotalFoodDiscount > model.TotalFoodPrice)
                {
                    _logger.LogWarning("TotalFoodDiscount ({TotalFoodDiscount}) is greater than TotalFoodPrice ({TotalFoodPrice})", model.TotalFoodDiscount, model.TotalFoodPrice);
                    return Json(new { success = false, message = "Total food discount cannot be greater than total food price" });
                }

                // Validate UsedScoreValue is not greater than TotalPrice
                if (model.UsedScoreValue > model.TotalPrice)
                {
                    _logger.LogWarning("UsedScoreValue ({UsedScoreValue}) is greater than TotalPrice ({TotalPrice})", model.UsedScoreValue, model.TotalPrice);
                    return Json(new { success = false, message = "Used score value cannot be greater than total price" });
                }

                // Validate AddedScore is not negative
                if (model.AddedScore < 0)
                {
                    _logger.LogWarning("AddedScore is negative: {AddedScore}", model.AddedScore);
                    return Json(new { success = false, message = "Added score cannot be negative" });
                }

                // Validate AddedScoreValue is not negative
                if (model.AddedScoreValue < 0)
                {
                    _logger.LogWarning("AddedScoreValue is negative: {AddedScoreValue}", model.AddedScoreValue);
                    return Json(new { success = false, message = "Added score value cannot be negative" });
                }

                // Validate MovieShowId is not zero
                if (model.BookingDetails.MovieShowId <= 0)
                {
                    _logger.LogWarning("MovieShowId is invalid: {MovieShowId}", model.BookingDetails.MovieShowId);
                    return Json(new { success = false, message = "Invalid movie show ID" });
                }

                // Validate MovieName is not null or empty
                if (string.IsNullOrEmpty(model.BookingDetails.MovieName))
                {
                    _logger.LogWarning("MovieName is null or empty");
                    return Json(new { success = false, message = "Movie name is required" });
                }

                // Validate MemberFullName is not null or empty
                if (string.IsNullOrEmpty(model.MemberFullName))
                {
                    _logger.LogWarning("MemberFullName is null or empty");
                    return Json(new { success = false, message = "Member full name is required" });
                }

                // Validate MemberPhoneNumber is not null or empty
                if (string.IsNullOrEmpty(model.MemberPhoneNumber))
                {
                    _logger.LogWarning("MemberPhoneNumber is null or empty");
                    return Json(new { success = false, message = "Member phone number is required" });
                }

                // Validate ShowDate is not default
                if (model.BookingDetails.ShowDate == default)
                {
                    _logger.LogWarning("ShowDate is default");
                    return Json(new { success = false, message = "Show date is required" });
                }

                // Validate ShowTime is not null or empty
                if (string.IsNullOrEmpty(model.BookingDetails.ShowTime))
                {
                    _logger.LogWarning("ShowTime is null or empty");
                    return Json(new { success = false, message = "Show time is required" });
                }

                // Validate CinemaRoomName is not null or empty
                if (string.IsNullOrEmpty(model.BookingDetails.CinemaRoomName))
                {
                    _logger.LogWarning("CinemaRoomName is null or empty");
                    return Json(new { success = false, message = "Cinema room name is required" });
                }

                // Validate VersionName is not null or empty
                if (string.IsNullOrEmpty(model.BookingDetails.VersionName))
                {
                    _logger.LogWarning("VersionName is null or empty");
                    return Json(new { success = false, message = "Version name is required" });
                }

                // Validate VersionId is not null
                if (model.BookingDetails.VersionId == null)
                {
                    _logger.LogWarning("VersionId is null");
                    return Json(new { success = false, message = "Version ID is required" });
                }

                // Validate PricePerTicket is not negative
                if (model.BookingDetails.PricePerTicket < 0)
                {
                    _logger.LogWarning("PricePerTicket is negative: {PricePerTicket}", model.BookingDetails.PricePerTicket);
                    return Json(new { success = false, message = "Price per ticket cannot be negative" });
                }

                // Validate PromotionDiscountPercent is not negative
                if (model.BookingDetails.PromotionDiscountPercent < 0)
                {
                    _logger.LogWarning("PromotionDiscountPercent is negative: {PromotionDiscountPercent}", model.BookingDetails.PromotionDiscountPercent);
                    return Json(new { success = false, message = "Promotion discount percent cannot be negative" });
                }

                // Validate VoucherAmount is not negative
                if (model.VoucherAmount < 0)
                {
                    _logger.LogWarning("VoucherAmount is negative: {VoucherAmount}", model.VoucherAmount);
                    return Json(new { success = false, message = "Voucher amount cannot be negative" });
                }

                // Validate SelectedFoods is not null
                if (model.SelectedFoods == null)
                {
                    _logger.LogWarning("SelectedFoods is null");
                    return Json(new { success = false, message = "Selected foods cannot be null" });
                }

                // Validate EligibleFoodPromotions is not null
                if (model.EligibleFoodPromotions == null)
                {
                    _logger.LogWarning("EligibleFoodPromotions is null");
                    return Json(new { success = false, message = "Eligible food promotions cannot be null" });
                }

                // Validate TotalFoodDiscount is not negative
                if (model.TotalFoodDiscount < 0)
                {
                    _logger.LogWarning("TotalFoodDiscount is negative: {TotalFoodDiscount}", model.TotalFoodDiscount);
                    return Json(new { success = false, message = "Total food discount cannot be negative" });
                }

                // Validate UsedScoreValue is not negative
                if (model.UsedScoreValue < 0)
                {
                    _logger.LogWarning("UsedScoreValue is negative: {UsedScoreValue}", model.UsedScoreValue);
                    return Json(new { success = false, message = "Used score value cannot be negative" });
                }

                // Validate AddedScoreValue is not negative
                if (model.AddedScoreValue < 0)
                {
                    _logger.LogWarning("AddedScoreValue is negative: {AddedScoreValue}", model.AddedScoreValue);
                    return Json(new { success = false, message = "Added score value cannot be negative" });
                }

                // Validate Subtotal is not negative
                if (model.Subtotal < 0)
                {
                    _logger.LogWarning("Subtotal is negative: {Subtotal}", model.Subtotal);
                    return Json(new { success = false, message = "Subtotal cannot be negative" });
                }

                // Validate RankDiscount is not negative
                if (model.RankDiscount < 0)
                {
                    _logger.LogWarning("RankDiscount is negative: {RankDiscount}", model.RankDiscount);
                    return Json(new { success = false, message = "Rank discount cannot be negative" });
                }

                // Validate TotalPrice is not negative
                if (model.TotalPrice < 0)
                {
                    _logger.LogWarning("TotalPrice is negative: {TotalPrice}", model.TotalPrice);
                    return Json(new { success = false, message = "Total price cannot be negative" });
                }

                // Validate MemberCheckMessage is not null
                if (model.MemberCheckMessage == null)
                {
                    _logger.LogWarning("MemberCheckMessage is null");
                    return Json(new { success = false, message = "Member check message cannot be null" });
                }

                // Validate ReturnUrl is not null
                if (model.ReturnUrl == null)
                {
                    _logger.LogWarning("ReturnUrl is null");
                    return Json(new { success = false, message = "Return URL cannot be null" });
                }

                // Validate CustomerType is valid
                if (string.IsNullOrEmpty(model.CustomerType) || (model.CustomerType != "member" && model.CustomerType != "guest"))
                {
                    _logger.LogWarning("CustomerType is invalid: {CustomerType}", model.CustomerType);
                    return Json(new { success = false, message = "Invalid customer type" });
                }

                // Validate MemberIdInput is not null
                if (model.MemberIdInput == null)
                {
                    _logger.LogWarning("MemberIdInput is null");
                    return Json(new { success = false, message = "Member ID input cannot be null" });
                }

                // Validate MemberId is not null
                if (model.MemberId == null)
                {
                    _logger.LogWarning("MemberId is null");
                    return Json(new { success = false, message = "Member ID cannot be null" });
                }

                // Validate MemberIdentityCard is not null
                if (model.MemberIdentityCard == null)
                {
                    _logger.LogWarning("MemberIdentityCard is null");
                    return Json(new { success = false, message = "Member identity card cannot be null" });
                }

                // Validate MemberEmail is not null
                if (model.MemberEmail == null)
                {
                    _logger.LogWarning("MemberEmail is null");
                    return Json(new { success = false, message = "Member email cannot be null" });
                }

                // Validate MemberPhone is not null
                if (model.MemberPhone == null)
                {
                    _logger.LogWarning("MemberPhone is null");
                    return Json(new { success = false, message = "Member phone cannot be null" });
                }

                // Validate TicketsToConvert is not negative
                if (model.TicketsToConvert < 0)
                {
                    _logger.LogWarning("TicketsToConvert is negative: {TicketsToConvert}", model.TicketsToConvert);
                    return Json(new { success = false, message = "Tickets to convert cannot be negative" });
                }

                // Validate DiscountFromScore is not negative
                if (model.DiscountFromScore < 0)
                {
                    _logger.LogWarning("DiscountFromScore is negative: {DiscountFromScore}", model.DiscountFromScore);
                    return Json(new { success = false, message = "Discount from score cannot be negative" });
                }

                // Validate AddedScore is not negative
                if (model.AddedScore < 0)
                {
                    _logger.LogWarning("AddedScore is negative: {AddedScore}", model.AddedScore);
                    return Json(new { success = false, message = "Added score cannot be negative" });
                }

                // Validate MemberScore is not negative
                if (model.MemberScore < 0)
                {
                    _logger.LogWarning("MemberScore is negative: {MemberScore}", model.MemberScore);
                    return Json(new { success = false, message = "Member score cannot be negative" });
                }

                // Validate MovieId is not null or empty
                if (string.IsNullOrEmpty(model.BookingDetails.MovieId))
                {
                    _logger.LogWarning("MovieId is null or empty");
                    return Json(new { success = false, message = "Movie ID is required" });
                }

                // Validate PricePerTicket is not zero
                if (model.BookingDetails.PricePerTicket <= 0)
                {
                    _logger.LogWarning("PricePerTicket is zero or negative: {PricePerTicket}", model.BookingDetails.PricePerTicket);
                    return Json(new { success = false, message = "Price per ticket must be greater than zero" });
                }

                // Validate RankDiscountPercent is not greater than 100
                if (model.RankDiscountPercent > 100)
                {
                    _logger.LogWarning("RankDiscountPercent is greater than 100: {RankDiscountPercent}", model.RankDiscountPercent);
                    return Json(new { success = false, message = "Rank discount percent cannot be greater than 100" });
                }

                // Validate PromotionDiscountPercent is not greater than 100
                if (model.BookingDetails.PromotionDiscountPercent > 100)
                {
                    _logger.LogWarning("PromotionDiscountPercent is greater than 100: {PromotionDiscountPercent}", model.BookingDetails.PromotionDiscountPercent);
                    return Json(new { success = false, message = "Promotion discount percent cannot be greater than 100" });
                }

                // Validate UsedScore is not greater than MemberScore
                if (model.UsedScore > model.MemberScore)
                {
                    _logger.LogWarning("UsedScore ({UsedScore}) is greater than MemberScore ({MemberScore})", model.UsedScore, model.MemberScore);
                    return Json(new { success = false, message = "Used score cannot be greater than member score" });
                }



                // Validate RankDiscount is not greater than Subtotal
                if (model.RankDiscount > model.Subtotal)
                {
                    _logger.LogWarning("RankDiscount ({RankDiscount}) is greater than Subtotal ({Subtotal})", model.RankDiscount, model.Subtotal);
                    return Json(new { success = false, message = "Rank discount cannot be greater than subtotal" });
                }

                // Validate TotalPrice is not zero
                if (model.TotalPrice <= 0)
                {
                    _logger.LogWarning("TotalPrice is zero or negative: {TotalPrice}", model.TotalPrice);
                    _logger.LogWarning("Model data received: {ModelData}", JsonSerializer.Serialize(model));
                    
                    // Tính toán lại total price nếu bị 0 hoặc âm
                    var recalculatedTotal = model.BookingDetails.TotalPrice + model.TotalFoodPrice;
                    if (recalculatedTotal > 0)
                    {
                        _logger.LogInformation("Recalculating total price to: {RecalculatedTotal}", recalculatedTotal);
                        model.TotalPrice = recalculatedTotal;
                    }
                    else if (model.TotalFoodPrice > 0)
                    {
                        // Nếu chỉ có food price > 0, sử dụng nó
                        _logger.LogInformation("Using food price as total: {FoodPrice}", model.TotalFoodPrice);
                        model.TotalPrice = model.TotalFoodPrice;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Total price must be greater than zero" });
                    }
                }
                
                // Đảm bảo voucher amount không vượt quá giá gốc (trước khi áp dụng rank discount)
                if (model.VoucherAmount > 0)
                {
                    var originalTotalPrice = model.BookingDetails.TotalPrice + model.TotalFoodPrice;
                    if (model.VoucherAmount > originalTotalPrice)
                    {
                        _logger.LogWarning("VoucherAmount ({VoucherAmount}) is greater than original TotalPrice ({OriginalTotalPrice})", model.VoucherAmount, originalTotalPrice);
                        return Json(new { success = false, message = "Voucher amount cannot be greater than original total price" });
                    }
                }

                // Validate TotalFoodPrice is not negative
                if (model.TotalFoodPrice < 0)
                {
                    _logger.LogWarning("TotalFoodPrice is negative: {TotalFoodPrice}", model.TotalFoodPrice);
                    return Json(new { success = false, message = "Total food price cannot be negative" });
                }

                // 1. Sinh mã OrderId duy nhất cho QR Payment (DH = Đơn Hàng)
                string invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                while (context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                }

                // Tổng tiền = sử dụng giá đã được tính toán từ frontend
                decimal totalAmount = model.TotalPrice > 0 ? model.TotalPrice : (model.BookingDetails.TotalPrice + model.TotalFoodPrice);
                var orderInfo = $"Ve xem phim - {model.BookingDetails.MovieName}";

                // 2. Lưu invoice vào DB với AccountId của member
                if (!context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    var voucherId = !string.IsNullOrEmpty(model.SelectedVoucherId) && model.SelectedVoucherId.Trim() != "" ? model.SelectedVoucherId : null;
                    
                    _logger.LogInformation("Creating invoice with InvoiceId: {InvoiceId}, AccountId: {AccountId}, VoucherId: {VoucherId}, AddedScore: {AddedScore}, UsedScore: {UsedScore}", 
                        invoiceId, model.AccountId, voucherId, model.AddedScore, model.UsedScore);
                    
                    var invoice = new Invoice
                    {
                        InvoiceId = invoiceId,
                        AccountId = model.AccountId, // Sử dụng AccountId của member
                        AddScore = model.AddedScore, // Sử dụng AddedScore từ model
                        BookingDate = DateTime.Now,
                        Status = InvoiceStatus.Incomplete, // Pending
                        TotalMoney = totalAmount,
                        UseScore = model.UsedScore, // Sử dụng điểm của member
                        Seat = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName)),
                        SeatIds = string.Join(",", model.BookingDetails.SelectedSeats.Select(s => s.SeatId)),
                        MovieShowId = model.BookingDetails.MovieShowId,
                        PromotionDiscount = model.RankDiscountPercent.ToString(),
                        VoucherId = voucherId, // Chỉ set VoucherId nếu có voucher
                        RankDiscountPercentage = model.RankDiscountPercent
                    };
                    context.Invoices.Add(invoice);
                    context.SaveChanges();
                }

                // 3. Tạo QR code với reference là invoiceId
                var qrData = _qrPaymentService.GenerateQRCodeData(totalAmount, orderInfo, invoiceId);
                var qrImage = _qrPaymentService.GetQRCodeImage(qrData);

                // Tạo ScheduleSeat records và extend hold time for selected seats when creating QR code
                var movieShowId = model.BookingDetails.MovieShowId;
                var accountId = model.AccountId; // Sử dụng AccountId của member
                foreach (var seat in model.BookingDetails.SelectedSeats)
                {
                    if (seat.SeatId.HasValue)
                    {
                        // Tạo hoặc cập nhật ScheduleSeat record
                        var existingScheduleSeat = context.ScheduleSeats
                            .FirstOrDefault(ss => ss.MovieShowId == movieShowId && ss.SeatId == seat.SeatId.Value);
                        
                        if (existingScheduleSeat != null)
                        {
                            // Cập nhật existing record
                            existingScheduleSeat.InvoiceId = invoiceId;
                            existingScheduleSeat.SeatStatusId = 1; // 1 = Held
                            context.ScheduleSeats.Update(existingScheduleSeat);
                        }
                        else
                        {
                            // Tạo mới ScheduleSeat record
                            var scheduleSeat = new ScheduleSeat
                            {
                                MovieShowId = movieShowId,
                                InvoiceId = invoiceId,
                                SeatId = seat.SeatId.Value,
                                SeatStatusId = 1, // 1 = Held
                                BookedPrice = seat.Price
                            };
                            context.ScheduleSeats.Add(scheduleSeat);
                        }
                        
                        // Extend hold time
                        SeatHub.ExtendHoldTime(movieShowId, seat.SeatId.Value, accountId);
                    }
                }
                
                // Save changes for ScheduleSeat records
                context.SaveChanges();

                var viewModel = new QRPaymentViewModel
                {
                    OrderId = invoiceId,
                    Amount = totalAmount,
                    OrderInfo = orderInfo,
                    QRCodeData = qrData,
                    QRCodeImage = qrImage,
                    ExpiredTime = DateTime.Now.AddMinutes(15),
                    CustomerName = model.MemberFullName,
                    CustomerPhone = model.MemberPhoneNumber,
                    MovieName = model.BookingDetails.MovieName,
                    ShowTime = $"{model.BookingDetails.ShowDate:dd/MM/yyyy} {model.BookingDetails.ShowTime}",
                    SeatInfo = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName))
                };
                return RedirectToAction("DisplayQR", "QRPayment", new {
                    orderId = invoiceId,
                    amount = totalAmount,
                    customerName = model.MemberFullName,
                    customerPhone = model.MemberPhoneNumber,
                    movieName = model.BookingDetails.MovieName,
                    showTime = $"{model.BookingDetails.ShowDate:dd/MM/yyyy} {model.BookingDetails.ShowTime}",
                    seatInfo = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR code for member");
                return Json(new { success = false, message = "Error creating QR code: " + ex.Message });
            }
        }

        /// <summary>
        /// Test QR code - để kiểm tra QR code có hoạt động không
        /// </summary>
        [HttpGet]
        public IActionResult TestQR()
        {
            try
            {
                var testOrderId = $"TEST{DateTime.Now:MMddHHmm}";
                var testAmount = 50000m;
                var testOrderInfo = "Test QR Code Payment";
                
                _logger.LogInformation("TestQR - Testing PayOS QR generation");
                var payosQrUrl = _qrPaymentService.GeneratePayOSQRCode(testAmount, testOrderInfo, testOrderId);
                _logger.LogInformation("TestQR - PayOS QR URL: {PayOSQRCodeUrl}", payosQrUrl);
                
                var viewModel = new QRPaymentViewModel
                {
                    OrderId = testOrderId,
                    Amount = testAmount,
                    OrderInfo = testOrderInfo,
                    PayOSQRCodeUrl = payosQrUrl,
                    ExpiredTime = DateTime.Now.AddMinutes(15),
                    CustomerName = "Test Customer",
                    CustomerPhone = "0123456789",
                    MovieName = "Test Movie",
                    ShowTime = "01/01/2024 20:00",
                    SeatInfo = "A1, A2"
                };

                return View("DisplayQR", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestQR");
                return Content($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test QR code cho Member - để kiểm tra QR code có hoạt động không
        /// </summary>
        [HttpGet]
        public IActionResult TestQRForMember()
        {
            try
            {
                var testOrderId = $"TEST{DateTime.Now:MMddHHmm}";
                var testAmount = 50000m;
                var testOrderInfo = "Test QR Code Payment for Member";
                
                // Tạo QR code VietQR thực tế
                var qrData = _qrPaymentService.GenerateQRCodeData(testAmount, testOrderInfo, testOrderId);
                var qrImage = _qrPaymentService.GetQRCodeImage(qrData);
                
                var viewModel = new QRPaymentViewModel
                {
                    OrderId = testOrderId,
                    Amount = testAmount,
                    OrderInfo = testOrderInfo,
                    QRCodeData = qrData,
                    QRCodeImage = qrImage,
                    ExpiredTime = DateTime.Now.AddMinutes(15),
                    CustomerName = "Test Member",
                    CustomerPhone = "0123456789",
                    MovieName = "Test Movie",
                    ShowTime = "01/01/2024 20:00",
                    SeatInfo = "A1, A2"
                };

                return View("TestQR", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestQRForMember");
                return Content($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hiển thị QR code cho Guest
        /// </summary>
        [HttpGet]
        public IActionResult DisplayQR(string orderId, decimal amount, string customerName, string customerPhone, string movieName, string showTime, string seatInfo)
        {
            try
            {
                _logger.LogInformation("DisplayQR called with orderId: {OrderId}, amount: {Amount}", orderId, amount);

                var orderInfo = $"Ve xem phim - {movieName}";
                
                // Debug: Log thông tin đầu vào
                _logger.LogInformation("DisplayQR input - Amount: {Amount}, OrderInfo: {OrderInfo}, OrderId: {OrderId}", amount, orderInfo, orderId);
                
                // Chỉ tạo QR PayOS
                var payosQrUrl = _qrPaymentService.GeneratePayOSQRCode(amount, orderInfo, orderId);

                _logger.LogInformation("PayOS QR URL generated: {PayOSQRCodeUrl}", payosQrUrl);

                // Fallback nếu PayOS QR không tạo được
                if (string.IsNullOrEmpty(payosQrUrl))
                {
                    _logger.LogWarning("PayOS QR generation failed, using fallback VietQR");
                    var vietQrUrl = _qrPaymentService.GenerateVietQRCode(amount, orderInfo, orderId);
                    _logger.LogInformation("VietQR URL generated: {VietQRUrl}", vietQrUrl);
                    
                    if (!string.IsNullOrEmpty(vietQrUrl))
                    {
                        // Tạo QR code image từ VietQR URL
                        payosQrUrl = _qrPaymentService.GetQRCodeImage(vietQrUrl);
                        _logger.LogInformation("QR Image URL from VietQR: {QRImageUrl}", payosQrUrl);
                    }
                    else
                    {
                        // Fallback cuối cùng: tạo QR code đơn giản
                        _logger.LogWarning("VietQR also failed, using simple QR code");
                        payosQrUrl = _qrPaymentService.GenerateSimpleQRCode($"PAYMENT_{orderId}_{amount}");
                        _logger.LogInformation("Simple QR URL: {SimpleQRUrl}", payosQrUrl);
                    }
                }

                // Final fallback nếu tất cả đều fail
                if (string.IsNullOrEmpty(payosQrUrl))
                {
                    _logger.LogError("All QR generation methods failed, using demo QR");
                    payosQrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=DEMO_QR_CODE_PAYMENT";
                }

                var viewModel = new QRPaymentViewModel
                {
                    OrderId = orderId,
                    Amount = amount,
                    OrderInfo = orderInfo,
                    PayOSQRCodeUrl = payosQrUrl,
                    ExpiredTime = DateTime.Now.AddMinutes(15),
                    CustomerName = customerName,
                    CustomerPhone = customerPhone,
                    MovieName = movieName,
                    ShowTime = showTime,
                    SeatInfo = seatInfo
                };

                _logger.LogInformation("Final ViewModel created with PayOSQRCodeUrl: {PayOSQRCodeUrl}", viewModel.PayOSQRCodeUrl);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DisplayQR: {Message}", ex.Message);
                // Fallback view model
                return View(new QRPaymentViewModel { OrderId = orderId, Amount = amount });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán - Thực tế
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckPaymentStatus([FromBody] CheckPaymentRequest request)
        {
            try
            {
                _logger.LogInformation("CheckPaymentStatus called with orderId: {OrderId}", request.orderId);
                
                // Force reload from database to get latest data
                _context.ChangeTracker.Clear();
                var invoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == request.orderId);
                
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for orderId: {OrderId}", request.orderId);
                    return Json(new { success = false, message = "Invoice not found" });
                }
                
                // Log chi tiết invoice để debug
                _logger.LogInformation("Invoice found: {OrderId}, Status: {Status}, TotalMoney: {TotalMoney}, BookingDate: {BookingDate}", 
                    request.orderId, invoice.Status, invoice.TotalMoney, invoice.BookingDate);
                
                bool isPaid = invoice.Status == InvoiceStatus.Completed;
                _logger.LogInformation("Invoice {OrderId} status: {Status}, isPaid: {IsPaid}", request.orderId, invoice.Status, isPaid);
                
                if (isPaid)
                {
                    _logger.LogInformation("Payment completed for {OrderId}, returning success response", request.orderId);
                    
                    // Tự động xác nhận thanh toán và cập nhật trạng thái ghế
                    try
                    {
                        var existingInvoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == request.orderId);
                        if (existingInvoice != null && existingInvoice.Status == InvoiceStatus.Completed)
                        {
                            // Gọi logic xác nhận thanh toán
                            await ConfirmPaymentInternal(request.orderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-confirming payment for {OrderId}", request.orderId);
                    }
                    
                    return Json(new { 
                        success = true, 
                        isPaid = true, 
                        message = "Payment completed successfully!",
                        invoiceId = request.orderId,
                        redirectUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" })
                    });
                }
                else
                {
                    _logger.LogInformation("Payment pending for {OrderId}, returning pending response", request.orderId);
                    return Json(new { 
                        success = true, 
                        isPaid = false, 
                        message = "Payment pending...",
                        invoiceId = request.orderId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for orderId: {OrderId}", request.orderId);
                return Json(new { success = false, message = "Error checking payment status: " + ex.Message });
            }
        }
        
        /// <summary>
        /// Xác nhận thanh toán và cập nhật trạng thái Invoice và ghế trong database
        /// </summary>
        [HttpPost]
        public IActionResult ConfirmPayment(string invoiceId)
        {
            try
            {
                var result = ConfirmPaymentInternal(invoiceId).Result;
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for invoice {InvoiceId}", invoiceId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Logic nội bộ để xác nhận thanh toán và cập nhật trạng thái
        /// </summary>
        private async Task<bool> ConfirmPaymentInternal(string invoiceId)
        {
            try
            {
                var invoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoice != null)
                {
                    // 1. Cập nhật trạng thái Invoice
                    invoice.Status = InvoiceStatus.Completed;
                    
                    // 2. Cập nhật trạng thái ghế sang "booked"
                    if (!string.IsNullOrEmpty(invoice.SeatIds) && invoice.MovieShowId.HasValue)
                    {
                        var seatIds = invoice.SeatIds.Split(',').Select(s => int.Parse(s.Trim())).ToList();
                        var movieShowId = invoice.MovieShowId.Value;
                        
                        // Tìm và cập nhật trạng thái ghế
                        var scheduleSeats = _context.ScheduleSeats
                            .Where(ss => ss.MovieShowId == movieShowId && ss.SeatId.HasValue && seatIds.Contains(ss.SeatId.Value))
                            .ToList();
                        
                        foreach (var scheduleSeat in scheduleSeats)
                        {
                            scheduleSeat.SeatStatusId = 2; // 2 = Booked status
                            _logger.LogInformation("Updating seat {SeatId} status to Booked for invoice {InvoiceId}", 
                                scheduleSeat.SeatId, invoiceId);
                        }
                        
                        // Gửi SignalR notification để cập nhật UI real-time
                        if (scheduleSeats.Any())
                        {
                            var currentMovieShowId = scheduleSeats.First().MovieShowId;
                            foreach (var scheduleSeat in scheduleSeats)
                            {
                                if (scheduleSeat.SeatId.HasValue && currentMovieShowId.HasValue)
                                {
                                    // Gửi notification qua SignalR
                                    await _seatHubContext.Clients.Group(currentMovieShowId.Value.ToString()).SendAsync("SeatStatusChanged", scheduleSeat.SeatId.Value, 2); // 2 = Booked
                                }
                            }
                        }
                        
                        // 3. Cập nhật điểm cho member nếu có
                        if (!string.IsNullOrEmpty(invoice.AccountId) && invoice.AccountId != "GUEST")
                        {
                            var member = _context.Members.FirstOrDefault(m => m.AccountId == invoice.AccountId);
                            if (member != null)
                            {
                                _logger.LogInformation("Found member {AccountId} with current TotalPoints: {CurrentPoints}", 
                                    invoice.AccountId, member.TotalPoints);
                                
                                // Trừ điểm đã sử dụng
                                if (invoice.UseScore > 0)
                                {
                                    member.TotalPoints -= invoice.UseScore.Value;
                                    _logger.LogInformation("Deducted {UseScore} points from account {AccountId}. New TotalPoints: {NewPoints}", 
                                        invoice.UseScore, invoice.AccountId, member.TotalPoints);
                                }
                                
                                // Cộng điểm thưởng
                                if (invoice.AddScore > 0)
                                {
                                    member.TotalPoints += invoice.AddScore.Value;
                                    _logger.LogInformation("Added {AddScore} points to account {AccountId}. New TotalPoints: {NewPoints}", 
                                        invoice.AddScore, invoice.AccountId, member.TotalPoints);
                                }
                                else
                                {
                                    _logger.LogWarning("AddScore is 0 or null for invoice {InvoiceId}. AddScore value: {AddScore}", 
                                        invoiceId, invoice.AddScore);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Member not found for AccountId: {AccountId}", invoice.AccountId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Skipping member points update - AccountId: {AccountId}", invoice.AccountId);
                        }
                        
                        // 4. Cập nhật voucher status nếu có
                        if (!string.IsNullOrEmpty(invoice.VoucherId))
                        {
                            var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == invoice.VoucherId);
                            if (voucher != null)
                            {
                                voucher.IsUsed = true;
                                _logger.LogInformation("Marked voucher {VoucherId} as used", invoice.VoucherId);
                            }
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Payment confirmed successfully for invoice {InvoiceId}", invoiceId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmPaymentInternal for invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

    }
} 