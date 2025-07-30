using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface ICinemaRepository
    {
        public IEnumerable<CinemaRoom> GetAll();
        public CinemaRoom? GetById(int? id);
        Task<CinemaRoom?> GetByIdAsync(int? id);
        public void Add(CinemaRoom cinemaRoom);
        public void Update(CinemaRoom cinemaRoom);
        Task Delete(int id);
        Task Active(int id);
        Task Save();
        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId);
    }
}
