using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface ICinemaRepository
    {
        public IEnumerable<CinemaRoom> GetAll();
        public CinemaRoom? GetById(int? id);
        Task<CinemaRoom?> GetByIdAsync(int? id);
        public void Add(CinemaRoom cinemaRoom);
        Task Update(CinemaRoom cinemaRoom);
        Task Delete(int id);
        Task Disable(CinemaRoom cinemaRoom);
        Task Enable(CinemaRoom cinemaRoom);
        Task Save();
        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId);
    }
}
