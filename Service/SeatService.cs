using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class SeatService : ISeatService
    {
        private readonly ISeatRepository _repository;

        public SeatService(ISeatRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Seat>> GetAllSeatsAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<List<Seat>> GetSeatsByRoomIdAsync(int cinemaRoomId)
        {
            return _repository.GetByCinemaRoomIdAsync(cinemaRoomId);
        }

        public async Task<List<SeatType>> GetSeatTypesAsync()
        {
            return await _repository.GetSeatTypesAsync();
        }

        public void AddSeatAsync(Seat seat)
        {
            _repository.Add(seat);
            _repository.Save();
        }

        public void UpdateSeatAsync(Seat seat)
        {
            _repository.Update(seat);
            _repository.Save();
        }

        public void DeleteSeatAsync(int id)
        {
            _repository.DeleteAsync(id);
            _repository.Save();
        }

        public void Save()
        {
            _repository.Save();
        }

        public Seat GetSeatByName(string seatName)
        {
            return _repository.GetSeatByName(seatName);
        }

        public Seat GetSeatById(int? id)
        {
            return _repository.GetById(id);
        }
        
        public async Task DeleteCoupleSeatBySeatIdsAsync(int seatId1, int seatId2)
        {
            await _repository.DeleteCoupleSeatBySeatIdsAsync(seatId1, seatId2);
        }
    }
}
