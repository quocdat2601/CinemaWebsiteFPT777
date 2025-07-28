using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface ICinemaService
    {
        public IEnumerable<CinemaRoom> GetAll();
        public CinemaRoom? GetById(int? id);
        public void Add(CinemaRoom cinemaRoom);
        public bool Update(int id, CinemaRoom cinemaRoom);
        Task<bool> DeleteAsync(int id);
        Task SaveAsync();
        Task Active(int id);
        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId);
    }
}
