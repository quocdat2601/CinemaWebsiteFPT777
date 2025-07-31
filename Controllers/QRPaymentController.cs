using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using MovieTheater.Models;
using MovieTheater.Hubs;
using System.Text.Json;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QRPaymentController : Controller
    {
        private readonly IQRPaymentService _qrPaymentService;
        private readonly IGuestInvoiceService _guestInvoiceService;
        private readonly ILogger<QRPaymentController> _logger;
        private readonly MovieTheaterContext _context;

        public QRPaymentController(IQRPaymentService qrPaymentService, IGuestInvoiceService guestInvoiceService, ILogger<QRPaymentController> logger, MovieTheaterContext context)
        {
            _qrPaymentService = qrPaymentService;
            _guestInvoiceService = guestInvoiceService;
            _logger = logger;
            _context = context;
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

                // 1. Sinh mã InvoiceId duy nhất, tối đa 10 ký tự
                string invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                while (context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                }

                // Tổng tiền = seat + food
                decimal totalAmount = model.BookingDetails.TotalPrice + model.TotalFoodPrice;
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

                // Extend hold time for selected seats when creating QR code
                var movieShowId = model.BookingDetails.MovieShowId;
                var accountId = "GUEST"; // Since this is guest booking
                foreach (var seat in model.BookingDetails.SelectedSeats)
                {
                    if (seat.SeatId.HasValue)
                    {
                        SeatHub.ExtendHoldTime(movieShowId, seat.SeatId.Value, accountId);
                    }
                }

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

                // Validate VoucherAmount is not greater than TotalPrice
                var effectiveTotalPrice = model.TotalPrice > 0 ? model.TotalPrice : model.BookingDetails.TotalPrice;
                if (model.VoucherAmount > effectiveTotalPrice)
                {
                    _logger.LogWarning("VoucherAmount ({VoucherAmount}) is greater than TotalPrice ({TotalPrice})", model.VoucherAmount, effectiveTotalPrice);
                    return Json(new { success = false, message = "Voucher amount cannot be greater than total price" });
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
                
                // Đảm bảo TotalPrice không bị âm sau khi áp dụng voucher
                if (model.VoucherAmount > 0 && model.TotalPrice > 0)
                {
                    var priceAfterVoucher = model.TotalPrice - model.VoucherAmount;
                    if (priceAfterVoucher < 0)
                    {
                        _logger.LogWarning("Price after voucher would be negative: {PriceAfterVoucher}", priceAfterVoucher);
                        return Json(new { success = false, message = "Voucher amount cannot be greater than total price" });
                    }
                }

                // Validate TotalFoodPrice is not negative
                if (model.TotalFoodPrice < 0)
                {
                    _logger.LogWarning("TotalFoodPrice is negative: {TotalFoodPrice}", model.TotalFoodPrice);
                    return Json(new { success = false, message = "Total food price cannot be negative" });
                }

                // 1. Sinh mã InvoiceId duy nhất, tối đa 10 ký tự
                string invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                while (context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    invoiceId = "DH" + (DateTime.UtcNow.Ticks % 1000000).ToString("D6");
                }

                // Tổng tiền = seat + food
                decimal totalAmount = model.BookingDetails.TotalPrice + model.TotalFoodPrice;
                var orderInfo = $"Ve xem phim - {model.BookingDetails.MovieName}";

                // 2. Lưu invoice vào DB với AccountId của member
                if (!context.Invoices.Any(i => i.InvoiceId == invoiceId))
                {
                    var voucherId = !string.IsNullOrEmpty(model.SelectedVoucherId) && model.SelectedVoucherId.Trim() != "" ? model.SelectedVoucherId : null;
                    
                    _logger.LogInformation("Creating invoice with InvoiceId: {InvoiceId}, AccountId: {AccountId}, VoucherId: {VoucherId}", 
                        invoiceId, model.AccountId, voucherId);
                    
                    var invoice = new Invoice
                    {
                        InvoiceId = invoiceId,
                        AccountId = model.AccountId, // Sử dụng AccountId của member
                        AddScore = 0,
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

                // Extend hold time for selected seats when creating QR code
                var movieShowId = model.BookingDetails.MovieShowId;
                var accountId = model.AccountId; // Sử dụng AccountId của member
                foreach (var seat in model.BookingDetails.SelectedSeats)
                {
                    if (seat.SeatId.HasValue)
                    {
                        SeatHub.ExtendHoldTime(movieShowId, seat.SeatId.Value, accountId);
                    }
                }

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
                    CustomerName = "Test Customer",
                    CustomerPhone = "0123456789",
                    MovieName = "Test Movie",
                    ShowTime = "01/01/2024 20:00",
                    SeatInfo = "A1, A2"
                };

                return View("TestQR", viewModel);
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
                // Chỉ tạo QR PayOS
                var payosQrUrl = _qrPaymentService.GeneratePayOSQRCode(amount, orderInfo, orderId);

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

                _logger.LogInformation("PayOS QR URL: {PayOSQRCodeUrl}", payosQrUrl);
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
        /// Xác nhận thanh toán và cập nhật trạng thái Invoice trong database
        /// </summary>
        [HttpPost]
        public IActionResult ConfirmPayment(string invoiceId)
        {
            try
            {
                var invoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoice != null)
                {
                    invoice.Status = InvoiceStatus.Completed; // hoặc tên enum tương ứng trạng thái đã thanh toán // Đã thanh toán
                    _context.SaveChanges();
                    TempData["PaymentSuccess"] = true;
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy hóa đơn" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
} 