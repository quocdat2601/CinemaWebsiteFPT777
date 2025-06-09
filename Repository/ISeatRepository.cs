using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface ISeatRepository
    {
        Task<List<Seat>> GetAllAsync();
        Task<List<Seat>> GetByCinemaRoomIdAsync(int cinemaRoomId);
        public Seat? GetById(int id);
        public void Add(Seat seat);
        public void Update(Seat seat);
        Task DeleteAsync(int id);
        public void Save();
        Task<List<int>> GetBookedSeatsAsync(string movieId, DateTime date, string time);
        Task<List<SeatType>> GetSeatTypesAsync();
        public void UpdateSeatAndScheduleStatus(int seatId, int statusId);
    }
}
