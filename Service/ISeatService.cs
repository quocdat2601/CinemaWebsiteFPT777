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
        public Task<List<int>> GetBookedSeatsAsync(string movieId, DateTime date, string time);
        public Task<List<SeatType>> GetSeatTypesAsync();
        public void UpdateSeatStatus(int? seatId);
        Task ResetSeatsAfterShowAsync(string movieId, DateTime showDate, string showTime);
        Seat GetSeatByName(string seatName);
    }
}
