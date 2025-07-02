using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Service;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Repository;
using System.Threading.Tasks;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly MovieTheaterContext _context;
        private readonly IVoucherService _voucherService;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAccountService _accountService;


        public TicketController(MovieTheaterContext context, IInvoiceRepository invoiceRepository, IAccountService accountService, IVoucherService voucherService)
        {
            _invoiceRepository = invoiceRepository;
            _context = context;
            _accountService = accountService;
            _voucherService = voucherService;
        }
        /// <summary>
        /// Chuyển hướng lịch sử vé sang trang Index
        /// </summary>
        /// <remarks>url: /Ticket/History (GET)</remarks>
        [HttpGet]
        public IActionResult History()
        {
            // Redirect /Ticket/History to /Ticket/Index
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xem tất cả vé đã đặt
        /// </summary>
        /// <remarks>url: /Ticket/Index (GET)</remarks>
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


        /// <summary>
        /// Xem vé đã đặt (lọc trạng thái completed)
        /// </summary>
        /// <remarks>url: /Ticket/Booked (GET)</remarks>
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

        /// <summary>
        /// Xem vé đã hủy (lọc trạng thái incomplete)
        /// </summary>
        /// <remarks>url: /Ticket/Canceled (GET)</remarks>
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

        /// <summary>
        /// Xem chi tiết vé
        /// </summary>
        /// <remarks>url: /Ticket/Details (GET)</remarks>
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

        /// <summary>
        /// Hủy vé đã đặt
        /// </summary>
        /// <remarks>url: /Ticket/Cancel (POST)</remarks>
        [HttpPost]
        public async Task<IActionResult> Cancel(string id, string returnUrl)
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
                booking.AddScore = 0;
            }

            if (booking.UseScore.HasValue && booking.UseScore.Value > 0)
            {
                await _accountService.AddScoreAsync(accountId, booking.UseScore.Value, false);
                booking.UseScore = 0;
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

        /// <summary>
        /// Xem lịch sử vé theo khoảng ngày và trạng thái
        /// </summary>
        /// <remarks>url: /Ticket/HistoryPartial (GET)</remarks>
        [HttpGet]
        public async Task<IActionResult> HistoryPartial(DateTime? fromDate, DateTime? toDate, string status = "all")
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Json(new { success = false, message = "Not logged in." });

            var invoices = await _invoiceRepository.GetByAccountIdAsync(accountId);

            if (fromDate.HasValue)
                invoices = invoices.Where(i => i.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                invoices = invoices.Where(i => i.BookingDate <= toDate.Value);
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                if (status == "booked")
                    invoices = invoices.Where(i => i.Status == InvoiceStatus.Completed);
                else if (status == "canceled")
                    invoices = invoices.Where(i => i.Status == InvoiceStatus.Incomplete);
            }

            var result = invoices
                .OrderByDescending(i => i.BookingDate)
                .Select(i => new {
                    invoiceId = i.InvoiceId,
                    bookingDate = i.BookingDate,
                    seat = i.Seat,
                    totalMoney = i.TotalMoney,
                    status = i.Status,
                    MovieShow = i.MovieShow == null ? null : new {
                        showDate = i.MovieShow.ShowDate,
                        Movie = i.MovieShow.Movie == null ? null : new {
                            MovieNameEnglish = i.MovieShow.Movie.MovieNameEnglish
                        },
                        Schedule = i.MovieShow.Schedule == null ? null : new {
                            ScheduleTime = i.MovieShow.Schedule.ScheduleTime
                        }
                    }
                }).ToList();

            return Json(new { success = true, data = result });
        }

        /// <summary>
        /// Test chức năng (dùng cho dev)
        /// </summary>
        /// <remarks>url: /Ticket/Test (GET)</remarks>
        public IActionResult Test()
        {
            return Content("Test OK");
        }

        /// <summary>
        /// Hủy vé bởi admin
        /// </summary>
        /// <remarks>url: /Ticket/CancelByAdmin (POST)</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelByAdmin(string id, string returnUrl)
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
                booking.AddScore = 0;
            }

            if (booking.UseScore.HasValue && booking.UseScore.Value > 0)
            {
                await _accountService.AddScoreAsync(booking.AccountId, booking.UseScore.Value, false);
                booking.UseScore = 0;
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