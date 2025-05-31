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

        public async Task<bool> DeleteAsync(int id)
        {
            await _repository.Delete(id);
            await _repository.Save();
            return true;
        }

        public IEnumerable<CinemaRoom> GetAll()
        {
            return _repository.GetAll();
        }

        public CinemaRoom? GetById(int? id)
        {
            return _repository.GetById(id);
        }

        public async Task SaveAsync()
        {
            await _repository.Save();
        }

        public bool Update(int id, CinemaRoom cinemaRoom)
        {
            try
            {
                _repository.Update(cinemaRoom);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
