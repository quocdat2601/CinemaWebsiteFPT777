using Microsoft.AspNetCore.SignalR;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;

public class TicketService : ITicketService
{
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAccountService _accountService;
    private readonly IHubContext<MovieTheater.Hubs.DashboardHub> _dashboardHubContext;
    private readonly IScheduleSeatRepository _scheduleSeatRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IHubContext<MovieTheater.Hubs.SeatHub> _seatHubContext;
    private readonly IFoodInvoiceService _foodInvoiceService;

    public TicketService(
        IInvoiceRepository invoiceRepository,
        IAccountService accountService,
        IVoucherService voucherService,
        IHubContext<MovieTheater.Hubs.DashboardHub> dashboardHubContext,
        IScheduleSeatRepository scheduleSeatRepository,
        ISeatRepository seatRepository,
        IHubContext<MovieTheater.Hubs.SeatHub> seatHubContext,
        IFoodInvoiceService foodInvoiceService)
    {
        _invoiceRepository = invoiceRepository;
        _accountService = accountService;
        _voucherService = voucherService;
        _dashboardHubContext = dashboardHubContext;
        _scheduleSeatRepository = scheduleSeatRepository;
        _seatRepository = seatRepository;
        _seatHubContext = seatHubContext;
        _foodInvoiceService = foodInvoiceService;
    }

    public async Task<IEnumerable<object>> GetUserTicketsAsync(string accountId, int? status = null)
    {

        InvoiceStatus? invoiceStatus = null;
        if (status.HasValue)
        {
            invoiceStatus = (InvoiceStatus)status.Value;
        }
        var bookings = await _invoiceRepository.GetByAccountIdAsync(accountId, invoiceStatus, null);
        return bookings;
    }

