using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IScheduleSeatService
    {
        Task<bool> CreateScheduleSeatAsync(ScheduleSeat scheduleSeat);
        Task<bool> CreateMultipleScheduleSeatsAsync(IEnumerable<ScheduleSeat> scheduleSeats);
        Task<ScheduleSeat> GetScheduleSeatAsync(int movieShowId, int seatId);
        Task<IEnumerable<ScheduleSeat>> GetScheduleSeatsByMovieShowAsync(int movieShowId);
        Task<bool> UpdateSeatStatusAsync(int movieShowId, int seatId, int statusId);
        IEnumerable<ScheduleSeat> GetByInvoiceId(string invoiceId);
        void Update(ScheduleSeat seat);
        void Save();
        Task UpdateScheduleSeatsStatusAsync(string invoiceId, int statusId);
        Task UpdateScheduleSeatsBookedPriceAsync(string invoiceId, decimal bookedPrice);
        Task UpdateScheduleSeatsToBookedAsync(string invoiceId, int movieShowId, List<int> seatIds);
    }
}