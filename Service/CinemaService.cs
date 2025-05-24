using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class CinemaService : ICinemaService
    {
        private readonly ICinemaRepository _repository;

        public CinemaService(ICinemaRepository repository)
        {
            _repository = repository;
        }

        public void Add(CinemaRoom cinemaRoom)
        {
            _repository.Add(cinemaRoom);
        }

        public void Delete(int id)
        {
            _repository.Delete(id);
        }

        public IEnumerable<CinemaRoom> GetAll()
        {
            return _repository.GetAll();

        }

        public CinemaRoom? GetById(int? id)
        {
            return _repository.GetById(id);

        }

        public void Save()
        {
            _repository.Save();

        }

        public void Update(CinemaRoom cinemaRoom)
        {
            _repository.Update(cinemaRoom);

        }
    }
}
