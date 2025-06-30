using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class ScheduleSeatRepository : IScheduleSeatRepository
    {
        private readonly MovieTheaterContext _context;

        public ScheduleSeatRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateScheduleSeatAsync(ScheduleSeat scheduleSeat)
        {
            try
            {
                await _context.ScheduleSeats.AddAsync(scheduleSeat);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateMultipleScheduleSeatsAsync(IEnumerable<ScheduleSeat> scheduleSeats)
        {
            try
            {
                await _context.ScheduleSeats.AddRangeAsync(scheduleSeats);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ScheduleSeat> GetScheduleSeatAsync(int movieShowId, int seatId)
        {
            return await _context.ScheduleSeats
                .FirstOrDefaultAsync(s => s.MovieShowId == movieShowId && s.SeatId == seatId);
        }

        public async Task<IEnumerable<ScheduleSeat>> GetScheduleSeatsByMovieShowAsync(int movieShowId)
        {
            var scheduleSeats = await _context.ScheduleSeats
                .Where(s => s.MovieShowId == movieShowId)
                .ToListAsync();

            // Lấy bản ghi ScheduleSeatId lớn nhất cho mỗi SeatId
            var latestSeats = scheduleSeats
                .GroupBy(s => s.SeatId)
                .Select(g => g.OrderByDescending(s => s.ScheduleSeatId).First())
                .ToList();

            return latestSeats;
        }

        public async Task<bool> UpdateSeatStatusAsync(int movieShowId, int seatId, int statusId)
        {
            try
            {
                var scheduleSeat = await GetScheduleSeatAsync(movieShowId, seatId);
                if (scheduleSeat == null) return false;

                scheduleSeat.SeatStatusId = statusId;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<ScheduleSeat> GetByInvoiceId(string invoiceId)
        {
            return _context.ScheduleSeats
                .Include(s => s.MovieShow)
                    .ThenInclude(ms => ms.CinemaRoom)
                .Include(s => s.Seat)
                .Where(s => s.InvoiceId == invoiceId)
                .ToList();
        }

        public void Update(ScheduleSeat seat)
        {
            _context.ScheduleSeats.Update(seat);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
