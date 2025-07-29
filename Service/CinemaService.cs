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

        public bool Update(CinemaRoom cinemaRoom)
        {
            try
            {
                _repository.Update(cinemaRoom);
                _repository.Save().Wait(); // Ensure changes are saved to database
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error updating cinema room: {ex.Message}");
                return false;
            }
        }
        public bool Enable(CinemaRoom cinemaRoom)
        {
            _repository.Enable(cinemaRoom);
            return true;
        }
        public bool Disable(CinemaRoom cinemaRoom)
        {
            _repository.Disable(cinemaRoom);
            return true;
        }

        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId){
            return _repository.GetRoomsByVersion(versionId);
        }
    }
}
