using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface ICinemaRepository
    {
        public IEnumerable<CinemaRoom> GetAll();
        public CinemaRoom? GetById(int? id);
        public void Add(CinemaRoom cinemaRoom);
        public void Update(CinemaRoom cinemaRoom);
        Task Delete(int id);
        Task Save();

    }
}
