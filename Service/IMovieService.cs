using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IMovieService
    {
        public IEnumerable<Movie> GetAll();
        public Movie? GetById(string id);
        public bool AddMovie(Movie movie);
        public bool UpdateMovie(Movie movie);
        public bool DeleteMovie(string id);
        public void Save();
        public List<Schedule> GetSchedules();
        public List<ShowDate> GetShowDates();
        public List<Models.Type> GetTypes();
        public List<CinemaRoom> GetAllCinemaRooms();
        public IEnumerable<Movie> SearchMovies(string searchTerm);
        public string ConvertToEmbedUrl(string trailerUrl);
        public List<MovieShow> GetMovieShows(string movieId);
        public bool IsScheduleAvailable(int showDateId, int scheduleId, int cinemaRoomId);
        public bool AddMovieShow(MovieShow movieShow);
        public bool AddMovieShows(List<MovieShow> movieShows);
        public List<Models.Type> GetAllTypes();
        public List<ShowDate> GetAllShowDates();
        public List<Schedule> GetAllSchedules();
        public bool DeleteAllMovieShows(string movieId);
        public Task<List<Schedule>> GetAvailableSchedulesAsync(int showDateId, int cinemaRoomId);
    }
}
