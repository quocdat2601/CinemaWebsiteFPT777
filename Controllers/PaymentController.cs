using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using System.Linq;
using System.Collections.Generic;
using MovieTheater.ViewModels;
using Microsoft.EntityFrameworkCore;

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
            var context = new MovieTheater.Models.MovieTheaterContext();
            var invoice = context.Invoices
                .Include(i => i.ScheduleSeats)
                .ThenInclude(ss => ss.MovieShow)
                .ThenInclude(ms => ms.CinemaRoom)
                .FirstOrDefault(i => i.InvoiceId == model.vnp_TxnRef);
            if (model.vnp_ResponseCode == "00")
            {
                // Thanh toán thành công
                if (invoice != null)
                {
                    invoice.Status = MovieTheater.Models.InvoiceStatus.Completed;
                    if (invoice.AddScore == 0)
                    {
                        int addScore = (int)((invoice.TotalMoney ?? 0) * 0.01m);
                        invoice.AddScore = addScore;
                        var member = context.Members.FirstOrDefault(m => m.AccountId == invoice.AccountId);
                        if (member != null)
                        {
                            member.Score += addScore;
                            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0)
                            {
                                member.Score -= invoice.UseScore.Value;
                            }
                        }
                    }
                    context.Invoices.Update(invoice);
                    context.SaveChanges();
                }
                TempData["InvoiceId"] = model.vnp_TxnRef;
                TempData["MovieName"] = invoice?.MovieName ?? "";
                TempData["ShowDate"] = invoice?.ScheduleShow?.ToString("dd/MM/yyyy") ?? "N/A";
                TempData["ShowTime"] = invoice?.ScheduleShowTime ?? "N/A";
                TempData["Seats"] = invoice?.Seat ?? "N/A";
                TempData["CinemaRoomName"] = invoice?.ScheduleSeats.FirstOrDefault()?.MovieShow?.CinemaRoom?.CinemaRoomName ?? "N/A";
                TempData["BookingTime"] = invoice?.BookingDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
                TempData["OriginalPrice"] = (int.Parse(model.vnp_Amount) / 100).ToString();
                TempData["UsedScore"] = invoice?.UseScore ?? 0;
                TempData["FinalPrice"] = (invoice?.TotalMoney ?? 0).ToString();
                return RedirectToAction("Success", "Booking");
            }
            else
            {
                if (invoice != null)
                {
                    invoice.Status = MovieTheater.Models.InvoiceStatus.Incomplete;
                    context.Invoices.Update(invoice);
                    context.SaveChanges();
                }
                TempData["InvoiceId"] = model.vnp_TxnRef;
                TempData["MovieName"] = invoice?.MovieName ?? "";
                TempData["ShowDate"] = invoice?.ScheduleShow?.ToString("dd/MM/yyyy") ?? "N/A";
                TempData["ShowTime"] = invoice?.ScheduleShowTime ?? "N/A";
                TempData["Seats"] = invoice?.Seat ?? "N/A";
                TempData["BookingTime"] = invoice?.BookingDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
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