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

        public Seat? GetById(int? id)
        {
            return _context.Seats.Include(s => s.SeatType).FirstOrDefault(s => s.SeatId == id);
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



        public async Task<List<SeatType>> GetSeatTypesAsync()
        {
            return await _context.SeatTypes.ToListAsync();
        }

        public Seat GetSeatByName(string seatName)
        {
            return _context.Seats.FirstOrDefault(s => s.SeatName == seatName);
        }

        public async Task DeleteCoupleSeatBySeatIdsAsync(int seatId1, int seatId2)
        {
            var coupleSeat = await _context.CoupleSeats
                .FirstOrDefaultAsync(cs =>
                    (cs.FirstSeatId == seatId1 && cs.SecondSeatId == seatId2) ||
                    (cs.FirstSeatId == seatId2 && cs.SecondSeatId == seatId1));

            if (coupleSeat != null)
            {
                _context.CoupleSeats.Remove(coupleSeat);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteCoupleSeatsByCinemaRoomAsync(int cinemaRoomId)
        {
            // Get all seats for this cinema room
            var seatIds = await _context.Seats
                .Where(s => s.CinemaRoomId == cinemaRoomId)
                .Select(s => s.SeatId)
                .ToListAsync();

            // Delete all couple seats that reference any of these seats
            var coupleSeatsToDelete = await _context.CoupleSeats
                .Where(cs => seatIds.Contains(cs.FirstSeatId) || seatIds.Contains(cs.SecondSeatId))
                .ToListAsync();

            if (coupleSeatsToDelete.Any())
            {
                _context.CoupleSeats.RemoveRange(coupleSeatsToDelete);
                await _context.SaveChangesAsync();
            }
        }

        public List<Seat> GetSeatsWithTypeByIds(List<int> seatIds)
        {
            return _context.Seats.Include(s => s.SeatType).Where(s => seatIds.Contains(s.SeatId)).ToList();
        }

        public List<Seat> GetSeatsByNames(List<string> seatNames)
        {
            return _context.Seats.Include(s => s.SeatType).Where(s => seatNames.Contains(s.SeatName)).ToList();
        }

        public async Task UpdateSeatsStatusToBookedAsync(List<int> seatIds)
        {
            // Tìm SeatStatusId cho trạng thái "Booked"
            var bookedStatus = _context.SeatStatuses.FirstOrDefault(s => s.StatusName == "Booked");
            if (bookedStatus == null)
            {
                throw new InvalidOperationException("SeatStatus 'Booked' not found in database");
            }

            // Tìm và cập nhật trạng thái seat
            var seats = _context.Seats.Where(s => seatIds.Contains(s.SeatId)).ToList();
            
            foreach (var seat in seats)
            {
                seat.SeatStatusId = bookedStatus.SeatStatusId; // Chuyển từ BeingHeld thành Booked
            }

            await _context.SaveChangesAsync();
        }

        public SeatStatus? GetSeatStatusByName(string statusName)
        {
            return _context.SeatStatuses.FirstOrDefault(s => s.StatusName == statusName);
        }
    }
}
