using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service;

public class CoupleSeatService : ICoupleSeatService
{
    private readonly ICoupleSeatRepository _coupleSeatRepository;

    public CoupleSeatService(ICoupleSeatRepository coupleSeatRepository)
    {
        _coupleSeatRepository = coupleSeatRepository;
    }

    public async Task<IEnumerable<CoupleSeat>> GetAllCoupleSeatsAsync()
    {
        return await _coupleSeatRepository.GetAllAsync();
    }

    public async Task<CoupleSeat?> GetCoupleSeatByIdAsync(int id)
    {
        return await _coupleSeatRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<CoupleSeat>> GetCoupleSeatsBySeatIdAsync(int seatId)
    {
        return await _coupleSeatRepository.GetBySeatIdAsync(seatId);
    }

    public async Task<CoupleSeat> CreateCoupleSeatAsync(int firstSeatId, int secondSeatId)
    {
        // Validate the pair before creating
        if (!await ValidateCoupleSeatPairAsync(firstSeatId, secondSeatId))
        {
            throw new InvalidOperationException("Invalid couple seat pair. Please check the seat IDs and ensure they are not already in a couple seat.");
        }

        // Ensure FirstSeatId is always less than SecondSeatId (matching database constraint)
        if (firstSeatId > secondSeatId)
        {
            (firstSeatId, secondSeatId) = (secondSeatId, firstSeatId);
        }

        var coupleSeat = new CoupleSeat
        {
            FirstSeatId = firstSeatId,
            SecondSeatId = secondSeatId
        };

        return await _coupleSeatRepository.CreateAsync(coupleSeat);
    }

    public async Task<bool> DeleteCoupleSeatAsync(int id)
    {
        return await _coupleSeatRepository.DeleteAsync(id);
    }

    public async Task<bool> IsSeatInCoupleAsync(int seatId)
    {
        return await _coupleSeatRepository.IsSeatInCoupleAsync(seatId);
    }

    public async Task<bool> ValidateCoupleSeatPairAsync(int firstSeatId, int secondSeatId)
    {
        // Check if seats are the same (matching database constraint)
        if (firstSeatId == secondSeatId)
        {
            return false;
        }

        // Check if either seat is already in a couple seat (matching unique constraints)
        if (await _coupleSeatRepository.IsSeatInCoupleAsync(firstSeatId) ||
            await _coupleSeatRepository.IsSeatInCoupleAsync(secondSeatId))
        {
            return false;
        }

        return true;
    }
} 