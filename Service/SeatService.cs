using Microsoft.EntityFrameworkCore;
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

        public async Task<Seat?> GetSeatByIdAsync(int id)
        {
            return await Task.FromResult(_repository.GetById(id));
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

        public async Task<List<int>> GetBookedSeatsAsync(string movieId, DateTime date, string time)
        {
            return await _repository.GetBookedSeatsAsync(movieId, date, time);
        }

        public async Task<List<SeatType>> GetSeatTypesAsync()
        {
            return await _repository.GetSeatTypesAsync();
        }

    }

}
