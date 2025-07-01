using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Authorization;
using MovieTheater.Service;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MovieTheater.Repository;
using System.Threading.Tasks;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly MovieTheaterContext _context;

        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAccountService _accountService;


        public TicketController(MovieTheaterContext context, IInvoiceRepository invoiceRepository, IAccountService accountService)
        {
            _invoiceRepository = invoiceRepository;
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
        public async Task<IActionResult> Index()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId);

            return View(bookings);
        }


        // AC-01: View booked tickets with filtering
        [HttpGet]
        public async Task<IActionResult> Booked()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId, InvoiceStatus.Completed);

            return View("Index", bookings);
        }

        // AC-01: View canceled tickets
        [HttpGet]
        public async Task<IActionResult> Canceled()
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId, 0);

            return View("Index", bookings);
        }

        // AC-01: View ticket details
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _invoiceRepository.GetDetailsAsync(id, accountId);

            if (booking == null)
            {
                return NotFound();
            }

            List<SeatDetailViewModel> seatDetails = new List<SeatDetailViewModel>();
            if (booking.ScheduleSeats != null && booking.ScheduleSeats.Any(ss => ss.Seat != null))
            {
                seatDetails = booking.ScheduleSeats
                    .Where(ss => ss.Seat != null)
                    .Select(ss => new SeatDetailViewModel
                    {
                        SeatId = ss.Seat.SeatId,
                        SeatName = ss.Seat.SeatName,
                        SeatType = ss.Seat.SeatType?.TypeName,
                        Price = (decimal)(ss.Seat.SeatType?.PricePercent ?? 0)
                    }).ToList();
            }
            else if (!string.IsNullOrEmpty(booking.Seat))
            {
                // Fallback: lấy từ chuỗi tên ghế
                var seatNamesArr = booking.Seat.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
                foreach (var seatName in seatNamesArr)
                {
                    var seat = _context.Seats.Include(s => s.SeatType).FirstOrDefault(s => s.SeatName == seatName);
                    if (seat == null) continue;
                    seatDetails.Add(new SeatDetailViewModel
                    {
                        SeatId = seat.SeatId,
                        SeatName = seat.SeatName,
                        SeatType = seat.SeatType?.TypeName ?? "N/A",
                        Price = seat.SeatType?.PricePercent ?? 0
                    });
                }
            }

            ViewBag.SeatDetails = seatDetails;
            
            // Truyền voucher info vào ViewBag nếu có
            if (booking.Voucher != null)
            {
                ViewBag.VoucherAmount = booking.Voucher.Value;
                ViewBag.VoucherCode = booking.Voucher.Code;
            }

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

            var booking = await _invoiceRepository.GetForCancelAsync(id, accountId);

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
                await _accountService.DeductScoreAsync(accountId, booking.AddScore.Value, true);
            }

            if (booking.UseScore.HasValue && booking.UseScore.Value > 0)
            {
                await _accountService.AddScoreAsync(accountId, booking.UseScore.Value, false);
            }

            _context.SaveChanges();
            _accountService.CheckAndUpgradeRank(accountId);

            // Create voucher
            var voucher = new Voucher
            {
                VoucherId = voucherService.GenerateVoucherId(),
                AccountId = accountId,
                Code = "REFUND",
                Value = booking.TotalMoney ?? 0,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false,
                Image = "/images/vouchers/refund-voucher.jpg"
            };
            voucherService.Add(voucher);

            // Combine cancellation and rank upgrade notifications (member only)
            var messages = new List<string> { $"Ticket cancelled successfully. Voucher value: {voucher.Value:N0} VND (valid for 30 days)." };
            var rankUpMsg = _accountService.GetAndClearRankUpgradeNotification(accountId);
            if (!string.IsNullOrEmpty(rankUpMsg))
            {
                messages.Add(rankUpMsg);
            }
            TempData["ToastMessage"] = string.Join("<br/>", messages);

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> HistoryPartial(System.DateTime? fromDate, System.DateTime? toDate)
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Content("<div class='alert alert-danger'>Not logged in.</div>", "text/html");

            var result = await _invoiceRepository.GetByDateRangeAsync(accountId, fromDate, toDate);
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
            var booking = _invoiceRepository.GetById(id);
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

            // Mark as cancelled
            booking.Status = InvoiceStatus.Incomplete;

            // Update schedule seats: mark as available again
            var scheduleSeatsToUpdate = _context.ScheduleSeats
                .Where(s => s.InvoiceId != null && s.InvoiceId == booking.InvoiceId)
                .ToList();
            foreach (var seat in scheduleSeatsToUpdate)
            {
                seat.SeatStatusId = 1; // Available
            }

            // Handle score operations
            if (booking.AddScore.HasValue && booking.AddScore.Value > 0)
            {
                await _accountService.DeductScoreAsync(booking.AccountId, booking.AddScore.Value, true);
            }

            if (booking.UseScore.HasValue && booking.UseScore.Value > 0)
            {
                await _accountService.AddScoreAsync(booking.AccountId, booking.UseScore.Value, false);
            }
            _context.SaveChanges();
            _accountService.CheckAndUpgradeRank(booking.AccountId);

            // Create voucher
            var voucher = new Voucher
            {
                VoucherId = voucherService.GenerateVoucherId(),
                AccountId = booking.AccountId,
                Code = "REFUND",
                Value = booking.TotalMoney ?? 0,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false,
                Image = "/images/vouchers/refund-voucher.jpg"
            };
            voucherService.Add(voucher);

            // Combine cancellation and rank upgrade notifications
            var messages = new List<string> { $"Ticket cancelled successfully. Voucher value: {voucher.Value:N0} VND (valid for 30 days)." };
            var adminRankUpMsg = HttpContext.Session.GetString("RankUpToastMessage");
            if (!string.IsNullOrEmpty(adminRankUpMsg))
            {
                messages.Add(adminRankUpMsg);
                HttpContext.Session.Remove("RankUpToastMessage");
            }
            else
            {
                var rankUpMsg = _accountService.GetAndClearRankUpgradeNotification(booking.AccountId);
                if (!string.IsNullOrEmpty(rankUpMsg))
                {
                    messages.Add(rankUpMsg);
                }
            }
            TempData["ToastMessage"] = string.Join("<br/>", messages);

            TempData["CinemaRoomName"] = booking.MovieShow.CinemaRoom.CinemaRoomName;

            // Keep ConfirmedSeats TempData for the next request
            if (TempData["ConfirmedSeats"] != null)
            {
                TempData.Keep("ConfirmedSeats");
            }

            return RedirectToAction("TicketInfo", "Booking", new { invoiceId = id });
        }
    }
}