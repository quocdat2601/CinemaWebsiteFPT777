using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface ISeatTypeService
    {
        public void Update(SeatType seatType);
        public void Save();
        public IEnumerable<SeatType> GetAll();
        public SeatType? GetById(int id);

    }
}
