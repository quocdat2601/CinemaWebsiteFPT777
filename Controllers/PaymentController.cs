using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly VNPayService _vnPayService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IAccountService _accountService;
        private readonly MovieTheater.Models.MovieTheaterContext _context;

        public PaymentController(VNPayService vnPayService, ILogger<PaymentController> logger, IAccountService accountService, MovieTheater.Models.MovieTheaterContext context)
        {
            _vnPayService = vnPayService;
            _logger = logger;
            _accountService = accountService;
            _context = context;
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
        public IActionResult VNPayReturn([FromQuery] VnPayReturnModel model)
        {
            int? movieShowId = null; // Khai báo duy nhất ở đây
            var invoice = _context.Invoices
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
                        // Fetch member's earning rate
                        var member = _context.Members.Include(m => m.Account).ThenInclude(a => a.Rank).FirstOrDefault(m => m.AccountId == invoice.AccountId);
                        decimal earningRate = 1;
                        if (member?.Account?.Rank != null)
                            earningRate = member.Account.Rank.PointEarningPercentage ?? 1;

                        int addScore = new MovieTheater.Service.PointService().CalculatePointsToEarn(invoice.TotalMoney ?? 0, earningRate);
                        invoice.AddScore = addScore;
                        if (member != null)
                        {
                            member.Score += addScore;
                            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0)
                            {
                                member.Score -= invoice.UseScore.Value;
                            }
                        }
                    }
                    _context.Invoices.Update(invoice);
                    _context.SaveChanges();
                    _accountService.CheckAndUpgradeRank(invoice.AccountId);
                }
                // --- BẮT ĐẦU: Thêm bản ghi vào Schedule_Seat nếu chưa có ---
                if (invoice != null && !string.IsNullOrEmpty(invoice.Seat))
                {
                    // Gán giá trị, không khai báo lại biến movieShowId
                    if (TempData["MovieShowId"] != null)
                        movieShowId = Convert.ToInt32(TempData["MovieShowId"]);

                    var seatNames = invoice.Seat.Split(',');
                    foreach (var seatName in seatNames)
                    {
                        var seat = _context.Seats.FirstOrDefault(s => s.SeatName == seatName);
                        if (seat != null && movieShowId.HasValue)
                        {
                            var exist = _context.ScheduleSeats.FirstOrDefault(ss => ss.MovieShowId == movieShowId && ss.SeatId == seat.SeatId && ss.InvoiceId == invoice.InvoiceId);
                            if (exist == null)
                            {
                                var scheduleSeat = new Models.ScheduleSeat
                                {
                                    MovieShowId = movieShowId.Value,
                                    InvoiceId = invoice.InvoiceId,
                                    SeatId = seat.SeatId,
                                    SeatStatusId = 2 // Booked
                                };
                                _context.ScheduleSeats.Add(scheduleSeat);
                            }
                        }
                    }
                    _context.SaveChanges();
                }
                // --- KẾT THÚC: Thêm bản ghi vào Schedule_Seat nếu chưa có ---
                TempData["InvoiceId"] = model.vnp_TxnRef;
                TempData["MovieName"] = invoice?.MovieShow.Movie.MovieNameEnglish ?? "";
                TempData["ShowDate"] = invoice?.MovieShow.ShowDate.ToString("dd/MM/yyyy") ?? "N/A";
                TempData["ShowTime"] = invoice?.MovieShow.Schedule.ScheduleTime.ToString() ?? "N/A";
                TempData["Seats"] = invoice?.Seat ?? "N/A";
                // Lấy CinemaRoomName trực tiếp từ MovieShowId (TempData)
                if (movieShowId.HasValue)
                {
                    var movieShow = _context.MovieShows
                        .Include(ms => ms.CinemaRoom)
                        .FirstOrDefault(ms => ms.MovieShowId == movieShowId.Value);
                    TempData["CinemaRoomName"] = movieShow?.CinemaRoom?.CinemaRoomName ?? "N/A";
                }
                else
                {
                    TempData["CinemaRoomName"] = "N/A";
                }
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
                    _context.Invoices.Update(invoice);
                    _context.SaveChanges();
                }
                TempData["InvoiceId"] = model.vnp_TxnRef;
                TempData["MovieName"] = invoice?.MovieShow.Movie.MovieNameEnglish ?? "";
                TempData["ShowDate"] = invoice?.MovieShow.ShowDate.ToString("dd/MM/yyyy") ?? "N/A";
                TempData["ShowTime"] = invoice?.MovieShow.Schedule.ScheduleTime.ToString() ?? "N/A";
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