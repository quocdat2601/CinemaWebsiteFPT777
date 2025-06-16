using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using System.Linq;
using System.Collections.Generic;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly VNPayService _vnPayService;

        public PaymentController(VNPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        /// <summary>
        /// Tạo URL thanh toán VNPay
        /// </summary>
        /// <param name="request">Thông tin thanh toán</param>
        /// <returns>URL thanh toán VNPay</returns>
        /// <response code="200">Trả về URL thanh toán</response>
        /// <response code="400">Nếu có lỗi xảy ra</response>
        [HttpPost("create-payment")]
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
        /// <returns>Kết quả thanh toán</returns>
        /// <response code="200">Thanh toán thành công</response>
        /// <response code="400">Thanh toán thất bại hoặc chữ ký không hợp lệ</response>
       [HttpGet("vnpay-return")]
[ProducesResponseType(typeof(object), 200)]
[ProducesResponseType(typeof(object), 400)]
public IActionResult VNPayReturn([FromQuery] VnPayReturnModel model)
{
    // Kiểm tra mã phản hồi từ VNPay
    if (model.vnp_ResponseCode == "00")
    {
        // Thanh toán thành công
        // Lưu thông tin cần thiết vào TempData để hiển thị ở trang Success
        TempData["InvoiceId"] = model.vnp_TxnRef;
        TempData["MovieName"] = model.vnp_OrderInfo;
        TempData["ShowDate"] = DateTime.Now.ToString("dd/MM/yyyy");
        TempData["ShowTime"] = DateTime.Now.ToString("HH:mm");
        TempData["Seats"] = "Ghế đã chọn"; // Thay bằng thông tin thực tế
        TempData["BookingTime"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        TempData["OriginalPrice"] = int.Parse(model.vnp_Amount) / 100; // Chuyển đổi từ số tiền VNPay (x100)
        TempData["UsedScore"] = 0; // Thay bằng số điểm đã dùng nếu có
        TempData["FinalPrice"] = int.Parse(model.vnp_Amount) / 100; // Thay bằng số tiền cuối cùng

                // Redirect về trang Success
                return RedirectToAction("Success", "Booking");
    }
    else
    {
        // Thanh toán thất bại
        // Lưu thông tin cần thiết vào TempData để hiển thị ở trang Failed
        TempData["InvoiceId"] = model.vnp_TxnRef;
        TempData["MovieName"] = model.vnp_OrderInfo;
        TempData["ShowDate"] = DateTime.Now.ToString("dd/MM/yyyy");
        TempData["ShowTime"] = DateTime.Now.ToString("HH:mm");
        TempData["Seats"] = "Ghế đã chọn"; // Thay bằng thông tin thực tế
        TempData["BookingTime"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        // Redirect về trang Failed
        return RedirectToAction("Failed", "Booking");
    }
}

        /// <summary>
        /// Nhận callback IPN (server-to-server) từ VNPay
        /// </summary>
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