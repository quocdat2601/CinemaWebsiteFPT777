using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class ScheduleSeatService : IScheduleSeatService
    {
        private readonly IScheduleSeatRepository _scheduleSeatRepository;

        public ScheduleSeatService(IScheduleSeatRepository scheduleSeatRepository)
        {
            _scheduleSeatRepository = scheduleSeatRepository;
        }

        public async Task<bool> CreateScheduleSeatAsync(ScheduleSeat scheduleSeat)
        {
            return await _scheduleSeatRepository.CreateScheduleSeatAsync(scheduleSeat);
        }

        public async Task<bool> CreateMultipleScheduleSeatsAsync(IEnumerable<ScheduleSeat> scheduleSeats)
        {
            return await _scheduleSeatRepository.CreateMultipleScheduleSeatsAsync(scheduleSeats);
        }

        public async Task<ScheduleSeat> GetScheduleSeatAsync(int movieShowId, int seatId)
        {
            return await _scheduleSeatRepository.GetScheduleSeatAsync(movieShowId, seatId);
        }

        public async Task<IEnumerable<ScheduleSeat>> GetScheduleSeatsByMovieShowAsync(int movieShowId)
        {
            return await _scheduleSeatRepository.GetScheduleSeatsByMovieShowAsync(movieShowId);
        }

        public async Task<bool> UpdateSeatStatusAsync(int movieShowId, int seatId, int statusId)
        {
            return await _scheduleSeatRepository.UpdateSeatStatusAsync(movieShowId, seatId, statusId);
        }

        public IEnumerable<ScheduleSeat> GetByInvoiceId(string invoiceId)
        {
            return _scheduleSeatRepository.GetByInvoiceId(invoiceId);
        }

        public void Update(ScheduleSeat seat)
        {
            _scheduleSeatRepository.Update(seat);
        }

        public void Save()
        {
            _scheduleSeatRepository.Save();
        }

        public async Task UpdateScheduleSeatsStatusAsync(string invoiceId, int statusId)
        {
            var scheduleSeats = GetByInvoiceId(invoiceId);
            foreach (var scheduleSeat in scheduleSeats)
            {
                scheduleSeat.SeatStatusId = statusId;
                Update(scheduleSeat);
            }
            Save();
        }

        public async Task UpdateScheduleSeatsBookedPriceAsync(string invoiceId, decimal bookedPrice)
        {
            var scheduleSeats = GetByInvoiceId(invoiceId);
            foreach (var scheduleSeat in scheduleSeats)
            {
                scheduleSeat.BookedPrice = bookedPrice;
                Update(scheduleSeat);
            }
            Save();
        }

        public async Task UpdateScheduleSeatsToBookedAsync(string invoiceId, int movieShowId, List<int> seatIds)
        {
            await _scheduleSeatRepository.UpdateScheduleSeatsToBookedAsync(invoiceId, movieShowId, seatIds);
        }
    }
}