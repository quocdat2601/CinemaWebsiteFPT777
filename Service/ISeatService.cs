using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface ISeatService
    {
        public Task<List<Seat>> GetAllSeatsAsync();
        public Task<List<Seat>> GetSeatsByRoomIdAsync(int cinemaRoomId);
        public void AddSeatAsync(Seat seat);
        public void UpdateSeatAsync(Seat seat);
        public void DeleteSeatAsync(int id);
        public void Save();
        public Task<List<SeatType>> GetSeatTypesAsync();
        Seat GetSeatByName(string seatName);
        Seat GetSeatById(int? id);
        public Task DeleteCoupleSeatBySeatIdsAsync(int seatId1, int seatId2);
        public List<Seat> GetSeatsWithTypeByIds(List<int> seatIds);
        public List<Seat> GetSeatsByNames(List<string> seatNames);
    }
}
