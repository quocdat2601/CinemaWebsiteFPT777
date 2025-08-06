using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class QRCodeController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IScheduleSeatRepository _scheduleSeatRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly ISeatService _seatService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly ILogger<QRCodeController> _logger;
        private readonly ITicketVerificationService _ticketVerificationService;

        public QRCodeController(
            IInvoiceService invoiceService,
            IScheduleSeatRepository scheduleSeatRepository,
            IMemberRepository memberRepository,
            ISeatService seatService,
            ISeatTypeService seatTypeService,
            ILogger<QRCodeController> logger,
            ITicketVerificationService ticketVerificationService)
        {
            _invoiceService = invoiceService;
            _scheduleSeatRepository = scheduleSeatRepository;
            _memberRepository = memberRepository;
            _seatService = seatService;
            _seatTypeService = seatTypeService;
            _logger = logger;
            _ticketVerificationService = ticketVerificationService;
        }

        /// <summary>
        /// Trang quét QR code cho Admin/Employee
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult Scanner() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            return View();
        }

        /// <summary>
        /// API để xác nhận vé từ QR code
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public IActionResult VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = _ticketVerificationService.VerifyTicket(request.InvoiceId);
            if (result.IsSuccess)
            {
                return Json(new { success = true, message = result.Message, ticketInfo = result });
            }
            else
            {
                return Json(new { success = false, message = result.Message });
            }
        }

        /// <summary>
        /// Trang hiển thị kết quả xác nhận vé
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult VerificationResult(string invoiceId, string result) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var member = _memberRepository.GetByAccountId(invoice.AccountId);
            var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            var viewModel = new TicketVerificationResultViewModel
            {
                InvoiceId = invoice.InvoiceId,
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                ShowDate = invoice.MovieShow.ShowDate.ToString(),
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                CustomerName = member?.Account?.FullName ?? "N/A",
                CustomerPhone = member?.Account?.PhoneNumber ?? "N/A",
                Seats = string.Join(", ", seatNames),
                TotalAmount = invoice.TotalMoney?.ToString("N0") + " VND",
                IsSuccess = result == "success",
                VerificationTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            return View(viewModel);
        }

        /// <summary>
        /// API để lấy thông tin vé từ QR code (không xác nhận)
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult GetTicketInfo(string invoiceId) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var result = _ticketVerificationService.GetTicketInfo(invoiceId);
            if (result.IsSuccess)
            {
                return Json(new { success = true, ticketInfo = result });
            }
            else
            {
                return Json(new { success = false, message = result.Message });
            }
        }

        /// <summary>
        /// Xác nhận check-in vé (sau khi quét QR và kiểm tra hợp lệ)
        /// </summary>
        /// <remarks>url: /QRCode/ConfirmCheckIn (POST)</remarks>
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public IActionResult ConfirmCheckIn([FromBody] ConfirmCheckInRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var staffId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
            var result = _ticketVerificationService.ConfirmCheckIn(request.InvoiceId, staffId);
            if (result.IsSuccess)
            {
                return Json(new { success = true, message = result.Message });
            }
            else
            {
                return Json(new { success = false, message = result.Message });
            }
        }

        public class ConfirmCheckInRequest
        {
            public string InvoiceId { get; set; }
        }

        public class CheckInLog
        {
            public int Id { get; set; }
            public string InvoiceId { get; set; }
            public string StaffId { get; set; }
            public DateTime CheckInTime { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
        }
    }

    public class VerifyTicketRequest { public string InvoiceId { get; set; } }
}