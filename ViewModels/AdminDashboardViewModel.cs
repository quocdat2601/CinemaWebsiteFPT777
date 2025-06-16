using System;
using System.Collections.Generic;

namespace MovieTheater.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Today's summary
        public decimal RevenueToday { get; set; }
        public int BookingsToday { get; set; }
        public int TicketsSoldToday { get; set; }

        // Trends over the last 7 days
        public List<DateTime> RevenueTrendDates { get; set; }
        public List<decimal> RevenueTrendValues { get; set; }
        public List<DateTime> BookingTrendDates { get; set; }
        public List<int> BookingTrendValues { get; set; }

        // Top 5 by tickets sold
        public List<(string MovieName, int TicketsSold)> TopMovies { get; set; }
        // Top 5 members by booking count
        public List<(string MemberName, int Bookings)> TopMembers { get; set; }

        // Recent bookings 
        public List<RecentBookingInfo> RecentBookings { get; set; }

        // Newest 5 members
        public List<RecentMemberInfo> RecentMembers { get; set; }

        public decimal OccupancyRateToday { get; set; }    // from 0 to 100

    }

    public class RecentBookingInfo
    {
        public string InvoiceId { get; set; }
        public string MemberName { get; set; }
        public string MovieName { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; }
    }

    public class RecentMemberInfo
    {
        public string MemberId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly? JoinDate { get; set; }   
    }
}
