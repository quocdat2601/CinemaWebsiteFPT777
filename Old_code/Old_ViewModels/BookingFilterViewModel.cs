using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class BookingFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Status { get; set; } // 1 = Completed, 0 = Cancelled, null = All
        public List<Invoice> Bookings { get; set; } = new List<Invoice>();
    }

}
