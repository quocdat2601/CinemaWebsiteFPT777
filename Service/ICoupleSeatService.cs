using MovieTheater.Models;

namespace MovieTheater.Service;

public interface ICoupleSeatService
{
    Task<IEnumerable<CoupleSeat>> GetAllCoupleSeatsAsync();
    Task<CoupleSeat?> GetCoupleSeatByIdAsync(int id);
    Task<IEnumerable<CoupleSeat>> GetCoupleSeatsBySeatIdAsync(int seatId);
    Task<CoupleSeat> CreateCoupleSeatAsync(int firstSeatId, int secondSeatId);
    Task<bool> DeleteCoupleSeatAsync(int id);
    Task<bool> IsSeatInCoupleAsync(int seatId);
    Task<bool> ValidateCoupleSeatPairAsync(int firstSeatId, int secondSeatId);
} 