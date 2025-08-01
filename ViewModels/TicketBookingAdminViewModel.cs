using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class TicketBookingAdminViewModel
    {
        public IEnumerable<Movie> Movies { get; set; } = new List<Movie>();
        public string ReturnUrl { get; set; } = string.Empty;
    }
} 