using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

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

            // Get base data
            var (validInvoices, cancelledInvoices, foodInvoiceData) = GetBaseInvoiceData();

            // Calculate movie metrics
            var movieMetrics = CalculateMovieMetrics(validInvoices, cancelledInvoices, foodInvoiceData, today);

            // Calculate food metrics
            var foodMetrics = CalculateFoodMetrics(today);

            // Get trend data
            var trendData = CalculateTrendData(validInvoices, cancelledInvoices, foodInvoiceData, today, days);

            // Get top performers
            var topPerformers = GetTopPerformers(validInvoices, today);

            // Get recent activities
            var recentActivities = GetRecentActivities(validInvoices, cancelledInvoices, today);

            // Get recent members
            var recentMembers = GetRecentMembers(today);

            // Build and return the view model
            return new AdminDashboardViewModel
            {
                RevenueToday = movieMetrics.RevenueToday,
                TotalBookings = movieMetrics.TotalBookings,
                BookingsToday = movieMetrics.BookingsToday,
                TicketsSoldToday = movieMetrics.TicketsSoldToday,
                OccupancyRateToday = movieMetrics.OccupancyRateToday,
                RevenueTrendDates = trendData.RevenueTrendDates,
                RevenueTrendValues = trendData.GrossRevenueTrend,
                BookingTrendDates = trendData.RevenueTrendDates,
                BookingTrendValues = trendData.BookingTrend,
                VoucherTrendValues = trendData.VoucherTrend,
                TopMovies = topPerformers.TopMovies,
                TopMembers = topPerformers.TopMembers,
                MovieAnalytics = new MovieAnalyticsViewModel
                {
                    RecentBookings = recentActivities.RecentMovieBookings,
                    RecentCancellations = recentActivities.RecentMovieCancellations
                },
                RecentMembers = recentMembers,
                GrossRevenue = movieMetrics.GrossRevenue,
                NetRevenue = movieMetrics.NetRevenue,
                NetRevenueToday = movieMetrics.NetRevenueToday,
                TotalVouchersIssued = movieMetrics.TotalVouchersIssued,
                VouchersToday = movieMetrics.VouchersToday,
                FoodAnalytics = new FoodAnalyticsViewModel
                {
                    GrossRevenue = foodMetrics.GrossRevenue,
                    VouchersIssued = foodMetrics.VouchersIssued,
                    NetRevenue = foodMetrics.NetRevenue,
                    GrossRevenueToday = foodMetrics.GrossRevenueToday,
                    VouchersToday = foodMetrics.VouchersToday,
                    NetRevenueToday = foodMetrics.NetRevenueToday,
                    TotalOrders = foodMetrics.TotalOrders,
                    OrdersToday = foodMetrics.OrdersToday,
                    QuantitySoldToday = foodMetrics.QuantitySoldToday,
                    AvgOrderValueToday = foodMetrics.AvgOrderValueToday,
                    RevenueByDayDates = trendData.RevenueTrendDates,
                    RevenueByDayValues = trendData.FoodRevenueByDayValues,
                    VoucherTrendValues = trendData.FoodVoucherTrendValues,
                    NetRevenueByDayValues = trendData.FoodNetRevenueByDayValues,
                    OrdersByDayValues = trendData.OrdersByDayValues,
                    TopFoodItems = foodMetrics.TopFoodItems,
                    SalesByCategory = foodMetrics.SalesByCategory,
                    SalesByHour = foodMetrics.SalesByHour,
                    RecentOrders = foodMetrics.RecentOrders,
                    RecentCancels = foodMetrics.RecentCancels
                }
            };
        }

        private (List<Invoice> validInvoices, List<Invoice> cancelledInvoices, Dictionary<string, decimal> foodInvoiceData) GetBaseInvoiceData()
        {
            var allCompleted = _invoiceService.GetAll()
                .Where(i => i.Status == InvoiceStatus.Completed)
                .ToList();

            var validInvoices = allCompleted.Where(i => !i.Cancel).ToList();
            var cancelledInvoices = allCompleted.Where(i => i.Cancel).ToList();

            var allInvoiceIds = allCompleted.Select(i => i.InvoiceId).ToList();
            var foodInvoices = _context.FoodInvoices
                .Where(fi => allInvoiceIds.Contains(fi.InvoiceId))
                .ToList();
            var foodInvoiceData = foodInvoices
                .GroupBy(fi => fi.InvoiceId)
                .ToDictionary(g => g.Key, g => g.Sum(fi => fi.Price * fi.Quantity));

            return (validInvoices, cancelledInvoices, foodInvoiceData);
        }

        private MovieMetrics CalculateMovieMetrics(List<Invoice> validInvoices, List<Invoice> cancelledInvoices,
            Dictionary<string, decimal> foodInvoiceData, DateTime today)
        {
            // 7-day metrics
            var sevenDayValidInvoices = validInvoices.Where(i => i.BookingDate >= today.AddDays(-6)).ToList();
            var sevenDayCancelledInvoices = cancelledInvoices.Where(i => i.BookingDate >= today.AddDays(-6)).ToList();

            var grossRevenue = sevenDayValidInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            }) + sevenDayCancelledInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            });

            var totalVouchersIssued = sevenDayCancelledInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            });

            var netRevenue = sevenDayValidInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            });
            var totalBookings = sevenDayValidInvoices.Count();

            // Today's metrics
            var todayValidInvoices = validInvoices.Where(i => i.BookingDate?.Date == today).ToList();
            var todayCancelledInvoices = cancelledInvoices.Where(i => i.BookingDate?.Date == today).ToList();

            var revenueToday = todayValidInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            }) + todayCancelledInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            });

            var vouchersToday = todayCancelledInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            });

            var netRevenueToday = todayValidInvoices.Sum(i =>
            {
                var totalMoney = i.TotalMoney ?? 0m;
                var foodTotal = foodInvoiceData.ContainsKey(i.InvoiceId) ? foodInvoiceData[i.InvoiceId] : 0m;
                return totalMoney - foodTotal;
            });
            var bookingsToday = todayValidInvoices.Count();
            var ticketsSoldToday = todayValidInvoices.Sum(i => i.Seat?.Split(',').Length ?? 0);

            // Occupancy calculation
            var allSeats = _seatService.GetAllSeatsAsync().Result;
            var totalSeats = allSeats.Count;
            var occupancyRate = totalSeats > 0
                ? Math.Round((decimal)ticketsSoldToday / totalSeats * 100, 1)
                : 0m;

            return new MovieMetrics
            {
                GrossRevenue = grossRevenue,
                NetRevenue = netRevenue,
                TotalVouchersIssued = totalVouchersIssued,
                TotalBookings = totalBookings,
                RevenueToday = revenueToday,
                NetRevenueToday = netRevenueToday,
                VouchersToday = vouchersToday,
                BookingsToday = bookingsToday,
                TicketsSoldToday = ticketsSoldToday,
                OccupancyRateToday = occupancyRate
            };
        }

        private FoodMetrics CalculateFoodMetrics(DateTime today)
        {
            var foodInvoices = _context.FoodInvoices
                .Include(fi => fi.Food)
                .Include(fi => fi.Invoice)
                .ToList();

            var validFoodInvoices = foodInvoices.Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && !fi.Invoice.Cancel).ToList();
            var cancelledFoodInvoices = foodInvoices.Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && fi.Invoice.Cancel).ToList();

            // 7-day metrics
            var sevenDayValidFoodInvoices = validFoodInvoices.Where(fi => fi.Invoice.BookingDate >= today.AddDays(-6)).ToList();
            var sevenDayCancelledFoodInvoices = cancelledFoodInvoices.Where(fi => fi.Invoice.BookingDate >= today.AddDays(-6)).ToList();

            var grossRevenue = sevenDayValidFoodInvoices.Sum(fi => fi.Price * fi.Quantity) + sevenDayCancelledFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var vouchersIssued = sevenDayCancelledFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var netRevenue = sevenDayValidFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var totalOrders = sevenDayValidFoodInvoices.Select(fi => fi.InvoiceId).Distinct().Count();

            // Today's metrics
            var todayValidFoodInvoices = validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).ToList();
            var todayCancelledFoodInvoices = cancelledFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == today).ToList();

            var grossRevenueToday = todayValidFoodInvoices.Sum(fi => fi.Price * fi.Quantity) + todayCancelledFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var vouchersToday = todayCancelledFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var netRevenueToday = todayValidFoodInvoices.Sum(fi => fi.Price * fi.Quantity);
            var ordersToday = todayValidFoodInvoices.Select(fi => fi.InvoiceId).Distinct().Count();
            var quantitySoldToday = todayValidFoodInvoices.Sum(fi => fi.Quantity);
            var avgOrderValueToday = ordersToday > 0 ? Math.Round(todayValidFoodInvoices.Sum(fi => fi.Price * fi.Quantity) / ordersToday, 0) : 0;

            // Top food items (last 7 days)
            var topFoodItems = sevenDayValidFoodInvoices
                .GroupBy(fi => fi.Food.Name)
                .Select(g => new FoodItemQuantity
                {
                    FoodName = g.Key,
                    Quantity = g.Sum(fi => fi.Quantity),
                    Category = g.First().Food.Category,
                    Revenue = g.Sum(fi => fi.Price * fi.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Sales by category (last 7 days)
            var salesByCategory = sevenDayValidFoodInvoices
                .GroupBy(fi => fi.Food.Category)
                .Select(g => (Category: g.Key, Revenue: g.Sum(fi => fi.Price * fi.Quantity)))
                .ToList();

            // Sales by hour (last 7 days) - Full 24-hour array for JavaScript slicing
            var salesByHour = Enumerable.Range(0, 24) // 0 to 23 (full 24 hours)
                .Select(hour => sevenDayValidFoodInvoices.Count(fi => fi.Invoice.BookingDate.HasValue && fi.Invoice.BookingDate.Value.Hour == hour))
                .ToList();

            // Recent activities
            var recentOrders = sevenDayValidFoodInvoices
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

            var recentCancels = sevenDayCancelledFoodInvoices
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

            return new FoodMetrics
            {
                GrossRevenue = grossRevenue,
                VouchersIssued = vouchersIssued,
                NetRevenue = netRevenue,
                TotalOrders = totalOrders,
                GrossRevenueToday = grossRevenueToday,
                VouchersToday = vouchersToday,
                NetRevenueToday = netRevenueToday,
                OrdersToday = ordersToday,
                QuantitySoldToday = quantitySoldToday,
                AvgOrderValueToday = avgOrderValueToday,
                TopFoodItems = topFoodItems,
                SalesByCategory = salesByCategory,
                SalesByHour = salesByHour,
                RecentOrders = recentOrders,
                RecentCancels = recentCancels
            };
        }

        private TrendData CalculateTrendData(List<Invoice> validInvoices, List<Invoice> cancelledInvoices,
            Dictionary<string, decimal> foodInvoiceData, DateTime today, int days)
        {
            var lastNDays = Enumerable.Range(0, days)
                .Select(i => today.AddDays(-i))
                .Reverse()
                .ToList();

            // Movie trends
            var grossRevenueTrend = lastNDays
                .Select(d => validInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv =>
                {
                    var totalMoney = inv.TotalMoney ?? 0m;
                    var foodTotal = foodInvoiceData.ContainsKey(inv.InvoiceId) ? foodInvoiceData[inv.InvoiceId] : 0m;
                    return totalMoney - foodTotal;
                }) + cancelledInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv =>
                {
                    var totalMoney = inv.TotalMoney ?? 0m;
                    var foodTotal = foodInvoiceData.ContainsKey(inv.InvoiceId) ? foodInvoiceData[inv.InvoiceId] : 0m;
                    return totalMoney - foodTotal;
                }))
                .ToList();

            var voucherTrend = lastNDays
                .Select(d => cancelledInvoices.Where(inv => inv.BookingDate?.Date == d).Sum(inv =>
                {
                    var totalMoney = inv.TotalMoney ?? 0m;
                    var foodTotal = foodInvoiceData.ContainsKey(inv.InvoiceId) ? foodInvoiceData[inv.InvoiceId] : 0m;
                    return totalMoney - foodTotal;
                }))
                .ToList();

            var bookingTrend = lastNDays
                .Select(d => validInvoices.Where(inv => inv.BookingDate?.Date == d).Count())
                .ToList();

            // Food trends
            var foodInvoices = _context.FoodInvoices
                .Include(fi => fi.Invoice)
                .ToList();

            var validFoodInvoices = foodInvoices.Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && !fi.Invoice.Cancel).ToList();
            var cancelledFoodInvoices = foodInvoices.Where(fi => fi.Invoice.Status == InvoiceStatus.Completed && fi.Invoice.Cancel).ToList();

            var foodRevenueByDayValues = lastNDays
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Sum(fi => fi.Price * fi.Quantity) +
                              cancelledFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Sum(fi => fi.Price * fi.Quantity))
                .ToList();

            var foodVoucherTrendValues = lastNDays
                .Select(day => cancelledFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Sum(fi => fi.Price * fi.Quantity))
                .ToList();

            var foodNetRevenueByDayValues = lastNDays
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Sum(fi => fi.Price * fi.Quantity))
                .ToList();

            var ordersByDayValues = lastNDays
                .Select(day => validFoodInvoices.Where(fi => fi.Invoice.BookingDate?.Date == day).Select(fi => fi.InvoiceId).Distinct().Count())
                .ToList();

            return new TrendData
            {
                RevenueTrendDates = lastNDays,
                GrossRevenueTrend = grossRevenueTrend,
                VoucherTrend = voucherTrend,
                BookingTrend = bookingTrend,
                FoodRevenueByDayValues = foodRevenueByDayValues,
                FoodVoucherTrendValues = foodVoucherTrendValues,
                FoodNetRevenueByDayValues = foodNetRevenueByDayValues,
                OrdersByDayValues = ordersByDayValues
            };
        }

        private TopPerformers GetTopPerformers(List<Invoice> validInvoices, DateTime today)
        {
            var sevenDayValidInvoices = validInvoices.Where(i => i.BookingDate >= today.AddDays(-6)).ToList();

            var topMovies = sevenDayValidInvoices
                .GroupBy(i => i.MovieShow.Movie.MovieNameEnglish)
                .OrderByDescending(g => g.Sum(inv => inv.Seat?.Split(',').Length ?? 0))
                .Take(5)
                .Select(g => (MovieName: g.Key, TicketsSold: g.Sum(inv => inv.Seat?.Split(',').Length ?? 0)))
                .ToList();

            var topMembers = sevenDayValidInvoices
                .Where(i => i.Account != null && i.Account.RoleId == 3)
                .GroupBy(i => i.Account.FullName)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => (MemberName: g.Key, Bookings: g.Count()))
                .ToList();

            return new TopPerformers
            {
                TopMovies = topMovies,
                TopMembers = topMembers
            };
        }

        private RecentActivities GetRecentActivities(List<Invoice> validInvoices, List<Invoice> cancelledInvoices, DateTime today)
        {
            var sevenDayValidInvoices = validInvoices.Where(i => i.BookingDate >= today.AddDays(-6)).ToList();
            var sevenDayCancelledInvoices = cancelledInvoices.Where(i => i.CancelDate >= today.AddDays(-6)).ToList();

            var recentMovieBookings = sevenDayValidInvoices
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

            var recentMovieCancellations = sevenDayCancelledInvoices
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

            return new RecentActivities
            {
                RecentMovieBookings = recentMovieBookings,
                RecentMovieCancellations = recentMovieCancellations
            };
        }

        private List<RecentMemberInfo> GetRecentMembers(DateTime today)
        {
            return _memberRepository.GetAll()
                .Where(m => m.Account?.RegisterDate != null && m.Account.RegisterDate >= DateOnly.FromDateTime(today.AddDays(-6)))
                .OrderByDescending(m => m.Account!.RegisterDate)
                .Take(5)
                .Select(m => new RecentMemberInfo
                {
                    FullName = m.Account!.FullName ?? "N/A",
                    Email = m.Account.Email ?? "N/A",
                    JoinDate = m.Account.RegisterDate
                })
                .ToList();
        }

        // Helper classes for better organization
        private class MovieMetrics
        {
            public decimal GrossRevenue { get; set; }
            public decimal NetRevenue { get; set; }
            public decimal TotalVouchersIssued { get; set; }
            public int TotalBookings { get; set; }
            public decimal RevenueToday { get; set; }
            public decimal NetRevenueToday { get; set; }
            public decimal VouchersToday { get; set; }
            public int BookingsToday { get; set; }
            public int TicketsSoldToday { get; set; }
            public decimal OccupancyRateToday { get; set; }
        }

        private class FoodMetrics
        {
            public decimal GrossRevenue { get; set; }
            public decimal VouchersIssued { get; set; }
            public decimal NetRevenue { get; set; }
            public int TotalOrders { get; set; }
            public decimal GrossRevenueToday { get; set; }
            public decimal VouchersToday { get; set; }
            public decimal NetRevenueToday { get; set; }
            public int OrdersToday { get; set; }
            public int QuantitySoldToday { get; set; }
            public decimal AvgOrderValueToday { get; set; }
            public List<FoodItemQuantity> TopFoodItems { get; set; } = new();
            public List<(string Category, decimal Revenue)> SalesByCategory { get; set; } = new();
            public List<int> SalesByHour { get; set; } = new();
            public List<RecentFoodOrder> RecentOrders { get; set; } = new();
            public List<RecentFoodOrder> RecentCancels { get; set; } = new();
        }

        private class TrendData
        {
            public List<DateTime> RevenueTrendDates { get; set; } = new();
            public List<decimal> GrossRevenueTrend { get; set; } = new();
            public List<decimal> VoucherTrend { get; set; } = new();
            public List<int> BookingTrend { get; set; } = new();
            public List<decimal> FoodRevenueByDayValues { get; set; } = new();
            public List<decimal> FoodVoucherTrendValues { get; set; } = new();
            public List<decimal> FoodNetRevenueByDayValues { get; set; } = new();
            public List<int> OrdersByDayValues { get; set; } = new();
        }

        private class TopPerformers
        {
            public List<(string MovieName, int TicketsSold)> TopMovies { get; set; } = new();
            public List<(string MemberName, int Bookings)> TopMembers { get; set; } = new();
        }

        private class RecentActivities
        {
            public List<RecentMovieActivityInfo> RecentMovieBookings { get; set; } = new();
            public List<RecentMovieActivityInfo> RecentMovieCancellations { get; set; } = new();
        }
    }
}