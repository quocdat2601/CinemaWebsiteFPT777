using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface ISeatService
    {
        public Task<List<Seat>> GetAllSeatsAsync();
        public Task<List<Seat>> GetSeatsByRoomIdAsync(int cinemaRoomId);
        public Task<Seat?> GetSeatByIdAsync(int id);
        public void AddSeatAsync(Seat seat);
        public void UpdateSeatAsync(Seat seat);
        public void DeleteSeatAsync(int id);
        public void Save();
        public Task<List<SeatType>> GetSeatTypesAsync();
        Seat GetSeatByName(string seatName);
    }
}