    public async Task<TicketDetailsViewModel> GetTicketDetailsAsync(string ticketId, string accountId)
    {
        var booking = await _invoiceRepository.GetDetailsAsync(ticketId, accountId);
        if (booking == null) return null;

        List<SeatDetailViewModel> seatDetails = new List<SeatDetailViewModel>();
        decimal promotionDiscount = booking.PromotionDiscount ?? 0;
        var versionMulti = booking.MovieShow?.Version?.Multi ?? 1;
        if (!string.IsNullOrEmpty(booking.SeatIds))
        {
            var seatIdArr = booking.SeatIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id.Trim()))
                .ToList();
            foreach (var seatId in seatIdArr)
            {
                var seat = _seatRepository.GetById(seatId);
                if (seat == null) continue;
                var seatType = seat.SeatType;
                decimal originalPrice = (seatType?.PricePercent ?? 0) * versionMulti;
                decimal priceAfterPromotion = originalPrice;
                if (promotionDiscount > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - promotionDiscount / 100m);
                }
                seatDetails.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = promotionDiscount,
                    PriceAfterPromotion = priceAfterPromotion
                });
            }
        }
        else if (booking.ScheduleSeats != null && booking.ScheduleSeats.Any(ss => ss.Seat != null))
        {
            seatDetails = booking.ScheduleSeats
                .Where(ss => ss.Seat != null)
                .Select(ss =>
                {
                    var seat = ss.Seat;
                    var seatType = seat.SeatType;
                    decimal originalPrice = (seatType?.PricePercent ?? 0) * versionMulti;
                    decimal bookedPrice = ss.BookedPrice ?? originalPrice;
                    decimal priceAfterPromotion = bookedPrice;
                    // If you want to show promotion discount, you can compare originalPrice and bookedPrice
                    return new SeatDetailViewModel
                    {
                        SeatId = seat.SeatId,
                        SeatName = seat.SeatName,
                        SeatType = seatType?.TypeName ?? "N/A",
                        Price = bookedPrice,
                        OriginalPrice = originalPrice,
                        PromotionDiscount = (originalPrice > bookedPrice) ? (originalPrice - bookedPrice) : 0,
                        PriceAfterPromotion = bookedPrice
                    };
                }).ToList();
        }
        else if (!string.IsNullOrEmpty(booking.Seat))
        {
            var seatIdArr = booking.SeatIds
               .Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(id => int.Parse(id.Trim()))
               .ToList();
            foreach (var seatId in seatIdArr)
            {
                var seat = _seatRepository.GetById(seatId);
                if (seat == null) continue;
                var seatType = seat.SeatType;
                decimal originalPrice = (seatType?.PricePercent ?? 0) * versionMulti;
                decimal priceAfterPromotion = originalPrice;
                if (promotionDiscount > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - promotionDiscount / 100m);
                }
                seatDetails.Add(new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "N/A",
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = promotionDiscount,
                    PriceAfterPromotion = priceAfterPromotion
                });
            }
        }

        // Fetch food details and total food price
        var foodDetails = (await _foodInvoiceService.GetFoodsByInvoiceIdAsync(ticketId)).ToList();
        var totalFoodPrice = await _foodInvoiceService.GetTotalFoodPriceByInvoiceIdAsync(ticketId);

        var result = new TicketDetailsViewModel
        {
            Booking = booking,
            SeatDetails = seatDetails,
            VoucherAmount = booking.Voucher?.Value,
            VoucherCode = booking.Voucher?.Code,
            Subtotal = 0,
            RankDiscount = 0,
            UsedScoreValue = 0,
            FoodDetails = foodDetails,
            TotalFoodPrice = totalFoodPrice,
            TotalAmount = booking.TotalMoney ?? 0,
            PromotionDiscountPercent = booking.PromotionDiscount ?? 0
        };
        return result;
    }


    public async Task<(bool Success, List<string> Messages)> CancelTicketAsync(string ticketId, string accountId)
    {
        var booking = await _invoiceRepository.GetForCancelAsync(ticketId, accountId);
        if (booking == null)
            return (false, new List<string> { "Booking not found." });

        if (booking.Status != InvoiceStatus.Completed)
            return (false, new List<string> { "Only paid bookings can be cancelled." });
        if (booking.Cancel)
            return (false, new List<string> { "This ticket has already been cancelled." });

        // Đánh dấu đã hủy, không đổi status
        booking.Cancel = true;
        booking.CancelDate = DateTime.Now;
        booking.CancelBy = accountId;

        // Update schedule seats: mark as available again
        var scheduleSeatsToUpdate = _scheduleSeatRepository.GetByInvoiceId(booking.InvoiceId).ToList();
        foreach (var seat in scheduleSeatsToUpdate)
        {
            seat.SeatStatusId = 1; // Available
            _scheduleSeatRepository.Update(seat);
            // Phát sự kiện SignalR cho từng ghế trả lại
            if (seat.MovieShowId.HasValue && seat.SeatId.HasValue)
            {
                await _seatHubContext.Clients.Group(seat.MovieShowId.Value.ToString()).SendAsync("SeatStatusChanged", seat.SeatId.Value, 1);
            }
        }
        _scheduleSeatRepository.Save();

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

        _invoiceRepository.Update(booking);
        _invoiceRepository.Save();
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
                Code = $"REFUND-{booking.InvoiceId}", // Unique code per refund
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
        return (true, messages);
    }

    public async Task<IEnumerable<object>> GetHistoryPartialAsync(string accountId, DateTime? fromDate, DateTime? toDate, string status)
    {
        // Lấy tất cả invoice của user
        var invoices = await _invoiceRepository.GetByAccountIdAsync(accountId, null, null);
        if (fromDate.HasValue)
            invoices = invoices.Where(i => i.BookingDate >= fromDate.Value);
        if (toDate.HasValue)
            invoices = invoices.Where(i => i.BookingDate <= toDate.Value);
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (status == "booked")
                invoices = invoices.Where(i => i.Status == InvoiceStatus.Completed && !i.Cancel);
            else if (status == "canceled")
                invoices = invoices.Where(i => i.Status == InvoiceStatus.Completed && i.Cancel);
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
                cancel = i.Cancel,
                cancelDate = i.CancelDate,
                cancelBy = i.CancelBy,
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
        return result;
    }

    public async Task<(bool Success, List<string> Messages)> CancelTicketByAdminAsync(string ticketId)
    {
        var booking = _invoiceRepository.GetById(ticketId);
        if (booking == null)
            return (false, new List<string> { "Booking not found." });
        if (booking.Cancel)
            return (false, new List<string> { "This ticket has already been cancelled." });

        // Đánh dấu đã hủy, không đổi status
        booking.Cancel = true;
        booking.CancelDate = DateTime.Now;
        booking.CancelBy = "Admin";

        // Update schedule seats: mark as available again
        var scheduleSeatsToUpdate = _scheduleSeatRepository.GetByInvoiceId(booking.InvoiceId).ToList();
        foreach (var seat in scheduleSeatsToUpdate)
        {
            seat.SeatStatusId = 1; // Available
            _scheduleSeatRepository.Update(seat);
            // Phát sự kiện SignalR cho từng ghế trả lại
            if (seat.MovieShowId.HasValue && seat.SeatId.HasValue)
            {
                await _seatHubContext.Clients.Group(seat.MovieShowId.Value.ToString()).SendAsync("SeatStatusChanged", seat.SeatId.Value, 1);
            }
        }
        _scheduleSeatRepository.Save();

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

        _invoiceRepository.Update(booking);
        _invoiceRepository.Save();
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
                Code = $"REFUND-{booking.InvoiceId}", // Unique code per refund
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
        var rankUpMsg = _accountService.GetAndClearRankUpgradeNotification(booking.AccountId);
        if (!string.IsNullOrEmpty(rankUpMsg))
        {
            messages.Add(rankUpMsg);
        }
        return (true, messages);
    }

    public List<SeatDetailViewModel> BuildSeatDetails(Invoice booking)
    {
        var seatDetails = new List<SeatDetailViewModel>();
        int promotionDiscount = 0;
        if (!string.IsNullOrEmpty(booking.PromotionDiscount) && booking.PromotionDiscount != "0")
        {
            try
            {
                var promoObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(booking.PromotionDiscount);
                promotionDiscount = (int)(promoObj.seat ?? 0);
            }
            catch { promotionDiscount = 0; }
        }
        var versionMulti = booking.MovieShow?.Version?.Multi ?? 1;
        if (booking.ScheduleSeats != null && booking.ScheduleSeats.Any(ss => ss.Seat != null))
        {
            seatDetails = booking.ScheduleSeats
                .Where(ss => ss.Seat != null)
                .Select(ss =>
                {
                    var seatType = ss.Seat.SeatType;
                    decimal originalPrice = (seatType?.PricePercent ?? 0) * versionMulti;
                    decimal priceAfterPromotion = originalPrice;
                    if (promotionDiscount > 0)
                    {
                        priceAfterPromotion = originalPrice * (1 - promotionDiscount / 100m);
                    }
                    return new SeatDetailViewModel
                    {
                        SeatId = ss.Seat.SeatId,
                        SeatName = ss.Seat.SeatName,
                        SeatType = seatType?.TypeName ?? "Unknown",
                        Price = priceAfterPromotion,
                        OriginalPrice = originalPrice,
                        PromotionDiscount = promotionDiscount,
                        PriceAfterPromotion = priceAfterPromotion
                    };
                }).ToList();
        }
        else if (!string.IsNullOrEmpty(booking.SeatIds))
        {
            var seatIdArr = booking.SeatIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id.Trim()))
                .ToList();
            var allSeats = seatIdArr.Select(id => _seatRepository.GetById(id)).Where(s => s != null).ToList();
            seatDetails = allSeats.Select(seat =>
            {
                var seatType = seat.SeatType;
                decimal originalPrice = (seatType?.PricePercent ?? 0) * versionMulti;
                decimal priceAfterPromotion = originalPrice;
                if (promotionDiscount > 0)
                {
                    priceAfterPromotion = originalPrice * (1 - promotionDiscount / 100m);
                }
                return new SeatDetailViewModel
                {
                    SeatId = seat.SeatId,
                    SeatName = seat.SeatName,
                    SeatType = seatType?.TypeName ?? "Unknown",
                    Price = priceAfterPromotion,
                    OriginalPrice = originalPrice,
                    PromotionDiscount = promotionDiscount,
                    PriceAfterPromotion = priceAfterPromotion
                };
            }).ToList();
        }
        return seatDetails;
    }
}