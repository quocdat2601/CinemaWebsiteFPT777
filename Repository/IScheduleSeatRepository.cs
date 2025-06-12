using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IScheduleSeatRepository
    {
        Task<bool> CreateScheduleSeatAsync(ScheduleSeat scheduleSeat);
        Task<bool> CreateMultipleScheduleSeatsAsync(IEnumerable<ScheduleSeat> scheduleSeats);
        Task<ScheduleSeat> GetScheduleSeatAsync(int movieShowId, int seatId);
        Task<IEnumerable<ScheduleSeat>> GetScheduleSeatsByMovieShowAsync(int movieShowId);
        Task<bool> UpdateSeatStatusAsync(int movieShowId, int seatId, int statusId);
    }
}
