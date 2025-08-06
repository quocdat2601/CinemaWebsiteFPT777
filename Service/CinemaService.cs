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
            if (cinemaRoom == null)
                throw new ArgumentNullException(nameof(cinemaRoom));
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

        public async Task<bool> Update(CinemaRoom cinemaRoom)
        {
            if (cinemaRoom == null)
                throw new ArgumentNullException(nameof(cinemaRoom));
            try
            {
                await _repository.Update(cinemaRoom);
                await _repository.Save();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error updating cinema room: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> Enable(CinemaRoom cinemaRoom)
        {
            if (cinemaRoom == null)
                throw new ArgumentNullException(nameof(cinemaRoom));
            try
            {
                await _repository.Enable(cinemaRoom);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enabling cinema room: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Disable(CinemaRoom cinemaRoom)
        {
            if (cinemaRoom == null)
                throw new ArgumentNullException(nameof(cinemaRoom));
            try
            {
                await _repository.Disable(cinemaRoom);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disabling cinema room: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId)
        {
            return _repository.GetRoomsByVersion(versionId);
        }
    }
}
