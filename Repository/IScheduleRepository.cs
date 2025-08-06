using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Repository
{
    public interface IScheduleRepository
    {
        public List<Schedule> GetAllScheduleTimes();
        public AvailableSchedulesViewModel GetAvailableScheduleTimes(int cinemaRoomId, DateOnly showDate, int movieDurationMinutes, int cleaningTimeMinutes);
    }
}