using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository;

public class CoupleSeatRepository : ICoupleSeatRepository
{
    private readonly MovieTheaterContext _context;

    public CoupleSeatRepository(MovieTheaterContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CoupleSeat>> GetAllAsync()
    {
        return await _context.CoupleSeats
            .Include(cs => cs.FirstSeat)
            .Include(cs => cs.SecondSeat)
            .ToListAsync();
    }

    public async Task<CoupleSeat?> GetByIdAsync(int id)
    {
        return await _context.CoupleSeats
            .Include(cs => cs.FirstSeat)
            .Include(cs => cs.SecondSeat)
            .FirstOrDefaultAsync(cs => cs.CoupleSeatId == id);
    }

    public async Task<IEnumerable<CoupleSeat>> GetBySeatIdAsync(int seatId)
    {
        return await _context.CoupleSeats
            .Include(cs => cs.FirstSeat)
            .Include(cs => cs.SecondSeat)
            .Where(cs => cs.FirstSeatId == seatId || cs.SecondSeatId == seatId)
            .ToListAsync();
    }

    public async Task<CoupleSeat> CreateAsync(CoupleSeat coupleSeat)
    {
        _context.CoupleSeats.Add(coupleSeat);
        await _context.SaveChangesAsync();
        return coupleSeat;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var coupleSeat = await _context.CoupleSeats.FindAsync(id);
        if (coupleSeat == null)
            return false;

        _context.CoupleSeats.Remove(coupleSeat);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsSeatInCoupleAsync(int seatId)
    {
        return await _context.CoupleSeats
            .AnyAsync(cs => cs.FirstSeatId == seatId || cs.SecondSeatId == seatId);
    }
} 