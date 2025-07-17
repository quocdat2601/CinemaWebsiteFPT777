using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class CinemaRepository : ICinemaRepository
    {
        private readonly MovieTheaterContext _context;
        private readonly ISeatRepository _seatRepository;

        public CinemaRepository(MovieTheaterContext context, ISeatRepository seatRepository)
        {
            _context = context;
            _seatRepository = seatRepository;
        }

        public IEnumerable<CinemaRoom> GetAll()
        {
            return _context.CinemaRooms
                .Include(c => c.Version)
                .ToList();
        }

        public CinemaRoom? GetById(int? id)
        {
            return _context.CinemaRooms
                .Include(c => c.Version)
                .FirstOrDefault(a => a.CinemaRoomId == id);
        }

        private List<Seat> GenerateSeats(CinemaRoom cinemaRoom)
        {
            var seats = new List<Seat>();
            for (int row = 0; row < cinemaRoom.SeatLength; row++)
            {
                for (int col = 0; col < cinemaRoom.SeatWidth; col++)
                {
                    seats.Add(new Seat
                    {
                        CinemaRoomId = cinemaRoom.CinemaRoomId,
                        SeatRow = row + 1,
                        SeatColumn = (col + 1).ToString(),
                        SeatName = $"{(char)('A' + row)}{col + 1}",
                        SeatTypeId = 1,
                        SeatStatusId = 1
                    });
                }
            }
            return seats;
        }

        public void Add(CinemaRoom cinemaRoom)
        {
            _context.CinemaRooms.Add(cinemaRoom);
            _context.SaveChanges();

            var seats = GenerateSeats(cinemaRoom);

            _context.Seats.AddRange(seats);
            _context.SaveChanges();
        }

        public void Update(CinemaRoom cinemaRoom)
        {
            var existingCinema = _context.CinemaRooms
                .Include(c => c.Seats)
                .FirstOrDefault(c => c.CinemaRoomId == cinemaRoom.CinemaRoomId);

            if (existingCinema == null)
            {
                throw new KeyNotFoundException($"Cinema room with ID {cinemaRoom.CinemaRoomId} not found.");
            }

            try
            {
                existingCinema.CinemaRoomName = cinemaRoom.CinemaRoomName;
                existingCinema.VersionId = cinemaRoom.VersionId;

                if (existingCinema.SeatLength != cinemaRoom.SeatLength || existingCinema.SeatWidth != cinemaRoom.SeatWidth)
                {
                    _context.Seats.RemoveRange(existingCinema.Seats);

                    existingCinema.SeatLength = cinemaRoom.SeatLength;
                    existingCinema.SeatWidth = cinemaRoom.SeatWidth;

                    var newSeats = GenerateSeats(existingCinema);
                    _context.Seats.AddRange(newSeats);
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating cinema room: {ex.Message}", ex);
            }
        }

        public async Task Delete(int id)
        {
            var cinemaRoom = await _context.CinemaRooms.FirstOrDefaultAsync(m => m.CinemaRoomId == id);
            if (cinemaRoom != null)
            {
                var seats = await _seatRepository.GetByCinemaRoomIdAsync(id);

                var seatIds = seats.Select(s => s.SeatId).ToList();

                // Delete all CoupleSeat records involving these seats
                var coupleSeats = _context.CoupleSeats
                    .Where(cs => seatIds.Contains(cs.FirstSeatId) || seatIds.Contains(cs.SecondSeatId));
                _context.CoupleSeats.RemoveRange(coupleSeats);
                await _context.SaveChangesAsync();

                foreach (var seat in seats)
                {
                    await _seatRepository.DeleteAsync(seat.SeatId);
                }
                _context.CinemaRooms.Remove(cinemaRoom);
                await _context.SaveChangesAsync();
            }
        }

        public async Task Save()
        {
            await _context.SaveChangesAsync();
        }

        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId)
        {
            return _context.CinemaRooms
                .Include(c => c.Version)
                .Where(c => c.VersionId == versionId)
                .ToList();
        }
    }
}
