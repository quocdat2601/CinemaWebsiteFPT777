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
using Microsoft.AspNetCore.SignalR;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly MovieTheaterContext _context;
        private readonly IAccountService _accountService;
        private readonly IMovieService _movieService;
        private readonly ICinemaService _cinemaService;
        private readonly IVoucherService _voucherService;

        public TicketController(MovieTheaterContext context, IAccountService accountService, IMovieService movieService, ICinemaService cinemaService, IVoucherService voucherService)
        {
            _context = context;
            _accountService = accountService;
            _movieService = movieService;
            _cinemaService = cinemaService;
            _voucherService = voucherService;
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
                .Include(i => i.Account)
                    .ThenInclude(a => a.Rank)
                .Include(i => i.Voucher)
                .FirstOrDefault(i => i.InvoiceId == id && i.AccountId == accountId);

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
        public async Task<IActionResult> Cancel(string id, string returnUrl)
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
                // Phát sự kiện SignalR cho từng ghế trả lại
                if (seat.MovieShowId.HasValue && seat.SeatId.HasValue)
                {
                    var hubContext = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<MovieTheater.Hubs.SeatHub>)) as Microsoft.AspNetCore.SignalR.IHubContext<MovieTheater.Hubs.SeatHub>;
                    if (hubContext != null)
                    {
                        await hubContext.Clients.Group(seat.MovieShowId.Value.ToString()).SendAsync("SeatStatusChanged", seat.SeatId.Value, 1);
                    }
                }
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

            // Handle voucher refund - if booking used a voucher, restore it
            var usedVoucher = !string.IsNullOrEmpty(booking.VoucherId) ? _voucherService.GetById(booking.VoucherId) : null;
            if (usedVoucher != null)
            {
                usedVoucher.IsUsed = false; // Restore the used voucher
                _voucherService.Update(usedVoucher);
            }

            _context.SaveChanges();
            _accountService.CheckAndUpgradeRank(accountId);

            // Create refund voucher only if TotalMoney > 0
            Voucher refundVoucher = null;
            if ((booking.TotalMoney ?? 0) > 0)
            {
                refundVoucher = new Voucher
                {
                    VoucherId = _voucherService.GenerateVoucherId(),
                    AccountId = accountId,
                    Code = "REFUND",
                    Value = booking.TotalMoney ?? 0,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(30),
                    IsUsed = false,
                    Image = "/images/vouchers/refund-voucher.jpg"
                };
                _voucherService.Add(refundVoucher);
            }

            // Combine cancellation and rank upgrade notifications (member only)
            var messages = new List<string> { "Ticket cancelled successfully." };
            
            if (refundVoucher != null)
            {
                messages.Add($"Refund voucher value: {refundVoucher.Value:N0} VND (valid for 30 days).");
            }
            
            if (usedVoucher != null)
            {
                messages.Add($"Original voucher '{usedVoucher.Code}' has been restored.");
            }
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
        public async Task<IActionResult> CancelByAdmin(string id, string returnUrl)
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

            // Mark as cancelled
            booking.Status = InvoiceStatus.Incomplete;

            // Update schedule seats: mark as available again
            var scheduleSeatsToUpdate = _context.ScheduleSeats
                .Where(s => s.InvoiceId != null && s.InvoiceId == booking.InvoiceId)
                .ToList();
            foreach (var seat in scheduleSeatsToUpdate)
            {
                seat.SeatStatusId = 1; // Available
                // Phát sự kiện SignalR cho từng ghế trả lại
                if (seat.MovieShowId.HasValue && seat.SeatId.HasValue)
                {
                    var hubContext = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.SignalR.IHubContext<MovieTheater.Hubs.SeatHub>)) as Microsoft.AspNetCore.SignalR.IHubContext<MovieTheater.Hubs.SeatHub>;
                    if (hubContext != null)
                    {
                        await hubContext.Clients.Group(seat.MovieShowId.Value.ToString()).SendAsync("SeatStatusChanged", seat.SeatId.Value, 1);
                    }
                }
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
            // Handle voucher refund - if booking used a voucher, restore it
            var usedVoucher = !string.IsNullOrEmpty(booking.VoucherId) ? _voucherService.GetById(booking.VoucherId) : null;
            if (usedVoucher != null)
            {
                usedVoucher.IsUsed = false; // Restore the used voucher
                _voucherService.Update(usedVoucher);
            }

            _context.SaveChanges();
            _accountService.CheckAndUpgradeRank(booking.AccountId);

            // Create refund voucher only if TotalMoney > 0
            Voucher refundVoucher = null;
            if ((booking.TotalMoney ?? 0) > 0)
            {
                refundVoucher = new Voucher
                {
                    VoucherId = _voucherService.GenerateVoucherId(),
                    AccountId = booking.AccountId,
                    Code = "REFUND",
                    Value = booking.TotalMoney ?? 0,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(30),
                    IsUsed = false,
                    Image = "/images/vouchers/refund-voucher.jpg"
                };
                _voucherService.Add(refundVoucher);
            }

            // Combine cancellation and rank upgrade notifications
            var messages = new List<string> { "Ticket cancelled successfully." };
            
            if (refundVoucher != null)
            {
                messages.Add($"Refund voucher value: {refundVoucher.Value:N0} VND (valid for 30 days).");
            }
            
            if (usedVoucher != null)
            {
                messages.Add($"Original voucher '{usedVoucher.Code}' has been restored.");
            }
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

            // Repopulate TempData["CinemaRoomName"] before redirect
            string roomName = "N/A";
            var allMovies = _movieService.GetAll();
            var movie = allMovies.FirstOrDefault(m => m.MovieNameEnglish == booking.MovieName || m.MovieNameVn == booking.MovieName);
            if (movie != null && movie.CinemaRoomId.HasValue)
            {
                var room = _cinemaService.GetById(movie.CinemaRoomId.Value);
                roomName = room?.CinemaRoomName ?? "N/A";
            }
            TempData["CinemaRoomName"] = roomName;

            // Keep ConfirmedSeats TempData for the next request
            if (TempData["ConfirmedSeats"] != null)
            {
                TempData.Keep("ConfirmedSeats");
            }

            return RedirectToAction("TicketInfo", "Booking", new { invoiceId = id });
        }
    }
}