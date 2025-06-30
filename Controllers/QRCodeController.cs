using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Text.Json;

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

        public QRCodeController(
            IInvoiceService invoiceService,
            IScheduleSeatRepository scheduleSeatRepository,
            IMemberRepository memberRepository,
            ISeatService seatService,
            ISeatTypeService seatTypeService,
            ILogger<QRCodeController> logger)
        {
            _invoiceService = invoiceService;
            _scheduleSeatRepository = scheduleSeatRepository;
            _memberRepository = memberRepository;
            _seatService = seatService;
            _seatTypeService = seatTypeService;
            _logger = logger;
        }

        /// <summary>
        /// Trang quét QR code cho Admin/Employee
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult Scanner()
        {
            return View();
        }

        /// <summary>
        /// API để xác nhận vé từ QR code
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<IActionResult> VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.InvoiceId))
                {
                    return Json(new { success = false, message = "Invalid QR code" });
                }

                var invoice = _invoiceService.GetById(request.InvoiceId);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Ticket not found" });
                }

                // Kiểm tra trạng thái vé
                if (invoice.Status != InvoiceStatus.Completed)
                {
                    return Json(new { success = false, message = "Ticket is not paid" });
                }

                // Kiểm tra xem vé đã được sử dụng chưa
                var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(request.InvoiceId).ToList();
                if (scheduleSeats.Any(ss => ss.SeatStatusId == 2)) // 2 = Booked (đã check-in)
                {
                    return Json(new { success = false, message = "Ticket has already been used" });
                }

                // Đánh dấu vé đã sử dụng
                foreach (var seat in scheduleSeats)
                {
                    seat.SeatStatusId = 2; // Booked (dùng luôn cho check-in)
                    _scheduleSeatRepository.Update(seat);
                }
                _scheduleSeatRepository.Save();

                // Lấy thông tin chi tiết để trả về
                var member = _memberRepository.GetByAccountId(invoice.AccountId);
                var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                var seatDetails = new List<object>();
                foreach (var seatName in seatNames)
                {
                    var seat = _seatService.GetSeatByName(seatName);
                    if (seat != null)
                    {
                        var seatType = seat.SeatTypeId.HasValue ? _seatTypeService.GetById(seat.SeatTypeId.Value) : null;
                        seatDetails.Add(new
                        {
                            seatName = seat.SeatName,
                            seatType = seatType?.TypeName ?? "Standard"
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Ticket verified successfully!",
                    ticketInfo = new
                    {
                        invoiceId = invoice.InvoiceId,
                        movieName = invoice.MovieName,
                        showDate = invoice.ScheduleShow?.ToString("dd/MM/yyyy"),
                        showTime = invoice.ScheduleShowTime,
                        customerName = member?.Account?.FullName ?? "N/A",
                        customerPhone = member?.Account?.PhoneNumber ?? "N/A",
                        seats = seatDetails,
                        totalAmount = invoice.TotalMoney?.ToString("N0") + " VND"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying ticket");
                return Json(new { success = false, message = "An error occurred while verifying the ticket" });
            }
        }

        /// <summary>
        /// Trang hiển thị kết quả xác nhận vé
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public IActionResult VerificationResult(string invoiceId, string result)
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
                MovieName = invoice.MovieName,
                ShowDate = invoice.ScheduleShow?.ToString("dd/MM/yyyy"),
                ShowTime = invoice.ScheduleShowTime,
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
        public IActionResult GetTicketInfo(string invoiceId)
        {
            try
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Ticket not found" });
                }

                var member = _memberRepository.GetByAccountId(invoice.AccountId);
                var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                var seatDetails = new List<object>();
                foreach (var seatName in seatNames)
                {
                    var seat = _seatService.GetSeatByName(seatName);
                    if (seat != null)
                    {
                        var seatType = seat.SeatTypeId.HasValue ? _seatTypeService.GetById(seat.SeatTypeId.Value) : null;
                        seatDetails.Add(new
                        {
                            seatName = seat.SeatName,
                            seatType = seatType?.TypeName ?? "Standard"
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    ticketInfo = new
                    {
                        invoiceId = invoice.InvoiceId,
                        movieName = invoice.MovieName,
                        showDate = invoice.ScheduleShow?.ToString("dd/MM/yyyy"),
                        showTime = invoice.ScheduleShowTime,
                        customerName = member?.Account?.FullName ?? "N/A",
                        customerPhone = member?.Account?.PhoneNumber ?? "N/A",
                        seats = seatDetails,
                        totalAmount = invoice.TotalMoney?.ToString("N0") + " VND",
                        status = invoice.Status.ToString(),
                        isUsed = _scheduleSeatRepository.GetByInvoiceId(invoiceId).Any(ss => ss.SeatStatusId == 2)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticket info");
                return Json(new { success = false, message = "An error occurred while getting ticket info" });
            }
        }

        /// <summary>
        /// API xác nhận check-in vé (sau khi đã quét QR và kiểm tra hợp lệ)
        /// </summary>
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public IActionResult ConfirmCheckIn([FromBody] ConfirmCheckInRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.InvoiceId))
                {
                    return Json(new { success = false, message = "Invalid QR code" });
                }

                var invoice = _invoiceService.GetById(request.InvoiceId);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Ticket not found" });
                }

                // Kiểm tra trạng thái vé
                if (invoice.Status != InvoiceStatus.Completed)
                {
                    return Json(new { success = false, message = "Ticket is not paid" });
                }

                // Kiểm tra xem vé đã được sử dụng chưa
                var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(request.InvoiceId).ToList();
                if (scheduleSeats.Any(ss => ss.SeatStatusId == 2)) // 2 = Booked (đã check-in)
                {
                    return Json(new { success = false, message = "Ticket has already been checked in" });
                }

                // Đánh dấu vé đã sử dụng
                foreach (var seat in scheduleSeats)
                {
                    seat.SeatStatusId = 2; // Booked (dùng luôn cho check-in)
                    _scheduleSeatRepository.Update(seat);
                }
                _scheduleSeatRepository.Save();

                // Ghi log check-in
                var staffId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                var log = new CheckInLog
                {
                    InvoiceId = invoice.InvoiceId,
                    StaffId = staffId,
                    CheckInTime = DateTime.Now,
                    Status = "CheckedIn",
                    Note = "Check-in by staff via QR"
                };
                // TODO: Lưu log vào database (bạn cần tạo repository cho CheckInLog)

                return Json(new { success = true, message = "Check-in successful!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming check-in");
                return Json(new { success = false, message = "An error occurred while confirming check-in" });
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

    public class VerifyTicketRequest
    {
        public string InvoiceId { get; set; }
    }

    public class TicketVerificationResultViewModel
    {
        public string InvoiceId { get; set; }
        public string MovieName { get; set; }
        public string ShowDate { get; set; }
        public string ShowTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string Seats { get; set; }
        public string TotalAmount { get; set; }
        public bool IsSuccess { get; set; }
        public string VerificationTime { get; set; }
    }
} 