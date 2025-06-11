using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class SeatRepository : ISeatRepository
    {
        private readonly MovieTheaterContext _context;
        public SeatRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public async Task<List<Seat>> GetAllAsync()
        {
            return await _context.Seats.ToListAsync();
        }

        public async Task<List<Seat>> GetByCinemaRoomIdAsync(int cinemaRoomId)
        {
            return await _context.Seats
                .Include(s => s.SeatType)
                .Where(s => s.CinemaRoomId == cinemaRoomId)
                .ToListAsync();
        }

        public Seat? GetById(int id)
        {
            return _context.Seats.FirstOrDefault(s => s.SeatId == id);
        }

        public void Add(Seat seat)
        {
            _context.Seats.Add(seat);
        }

        public void Update(Seat seat)
        {
            _context.Seats.Update(seat);
        }

        public async Task DeleteAsync(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat != null)
            {
                _context.Seats.Remove(seat);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task<List<int>> GetBookedSeatsAsync(string movieId, DateTime date, string time)
        {
            // Get the movie to find its cinema room
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId);
            if (movie == null || !movie.CinemaRoomId.HasValue)
                return new List<int>();

            // Get the schedule ID for the given time
            var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.ScheduleTime == time);
            if (schedule == null)
                return new List<int>();

            // Get all booked seats for this movie, date, and time
            var bookedSeats = await _context.ScheduleSeats
                .Where(ss => ss.ScheduleId == schedule.ScheduleId)
                .Where(ss => ss.SeatStatusId == 2) // Assuming 2 is the status for booked seats
                .Select(ss => ss.SeatId)
                .ToListAsync();

            return bookedSeats;
        }

        public async Task<List<SeatType>> GetSeatTypesAsync()
        {
            return await _context.SeatTypes.ToListAsync();
        }

        public Seat GetSeatByName(string seatName)
        {
            return _context.Seats.FirstOrDefault(s => s.SeatName == seatName);
        }

    }
}
