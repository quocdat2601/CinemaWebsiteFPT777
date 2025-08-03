using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface ISeatRepository
    {
        Task<List<Seat>> GetAllAsync();
        Task<List<Seat>> GetByCinemaRoomIdAsync(int cinemaRoomId);
        public Seat? GetById(int? id);
        public void Add(Seat seat);
        public void Update(Seat seat);
        Task DeleteAsync(int id);
        public void Save();
        Task<List<SeatType>> GetSeatTypesAsync();
        Seat GetSeatByName(string seatName);
        Task DeleteCoupleSeatBySeatIdsAsync(int seatId1, int seatId2);
        List<Seat> GetSeatsWithTypeByIds(List<int> seatIds);
    }
}
