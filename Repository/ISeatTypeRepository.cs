using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface ISeatTypeRepository
    {
        public IEnumerable<SeatType> GetAll();
        public void Update(SeatType seatType);
        public void Save();
        public SeatType? GetById(int id);

    }
}
