using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class BookingService : IBookingService
    {
        private readonly IMovieRepository _repo;

        public BookingService(IMovieRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Movie>> GetAvailableMoviesAsync()
        {
            return _repo.GetAllMoviesAsync();
        }

        public Movie GetById(string movieId)
        {
            return _repo.GetById(movieId);
        }

        public List<Schedule> GetSchedulesByIds(List<int> ids)
        {
            return _repo.GetSchedulesByIds(ids);
        }

        public List<ShowDate> GetShowDatesByIds(List<int> ids)
        {
            return _repo.GetShowDatesByIds(ids);
        }

        public Task<List<DateTime>> GetShowDatesAsync(string movieId)
        {
            return _repo.GetShowDatesAsync(movieId);
        }

        public Task<List<string>> GetShowTimesAsync(string movieId, DateTime date)
        {
            return _repo.GetShowTimesAsync(movieId, date);
        }
    }
}
