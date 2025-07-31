namespace MovieTheater.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Today's summary
        public decimal RevenueToday { get; set; }
        public int TotalBookings { get; set; }
        public int BookingsToday { get; set; }
        public int TicketsSoldToday { get; set; }

        // Trends over the last 7 days
        public List<DateTime> RevenueTrendDates { get; set; }
        public List<decimal> RevenueTrendValues { get; set; }
        public List<DateTime> BookingTrendDates { get; set; }
        public List<int> BookingTrendValues { get; set; }
        public List<decimal> VoucherTrendValues { get; set; }

        // Top 5 by tickets sold
        public List<(string MovieName, int TicketsSold)> TopMovies { get; set; }
        // Top 5 members by booking count
        public List<(string MemberName, int Bookings)> TopMembers { get; set; }

        public MovieAnalyticsViewModel MovieAnalytics { get; set; }

        // Newest 5 members
        public List<RecentMemberInfo> RecentMembers { get; set; }

        public decimal OccupancyRateToday { get; set; }    // from 0 to 100
        
        // Movie Analytics - Three Bucket Pattern
        public decimal GrossRevenue { get; set; }           // Gross revenue (valid + cancelled)
        public decimal NetRevenue { get; set; }             // Net revenue (gross - vouchers)
        public decimal TotalVouchersIssued { get; set; }    // Total vouchers issued (all time)
        public decimal VouchersToday { get; set; }          // Today's vouchers issued
        public decimal NetRevenueToday { get; set; }        // Today's net revenue

        // Food analytics for dashboard food stat tab
        public FoodAnalyticsViewModel FoodAnalytics { get; set; }
    }



    public class RecentMovieActivityInfo
    {
        public string InvoiceId { get; set; }
        public string MemberName { get; set; }
        public string MovieName { get; set; }
        public DateTime ActivityDate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class MovieAnalyticsViewModel
    {
        public List<RecentMovieActivityInfo> RecentBookings { get; set; }
        public List<RecentMovieActivityInfo> RecentCancellations { get; set; }
    }

    public class RecentMemberInfo
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateOnly? JoinDate { get; set; }
    }

    public class FoodAnalyticsViewModel
    {
        // KPI cards with voucher support
        public decimal GrossRevenue { get; set; }
        public decimal VouchersIssued { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal GrossRevenueToday { get; set; }
        public decimal VouchersToday { get; set; }
        public decimal NetRevenueToday { get; set; }
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public int QuantitySoldToday { get; set; }
        public decimal AvgOrderValueToday { get; set; }


        


        // Combo chart: Revenue & Orders by Day
        public List<DateTime> RevenueByDayDates { get; set; }
        public List<decimal> RevenueByDayValues { get; set; }
        public List<decimal> VoucherTrendValues { get; set; }
        public List<decimal> NetRevenueByDayValues { get; set; }
        public List<int> OrdersByDayValues { get; set; }

        // Top 5 Food Items
        public List<FoodItemQuantity> TopFoodItems { get; set; }

        // Sales by Category
        public List<(string Category, decimal Revenue)> SalesByCategory { get; set; }

        // Sales by Hour (0-23)
        public List<int> SalesByHour { get; set; }

        // Recent Orders
        public List<RecentFoodOrder> RecentOrders { get; set; }

        // Recent Cancels
        public List<RecentFoodOrder> RecentCancels { get; set; }
    }

    public class FoodItemQuantity
    {
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public string Category { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentFoodOrder
    {
        public DateTime Date { get; set; }
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal OrderTotal { get; set; }
    }
}
