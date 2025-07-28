using MovieTheater.Models;

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
        public List<Models.Type> GetTypes();
        public List<CinemaRoom> GetAllCinemaRooms();
        public IEnumerable<Movie> SearchMovies(string searchTerm);
        public string ConvertToEmbedUrl(string trailerUrl);
        public List<MovieShow> GetMovieShows(string movieId);
        public bool IsScheduleAvailable(DateOnly showDate, int scheduleId, int cinemaRoomId, int movieDuration);
        public bool AddMovieShow(MovieShow movieShow);
        public bool AddMovieShows(List<MovieShow> movieShows);
        public List<Models.Type> GetAllTypes();
        public List<Schedule> GetAllSchedules();
        public bool DeleteAllMovieShows(string movieId);
        public bool DeleteMovieShows(int movieShowId);
        public Task<List<Schedule>> GetAvailableSchedulesAsync(DateOnly showDate, int cinemaRoomId);
        public List<DateOnly> GetShowDates(string movieId);
        public List<MovieShow> GetMovieShowsByRoomAndDate(int cinemaRoomId, DateOnly showDate);
        public List<MovieShow> GetMovieShow();
        public MovieShow? GetMovieShowById(int id);
        public List<MovieShow> GetMovieShowsByMovieId(string movieId);
        public List<Models.Version> GetAllVersions();
        public Models.Version? GetVersionById(int versionId);
        public MovieShow? GetMovieShowByCinemaRoomId(int cinemaRoomId);
    }
}
