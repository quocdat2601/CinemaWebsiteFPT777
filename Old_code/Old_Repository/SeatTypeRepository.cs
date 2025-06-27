using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class SeatTypeRepository : ISeatTypeRepository
    {
        private readonly MovieTheaterContext _context;

        public SeatTypeRepository(MovieTheaterContext context)
        {
            _context = context;
        }
        public IEnumerable<SeatType> GetAll()
        {
            return _context.SeatTypes.ToList();
        }
        public void Update(SeatType seatType)
        {
            var existing = _context.SeatTypes.FirstOrDefault(a => a.SeatTypeId == seatType.SeatTypeId);
            if (existing == null) return;

            existing.PricePercent = seatType.PricePercent;
            existing.ColorHex = seatType.ColorHex;
            _context.Entry(existing).State = EntityState.Modified;
        }
        public SeatType? GetById(int id)
        {
            return _context.SeatTypes.FirstOrDefault(a => a.SeatTypeId == id);
        }
        public void Save()
        {
            _context.SaveChanges();
        }

    }
}
