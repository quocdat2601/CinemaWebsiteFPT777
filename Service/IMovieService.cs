using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IMovieService
    {
        IEnumerable<Movie> GetAll();
        Movie? GetById(string id);
        bool AddMovie(Movie movie, List<int> showDateIds, List<int> scheduleIds);
        bool UpdateMovie(Movie movie, List<int> showDateIds, List<int> scheduleIds);
        bool DeleteMovie(string id);
        void Save();
        Task<List<Schedule>> GetSchedulesAsync();
        Task<List<ShowDate>> GetShowDatesAsync();
        Task<List<Models.Type>> GetTypesAsync();
        IEnumerable<Movie> SearchMovies(string searchTerm);
        string ConvertToEmbedUrl(string trailerUrl);

        // New methods for MovieShow
        List<MovieShow> GetMovieShows(string movieId);
        bool IsScheduleAvailable(int showDateId, int scheduleId, int cinemaRoomId);
        bool AddMovieShow(MovieShow movieShow);
        bool AddMovieShows(List<MovieShow> movieShows);

        // Additional methods for getting available options
        List<Models.Type> GetAllTypes();
        List<ShowDate> GetAllShowDates();
        List<Schedule> GetAllSchedules();
        List<CinemaRoom> GetAllCinemaRooms();
    }
}
