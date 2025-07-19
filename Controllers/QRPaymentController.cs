using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Text.Json;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QRPaymentController : Controller
    {
        private readonly IQRPaymentService _qrPaymentService;
        private readonly ILogger<QRPaymentController> _logger;

        public QRPaymentController(IQRPaymentService qrPaymentService, ILogger<QRPaymentController> logger)
        {
            _qrPaymentService = qrPaymentService;
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
                // Parse JSON data
                var model = JsonSerializer.Deserialize<ConfirmTicketAdminViewModel>(modelData);
                
                // Tạo order ID
                var orderId = $"GUEST_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                
                // Tạo thông tin đơn hàng
                var orderInfo = $"Ve xem phim - {model.BookingDetails.MovieName}";
                
                // Tạo QR code data
                var qrData = _qrPaymentService.GenerateQRCodeData(model.BookingDetails.TotalPrice, orderInfo, orderId);
                
                // Tạo QR code image
                var qrImage = _qrPaymentService.GetQRCodeImage(qrData);
                
                // Tạo view model
                var viewModel = new QRPaymentViewModel
                {
                    OrderId = orderId,
                    Amount = model.BookingDetails.TotalPrice,
                    OrderInfo = orderInfo,
                    QRCodeData = qrData,
                    QRCodeImage = qrImage,
                    ExpiredTime = DateTime.Now.AddMinutes(15), // 15 phút
                    CustomerName = model.MemberFullName,
                    CustomerPhone = model.MemberPhoneNumber,
                    MovieName = model.BookingDetails.MovieName,
                    ShowTime = $"{model.BookingDetails.ShowDate:dd/MM/yyyy} {model.BookingDetails.ShowTime}",
                    SeatInfo = string.Join(", ", model.BookingDetails.SelectedSeats.Select(s => s.SeatName))
                };

                // Redirect to QR display page with full data
                return RedirectToAction("DisplayQR", "QRPayment", new { 
                    orderId = orderId,
                    amount = model.BookingDetails.TotalPrice,
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
                return Json(new { success = false, message = "Error creating QR code" });
            }
        }

        /// <summary>
        /// Hiển thị QR code cho Guest
        /// </summary>
        [HttpGet]
        public IActionResult DisplayQR(string orderId, decimal amount, string customerName, string customerPhone, string movieName, string showTime, string seatInfo)
        {
            // Tạo thông tin đơn hàng
            var orderInfo = $"Ve xem phim - {movieName}";
            
            // Tạo QR code data
            var qrData = _qrPaymentService.GenerateQRCodeData(amount, orderInfo, orderId);
            
            // Tạo view model
            var viewModel = new QRPaymentViewModel
            {
                OrderId = orderId,
                Amount = amount,
                OrderInfo = orderInfo,
                QRCodeData = qrData,
                QRCodeImage = _qrPaymentService.GetQRCodeImage(qrData),
                ExpiredTime = DateTime.Now.AddMinutes(15),
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                MovieName = movieName,
                ShowTime = showTime,
                SeatInfo = seatInfo
            };

            return View(viewModel);
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán
        /// </summary>
        [HttpPost]
        public IActionResult CheckPaymentStatus(string orderId)
        {
            try
            {
                // Trong thực tế, bạn sẽ kiểm tra với ngân hàng
                // Ở đây chỉ là demo
                var isPaid = _qrPaymentService.ValidatePayment(orderId, "DEMO_TRANSACTION");
                
                return Json(new { success = true, isPaid = isPaid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status");
                return Json(new { success = false, message = "Error checking payment status" });
            }
        }
    }
} 