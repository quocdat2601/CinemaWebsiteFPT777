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
                        VoucherId = null,
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