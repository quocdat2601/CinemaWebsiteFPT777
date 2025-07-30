using MovieTheater.Models;
using MovieTheater.ViewModels;
using MovieTheater.Repository;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Service
{
    public interface IDashboardService
    {
        AdminDashboardViewModel GetDashboardViewModel(int days = 7);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ISeatService _seatService;
        private readonly IMemberRepository _memberRepository;
        private readonly MovieTheaterContext _context;

        public DashboardService(IInvoiceService invoiceService, ISeatService seatService, IMemberRepository memberRepository, MovieTheaterContext context)
        {
            _invoiceService = invoiceService;
            _seatService = seatService;
            _memberRepository = memberRepository;
            _context = context;
        }

        public AdminDashboardViewModel GetDashboardViewModel(int days = 7)
        {
            var today = DateTime.Today;
            
            // 1) Split invoices into three buckets as per specification
            var allCompleted = _invoiceService.GetAll()
                .Where(i => i.Status == InvoiceStatus.Completed)
                .ToList();

            // ① Valid — not cancelled at all
            var validInvoices = allCompleted.Where(i => !i.Cancel).ToList();

            // ② Cancelled — customer got a voucher for seats
            var cancelledInvoices = allCompleted.Where(i => i.Cancel).ToList();

            // 2) Movie (seat) metrics calculation
            var grossRevenue = validInvoices.Sum(i => i.TotalMoney ?? 0m) + cancelledInvoices.Sum(i => i.TotalMoney ?? 0m);
            var totalVouchersIssued = cancelledInvoices.Sum(i => i.TotalMoney ?? 0m);
            var netRevenue = grossRevenue - totalVouchersIssued;
            var totalBookings = validInvoices.Count();

            // Today's metrics
            var todayValidInvoices = validInvoices.Where(i => i.BookingDate?.Date == today).ToList();
            var todayCancelledInvoices = cancelledInvoices.Where(i => i.BookingDate?.Date == today).ToList();
            
            var revenueToday = todayValidInvoices.Sum(i => i.TotalMoney ?? 0m);
            var vouchersToday = todayCancelledInvoices.Sum(i => i.TotalMoney ?? 0m);
            var netRevenueToday = revenueToday - vouchersToday;
            var bookingsToday = todayValidInvoices.Count();
            var ticketsSoldToday = todayValidInvoices.Sum(i => i.Seat?.Split(',').Length ?? 0);

            // 3) Occupancy calculation
            var allSeats = _seatService.GetAllSeatsAsync().Result;
            var totalSeats = allSeats.Count;
            var occupancyRate = totalSeats > 0
                ? Math.Round((decimal)ticketsSoldToday / totalSeats * 100, 1)
                : 0m;

            // 4) N-day trends with proper bucket separation
            var lastNDays = Enumerable.Range(0, days)
                .Select(i => today.AddDays(-i))
                .Reverse()
                .ToList();

            // Revenue trends: Gross, Vouchers, Net
            var grossRevenueTrend = lastNDays
                .Select(d => validInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv => inv.TotalMoney ?? 0m) +
                            cancelledInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();

            var voucherTrend = lastNDays
                .Select(d => cancelledInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();

            var netRevenueTrend = lastNDays
                .Select(d => validInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv => inv.TotalMoney ?? 0m))
                .ToList();

            var bookingTrend = lastNDays
                .Select(d => validInvoices.Where(inv => inv.BookingDate?.Date == d).Count())
                .ToList();

            // 5) Top 5 movies & members (only from valid invoices)
            var topMovies = validInvoices
                .GroupBy(i => i.MovieShow.Movie.MovieNameEnglish)
                .OrderByDescending(g => g.Sum(inv => inv.Seat?.Split(',').Length ?? 0))
                .Take(5)
                .Select(g => (MovieName: g.Key, TicketsSold: g.Sum(inv => inv.Seat?.Split(',').Length ?? 0)))
                .ToList();

            var topMembers = validInvoices
                .Where(i => i.Account != null && i.Account.RoleId == 3)
                .GroupBy(i => i.Account.FullName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => (MemberName: g.Key, Bookings: g.Count()))
                .ToList();

            // 6) Recent bookings (only valid)
            var recentBookings = validInvoices
                .OrderByDescending(i => i.BookingDate)
                .Take(10)
                .Select(i => new RecentBookingInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
                    BookingDate = i.BookingDate ?? DateTime.MinValue,
                    Status = "Completed"
                })
                .ToList();

            var recentMovieBookings = validInvoices
                .OrderByDescending(i => i.BookingDate)
                .Take(10)
                .Select(i => new RecentMovieActivityInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
                    ActivityDate = i.BookingDate ?? DateTime.MinValue,
                    TotalAmount = i.TotalMoney ?? 0m
                })
                .ToList();

            var recentMovieCancellations = cancelledInvoices
                .OrderByDescending(i => i.CancelDate)
                .Take(10)
                .Select(i => new RecentMovieActivityInfo
                {
                    InvoiceId = i.InvoiceId,
                    MemberName = i.Account?.FullName ?? "N/A",
                    MovieName = i.MovieShow.Movie.MovieNameEnglish,
                    ActivityDate = i.CancelDate ?? DateTime.MinValue,
                    TotalAmount = i.TotalMoney ?? 0m
                })
                .ToList();

            // 7) Recent members
            var recentMembers = _memberRepository.GetAll()
                .Where(m => m.Account?.RegisterDate != null)
                .OrderByDescending(m => m.Account!.RegisterDate)
                .Take(5)
                .Select(m => new RecentMemberInfo
                {
                    MemberId = m.MemberId,
                    FullName = m.Account!.FullName ?? "N/A",
                    Email = m.Account.Email ?? "N/A",
                    PhoneNumber = m.Account.PhoneNumber ?? "N/A",
                    JoinDate = m.Account.RegisterDate
                })
                .ToList();

            // --- Food Analytics (unchanged - already correctly implemented) ---
            var foodInvoices = _context.FoodInvoices
                .Include(fi => fi.Food)
                .Include(fi => fi.Invoice)
                .ToList();

            // Only completed, not cancelled invoices for food stats
            var validFoodInvoices = foodInvoices.Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && !fi.Invoice.Cancel).ToList();

            // Recent food orders (from completed, not cancelled invoices)
            var recentFoodOrders = validFoodInvoices
                .OrderByDescending(fi => fi.Invoice.BookingDate)
                .Take(10)
                .Select(fi => new RecentFoodOrder
                {
                    Date = fi.Invoice.BookingDate ?? DateTime.MinValue,
                    FoodName = fi.Food.Name,
                    Quantity = fi.Quantity,
                    Price = fi.Price,
                    OrderTotal = fi.Price * fi.Quantity
                })
                .ToList();

            // Recent food cancels (from cancelled invoices)
            var recentFoodCancels = foodInvoices
                .Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && fi.Invoice.Cancel)
                .OrderByDescending(fi => fi.Invoice.CancelDate)
                .Take(10)
                .Select(fi => new RecentFoodOrder
                {
                    Date = fi.Invoice.CancelDate ?? fi.Invoice.BookingDate ?? DateTime.MinValue,
                    FoodName = fi.Food.Name,
                    Quantity = fi.Quantity,
                    Price = fi.Price,
                    OrderTotal = fi.Price * fi.Quantity
                })
                .ToList();

            // Food metrics calculation
            var foodRevenue = validFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var foodOrders = validFoodInvoices.Select(fi => fi.InvoiceId).Distinct().Count();
            var quantitySold = validFoodInvoices.Sum(fi => fi.Quantity);
            var avgOrderValue = foodOrders > 0 ? Math.Round(foodRevenue / foodOrders, 0) : 0;

            // Food trends
            var revenueByDayValues = lastNDays
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Sum(fi => fi.Price * fi.Quantity))
                .ToList();
            var ordersByDayValues = lastNDays
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Select(fi => fi.InvoiceId).Distinct().Count())
                .ToList();

            // Top food items
            var topFoodItems = validFoodInvoices
                .GroupBy(fi => fi.Food.Name)
                .Select(g => new FoodItemQuantity {
                    FoodName = g.Key,
                    Quantity = g.Sum(fi => fi.Quantity),
                    Category = g.First().Food.Category,
                    Revenue = g.Sum(fi => fi.Price * fi.Quantity),
                    OrderCount = g.Select(fi => fi.InvoiceId).Distinct().Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Sales by category
            var salesByCategory = validFoodInvoices
                .GroupBy(fi => fi.Food.Category)
                .Select(g => (Category: g.Key, Revenue: g.Sum(fi => fi.Price * fi.Quantity)))
                .ToList();

            // Sales by hour
            var salesByHour = Enumerable.Range(0, 24)
                .Select(hour => validFoodInvoices.Count(fi => fi.Invoice.BookingDate.HasValue && fi.Invoice.BookingDate.Value.Hour == hour))
                .ToList();

            // Build and return the view model
            return new AdminDashboardViewModel
            {
                RevenueToday = revenueToday,
                TotalBookings = totalBookings,
                BookingsToday = bookingsToday,
                TicketsSoldToday = ticketsSoldToday,
                OccupancyRateToday = occupancyRate,
                RevenueTrendDates = lastNDays,
                RevenueTrendValues = grossRevenueTrend, // Changed to gross revenue trend
                BookingTrendDates = lastNDays,
                BookingTrendValues = bookingTrend,
                VoucherTrendValues = voucherTrend,
                TopMovies = topMovies,
                TopMembers = topMembers,
                MovieAnalytics = new MovieAnalyticsViewModel
                {
                    RecentBookings = recentMovieBookings,
                    RecentCancellations = recentMovieCancellations
                },
                RecentMembers = recentMembers,
                GrossRevenue = grossRevenue,
                NetRevenue = netRevenue,
                NetRevenueToday = netRevenueToday,
                TotalVouchersIssued = totalVouchersIssued,
                VouchersToday = vouchersToday,
                FoodAnalytics = new FoodAnalyticsViewModel
                {
                    GrossRevenue = foodRevenue,
                    GrossRevenueToday = validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).Sum(fi => fi.Price * fi.Quantity),
                    TotalOrders = foodOrders,
                    OrdersToday = validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).Select(fi => fi.InvoiceId).Distinct().Count(),
                    QuantitySoldToday = validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).Sum(fi => fi.Quantity),
                    AvgOrderValueToday = validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).Select(fi => fi.InvoiceId).Distinct().Count() > 0 ? Math.Round(validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).Sum(fi => fi.Price * fi.Quantity) / validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).Select(fi => fi.InvoiceId).Distinct().Count(), 0) : 0,
                    RevenueByDayDates = lastNDays,
                    RevenueByDayValues = revenueByDayValues,
                    OrdersByDayValues = ordersByDayValues,
                    TopFoodItems = topFoodItems,
                    SalesByCategory = salesByCategory,
                    SalesByHour = salesByHour,
                    RecentOrders = recentFoodOrders,
                    RecentCancels = recentFoodCancels
                }
            };
        }
    }
} 