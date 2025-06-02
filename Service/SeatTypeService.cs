using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class SeatTypeService : ISeatTypeService
    {
        private readonly ISeatTypeRepository _seatTypeRepository;

        public SeatTypeService(ISeatTypeRepository seatTypeRepository)
        {
            _seatTypeRepository = seatTypeRepository;
        }

        public void Update(SeatType seatType)
        {
            _seatTypeRepository.Update(seatType);
        }

        public IEnumerable<SeatType> GetAll()
        {
            return _seatTypeRepository.GetAll();
        }
        public SeatType? GetById(int id)
        {
            return _seatTypeRepository.GetById(id);
        }

        public void Save()
        {
            _seatTypeRepository.Save();
        }

    }
}
