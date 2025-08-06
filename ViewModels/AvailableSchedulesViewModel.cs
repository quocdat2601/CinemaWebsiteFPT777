using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class AvailableSchedulesViewModel
    {
        public List<Schedule> Schedules { get; set; }
        public TimeSpan LastShowEndTime { get; set; }
        public bool HasExistingShows { get; set; }
    }
}