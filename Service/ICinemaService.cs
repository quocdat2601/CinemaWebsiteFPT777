using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface ICinemaService
    {
        public IEnumerable<CinemaRoom> GetAll();
        public CinemaRoom? GetById(int? id);
        public void Add(CinemaRoom cinemaRoom);
        Task<bool> Update(CinemaRoom cinemaRoom);
        Task<bool> DeleteAsync(int id);
        Task SaveAsync();
        public Task<bool> Enable(CinemaRoom cinemaRoom);
        public Task<bool> Disable(CinemaRoom cinemaRoom);
        public IEnumerable<CinemaRoom> GetRoomsByVersion(int versionId);
    }
}
