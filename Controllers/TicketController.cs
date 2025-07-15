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
using MovieTheater.Hubs;

namespace MovieTheater.Controllers
{
    public class TicketController : Controller
    {
        private readonly MovieTheaterContext _context;
        private readonly IVoucherService _voucherService;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAccountService _accountService;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        private readonly IFoodInvoiceService _foodInvoiceService;


        public TicketController(MovieTheaterContext context, IInvoiceRepository invoiceRepository, IAccountService accountService, IVoucherService voucherService, IHubContext<DashboardHub> dashboardHubContext, IFoodInvoiceService foodInvoiceService)
        {
            _invoiceRepository = invoiceRepository;
            _context = context;
            _accountService = accountService;
            _voucherService = voucherService;
            _dashboardHubContext = dashboardHubContext;
            _foodInvoiceService = foodInvoiceService;
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
            decimal promotionDiscount = booking.PromotionDiscount ?? 0;

            // Always try to load ScheduleSeat records for this invoice
            if (!string.IsNullOrEmpty(booking.SeatIds))
            {
                var seatIds = booking.SeatIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
                var allSeats = _context.Seats.Include(s => s.SeatType).Where(s => seatIds.Contains(s.SeatId)).ToList();
                seatDetails = allSeats.Select(seat => {
                    var seatType = seat.SeatType;
                    decimal price = seatType?.PricePercent ?? 0;
                    if (seatType != null && booking?.MovieShow?.Version != null)
                    {
                        price = (decimal)(seatType.PricePercent * booking.MovieShow.Version.Multi);
                    }
                    decimal priceAfterPromotion = price;
                    if (promotionDiscount > 0)
                    {
                        priceAfterPromotion = price * (1 - promotionDiscount / 100m);
                    }
                    return new SeatDetailViewModel
                    {
                        SeatId = seat.SeatId,
                        SeatName = seat.SeatName,
                        SeatType = seatType?.TypeName ?? "N/A",
                        Price = priceAfterPromotion,
                        OriginalPrice = price,
                        PromotionDiscount = promotionDiscount,
                        PriceAfterPromotion = priceAfterPromotion
                    };
                }).ToList();
            }
            else {
                // fallback to old logic if SeatIds is missing
                var scheduleSeats = _context.ScheduleSeats
                    .Include(ss => ss.Seat)
                    .Include(ss => ss.BookedSeatType)
                    .Where(ss => ss.InvoiceId == booking.InvoiceId)
                    .ToList();

                if (scheduleSeats.Any())
                {
                    seatDetails = scheduleSeats
                        .Where(ss => ss.Seat != null)
                        .Select(ss =>
                        {
                            var seatType = ss.BookedSeatType ?? ss.Seat.SeatType;
                            decimal price = ss.BookedPrice ?? 0;
                            decimal priceAfterPromotion = price;

                            // Only recalculate if BookedPrice is null or 0 (for legacy data)
                            if ((price == 0 || price == null) && seatType != null && booking?.MovieShow?.Version != null)
                            {
                                price = (decimal)(seatType.PricePercent * booking.MovieShow.Version.Multi);
                                priceAfterPromotion = price;
                                if (promotionDiscount > 0)
                                {
                                    priceAfterPromotion = price * (1 - promotionDiscount / 100m);
                                }
                            }
                            return new SeatDetailViewModel
                            {
                                SeatId = ss.Seat.SeatId,
                                SeatName = ss.Seat.SeatName,
                                SeatType = seatType?.TypeName ?? "N/A",
                                Price = priceAfterPromotion,
                                OriginalPrice = price,
                                PromotionDiscount = promotionDiscount,
                                PriceAfterPromotion = priceAfterPromotion
                            };
                        }).ToList();
                }
                else if (!string.IsNullOrEmpty(booking.Seat))
                {
                    var seatNamesArr = booking.Seat.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
                    foreach (var seatName in seatNamesArr)
                    {
                        var seat = _context.Seats.Include(s => s.SeatType).FirstOrDefault(s => s.SeatName == seatName);
                        if (seat == null) continue;
                        var seatType = seat.SeatType;
                        decimal price = seatType?.PricePercent ?? 0;
                        if (seatType != null && booking?.MovieShow?.Version != null)
                        {
                            price = (decimal)(seatType.PricePercent * booking.MovieShow.Version.Multi);
                        }
                        decimal priceAfterPromotion = price;
                        if (promotionDiscount > 0)
                        {
                            priceAfterPromotion = price * (1 - promotionDiscount / 100m);
                        }
                        seatDetails.Add(new SeatDetailViewModel
                        {
                            SeatId = seat.SeatId,
                            SeatName = seat.SeatName,
                            SeatType = seatType?.TypeName ?? "N/A",
                            Price = priceAfterPromotion,
                            OriginalPrice = price,
                            PromotionDiscount = promotionDiscount,
                            PriceAfterPromotion = priceAfterPromotion
                        });
                    }
                }
            }
            ViewBag.SeatDetails = seatDetails;

            // Truyền voucher info vào ViewBag nếu có
            if (booking.Voucher != null)
            {
                ViewBag.VoucherAmount = booking.Voucher.Value;
                ViewBag.VoucherCode = booking.Voucher.Code;
            }

            // Lấy thông tin food từ FoodInvoice
            var selectedFoods = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(id)).ToList();
            ViewBag.SelectedFoods = selectedFoods;
            ViewBag.TotalFoodPrice = selectedFoods.Sum(f => f.Price * f.Quantity);

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
            await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");

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
                .Select(i => new
                {
                    invoiceId = i.InvoiceId,
                    bookingDate = i.BookingDate,
                    seat = i.Seat,
                    totalMoney = i.TotalMoney,
                    status = i.Status,
                    MovieShow = i.MovieShow == null ? null : new
                    {
                        showDate = i.MovieShow.ShowDate,
                        Movie = i.MovieShow.Movie == null ? null : new
                        {
                            MovieNameEnglish = i.MovieShow.Movie.MovieNameEnglish
                        },
                        Schedule = i.MovieShow.Schedule == null ? null : new
                        {
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
            await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");

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