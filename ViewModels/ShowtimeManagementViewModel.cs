using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class ShowtimeManagementViewModel
    {
        public DateTime SelectedDate { get; set; }
        public List<DateTime> AvailableDates { get; set; } = new List<DateTime>();
        public List<Schedule> AvailableSchedules { get; set; } = new List<Schedule>();
        public List<MovieShow> MovieShows { get; set; } = new List<MovieShow>();
    }
}