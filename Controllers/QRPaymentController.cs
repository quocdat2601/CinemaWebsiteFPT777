using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using MovieTheater.Models;
using System.Text.Json;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QRPaymentController : Controller
    {
        private readonly IQRPaymentService _qrPaymentService;
        private readonly IGuestInvoiceService _guestInvoiceService;
        private readonly ILogger<QRPaymentController> _logger;

        public QRPaymentController(IQRPaymentService qrPaymentService, IGuestInvoiceService guestInvoiceService, ILogger<QRPaymentController> logger)
        {
            _qrPaymentService = qrPaymentService;
            _guestInvoiceService = guestInvoiceService;
            _logger = logger;
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
                var orderId = await bookingService.GenerateInvoiceIdAsync();
                // Tổng tiền = seat + food
                decimal totalAmount = model.BookingDetails.TotalPrice + model.TotalFoodPrice;
                var orderInfo = $"Ve xem phim - {model.BookingDetails.MovieName}";
                var qrData = _qrPaymentService.GenerateQRCodeData(totalAmount, orderInfo, orderId);
                var qrImage = _qrPaymentService.GetQRCodeImage(qrData);
                if (!context.Invoices.Any(i => i.InvoiceId == orderId))
                {
                    var invoice = new Invoice
                    {
                        InvoiceId = orderId,
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
                var viewModel = new QRPaymentViewModel
                {
                    OrderId = orderId,
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
                    orderId = orderId,
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
                    QRCodeData = payosQrUrl, // dùng luôn link QR PayOS nếu là ảnh
                    QRCodeImage = payosQrUrl, // dùng luôn link QR PayOS nếu là ảnh
                    ExpiredTime = DateTime.Now.AddMinutes(15),
                    CustomerName = customerName,
                    CustomerPhone = customerPhone,
                    MovieName = movieName,
                    ShowTime = showTime,
                    SeatInfo = seatInfo,
                    PayOSQRCodeUrl = payosQrUrl
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
        /// Kiểm tra trạng thái thanh toán - Demo version
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckPaymentStatus(string orderId, string modelData)
        {
            try
            {
                _logger.LogInformation("Demo CheckPaymentStatus called with orderId: {OrderId}", orderId);
                
                // Demo: Luôn trả về thanh toán thành công
                var isPaid = true; // Demo payment success
                
                if (isPaid)
                {
                    try
                    {
                        // Parse model data từ JSON string
                        var modelDataObj = JsonSerializer.Deserialize<JsonElement>(modelData);
                        _logger.LogInformation("Demo JSON parsed successfully");
                        
                        // Lấy thông tin từ JSON một cách an toàn
                        decimal totalPrice = 0;
                        var seatNames = new List<string>();
                        
                        try
                        {
                            var bookingDetails = modelDataObj.GetProperty("BookingDetails");
                            totalPrice = bookingDetails.GetProperty("TotalPrice").GetDecimal();
                            
                            var selectedSeats = bookingDetails.GetProperty("SelectedSeats");
                            foreach (var seat in selectedSeats.EnumerateArray())
                            {
                                var seatName = seat.GetProperty("SeatName").GetString();
                                if (!string.IsNullOrEmpty(seatName))
                                {
                                    seatNames.Add(seatName);
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning("Demo JSON parsing warning: {Message}", parseEx.Message);
                            // Sử dụng giá trị mặc định nếu parse lỗi
                            totalPrice = 64000; // Default amount
                            seatNames.Add("A10"); // Default seat
                        }
                        
                        _logger.LogInformation("Demo TotalPrice: {TotalPrice}, Seat names: {SeatNames}", totalPrice, string.Join(", ", seatNames));
                        
                        // Kiểm tra xem invoice đã tồn tại chưa
                        var existingInvoice = await _guestInvoiceService.GetInvoiceByOrderIdAsync(orderId);
                        if (existingInvoice != null)
                        {
                            _logger.LogInformation("Invoice already exists for order: {OrderId}", orderId);
                            return Json(new { 
                                success = true, 
                                isPaid = true, 
                                message = "Payment already completed",
                                invoiceId = existingInvoice.InvoiceId,
                                redirectUrl = Url.Action("TicketBookingConfirmed", "Booking", new { invoiceId = orderId })
                            });
                        }
                        
                        // Sử dụng GuestInvoiceService để lưu invoice
                        var invoiceSuccess = await _guestInvoiceService.CreateGuestInvoiceAsync(
                            orderId, 
                            totalPrice, 
                            "Demo Customer",
                            "Demo Phone",
                            "Demo Movie",
                            "Demo Time",
                            string.Join(", ", seatNames),
                            1 // Default movieShowId
                        );
                        
                        if (invoiceSuccess)
                        {
                            _logger.LogInformation("Guest invoice created successfully for order: {OrderId}", orderId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create guest invoice for order: {OrderId}", orderId);
                        }
                        
                        return Json(new { 
                            success = true, 
                            isPaid = true, 
                            message = "Demo payment successful!",
                            invoiceId = orderId,
                            redirectUrl = Url.Action("MainPage", "Admin", new { tab = "BookingMg" })
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Demo error processing payment data: {Message}", ex.Message);
                        return Json(new { success = false, message = "Error processing demo payment data" });
                    }
                }
                
                return Json(new { success = true, isPaid = false, message = "Demo payment pending" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demo error checking payment status: {Message}", ex.Message);
                return Json(new { success = false, message = "Error checking demo payment status" });
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
                var context = (MovieTheaterContext)HttpContext.RequestServices.GetService(typeof(MovieTheaterContext));
                var invoice = context.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
                if (invoice != null)
                {
                    invoice.Status = InvoiceStatus.Completed; // hoặc tên enum tương ứng trạng thái đã thanh toán // Đã thanh toán
                    context.SaveChanges();
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