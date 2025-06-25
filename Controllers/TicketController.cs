using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Authorization;
using MovieTheater.Service;
using System.Threading.Tasks;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly MovieTheaterContext _context;
        private readonly IAccountService _accountService;

        public TicketController(MovieTheaterContext context, IAccountService accountService)
        {
            _context = context;
            _accountService = accountService;
        }
        [HttpGet]
        public IActionResult History()
        {
            // Redirect /Ticket/History to /Ticket/Index
            return RedirectToAction("Index");
        }
        // AC-01: View all booked tickets
        [HttpGet]
        public IActionResult Index()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View(bookings);
        }


        // AC-01: View booked tickets with filtering
        [HttpGet]
        public IActionResult Booked()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId && i.Status == MovieTheater.Models.InvoiceStatus.Completed)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View("Index", bookings);
        }

        // AC-01: View canceled tickets
        [HttpGet]
        public IActionResult Canceled()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId && i.Status == MovieTheater.Models.InvoiceStatus.Incomplete)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View("Index", bookings);
        }

        // AC-01: View ticket details
        [HttpGet]
        public IActionResult Details(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = _context.Invoices
                .Include(i => i.ScheduleSeats)
                    .ThenInclude(ss => ss.Seat)
                        .ThenInclude(s => s.SeatType)
                .Include(i => i.ScheduleSeats)
                    .ThenInclude(ss => ss.MovieShow)
                        .ThenInclude(ms => ms.CinemaRoom)
                .FirstOrDefault(i => i.InvoiceId == id && i.AccountId == accountId);

            if (booking == null)
            {
                return NotFound();
            }

            // Tạo danh sách ghế chi tiết
            var seatDetails = booking.ScheduleSeats.Select(ss => new SeatDetailViewModel
            {
                SeatId = ss.Seat.SeatId,
                SeatName = ss.Seat.SeatName,
                SeatType = ss.Seat.SeatType?.TypeName,
                Price = (decimal)(ss.Seat.SeatType?.PricePercent)
            }).ToList();

            ViewBag.SeatDetails = seatDetails;

            return View(booking);
        }

        // AC-04: Cancel ticket
        [HttpPost]
        public async Task<IActionResult> Cancel(string id, string returnUrl, [FromServices] Service.IVoucherService voucherService)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = _context.Invoices
                .FirstOrDefault(i => i.InvoiceId == id && i.AccountId == accountId);

            if (booking == null)
            {
                return NotFound();
            }

            // Only allow cancel if paid, not already cancelled
            if (booking.Status != InvoiceStatus.Completed)
            {
                TempData["ErrorMessage"] = "Only paid bookings can be cancelled.";
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }
            // (Showtime check is commented out as per your logic)
            if (booking.Status == InvoiceStatus.Incomplete)
            {
                TempData["ErrorMessage"] = "This ticket has already been cancelled.";
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }

            // Mark as cancelled
            booking.Status = InvoiceStatus.Incomplete;

            // Update schedule seats: mark as available again
            var scheduleSeatsToUpdate = _context.ScheduleSeats
                .Where(s => s.InvoiceId == booking.InvoiceId)
                .ToList();

            foreach (var seat in scheduleSeatsToUpdate)
            {
                seat.SeatStatusId = 1; // Available
            }

            // Handle score operations
            if (booking.AddScore.HasValue && booking.AddScore.Value > 0)
            {
                // Trả lại điểm đã cộng khi đặt vé
                await _accountService.DeductScoreAsync(accountId, booking.AddScore.Value);
            }

            if (booking.UseScore.HasValue && booking.UseScore.Value > 0)
            {
                // Trả lại điểm đã sử dụng khi đặt vé
                await _accountService.AddScoreAsync(accountId, booking.UseScore.Value);
            }

            _context.SaveChanges();

            // Create voucher with code 'REFUND'
            var voucher = new Voucher
            {
                VoucherId = voucherService.GenerateVoucherId(),
                AccountId = accountId,
                Code = "REFUND",
                Value = booking.TotalMoney ?? 0,
                RemainingValue = booking.TotalMoney ?? 0,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false,
                Image = "/voucher-img/refund-voucher.jpg"
            };
            voucherService.Add(voucher);

            TempData["ToastMessage"] = $"Ticket cancelled successfully. Voucher value: {voucher.Value:N0} VND (valid for 30 days).";
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult HistoryPartial(DateTime? fromDate, DateTime? toDate)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Content("<div class='alert alert-danger'>Not logged in.</div>", "text/html");

            var query = _context.Invoices.Where(i => i.AccountId == accountId);
            if (fromDate.HasValue)
                query = query.Where(i => i.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(i => i.BookingDate <= toDate.Value);
            var result = query.ToList();
            return PartialView("~/Views/Account/Tabs/_HistoryPartial.cshtml", result);
        }

        public IActionResult Test()
        {
            return Content("Test OK");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelByAdmin(string id, string returnUrl, [FromServices] IVoucherService voucherService)
        {
            var booking = _context.Invoices.FirstOrDefault(i => i.InvoiceId == id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("TicketInfo", "Booking", new { invoiceId = id });
            }

            if (booking.Status == InvoiceStatus.Incomplete)
            {
                TempData["ErrorMessage"] = "This ticket has already been cancelled.";
                return RedirectToAction("TicketInfo", "Booking", new { invoiceId = id });
            }

            // ✅ Cập nhật trạng thái
            booking.Status = InvoiceStatus.Incomplete;

            // ✅ Lấy danh sách ghế cần cập nhật
            var scheduleSeatsToUpdate = _context.ScheduleSeats
                .Where(s => s.InvoiceId != null && s.InvoiceId == booking.InvoiceId)
                .ToList();

            // ✅ Cập nhật trạng thái ghế
            foreach (var seat in scheduleSeatsToUpdate)
            {
                seat.SeatStatusId = 1; // Trạng thái "available"
            }

            // ✅ Xử lý điểm
            if (booking.AddScore.HasValue && booking.AddScore.Value > 0)
            {
                // Trả lại điểm đã cộng khi đặt vé
                await _accountService.DeductScoreAsync(booking.AccountId, booking.AddScore.Value);
            }

            if (booking.UseScore.HasValue && booking.UseScore.Value > 0)
            {
                // Trả lại điểm đã sử dụng khi đặt vé
                await _accountService.AddScoreAsync(booking.AccountId, booking.UseScore.Value);
            }

            _context.SaveChanges(); // Lưu lại trước khi tạo voucher

            // ✅ Tạo voucher hoàn tiền
            var voucher = new Voucher
            {
                VoucherId = voucherService.GenerateVoucherId(),
                AccountId = booking.AccountId,
                Code = "REFUND",
                Value = booking.TotalMoney ?? 0,
                RemainingValue = booking.TotalMoney ?? 0,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false,
                Image = "/voucher-img/refund-voucher.jpg"
            };
            voucherService.Add(voucher);

            TempData["ToastMessage"] = $"Ticket cancelled successfully. Voucher value: {voucher.Value:N0} VND (valid for 30 days).";

            // Redirect về TicketInfo thay vì returnUrl
            return RedirectToAction("TicketInfo", "Booking", new { invoiceId = id });
        }
    }
}