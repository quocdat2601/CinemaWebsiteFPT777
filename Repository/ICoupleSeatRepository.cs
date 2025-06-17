using MovieTheater.Models;

namespace MovieTheater.Repository;

public interface ICoupleSeatRepository
{
    Task<IEnumerable<CoupleSeat>> GetAllAsync();
    Task<CoupleSeat?> GetByIdAsync(int id);
    Task<IEnumerable<CoupleSeat>> GetBySeatIdAsync(int seatId);
    Task<CoupleSeat> CreateAsync(CoupleSeat coupleSeat);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsSeatInCoupleAsync(int seatId);
}