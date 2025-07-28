using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Repository
{
    public interface IMovieRepository
    {
        public string GenerateMovieId();
        public IEnumerable<Movie> GetAll();
        public Movie? GetById(string id);
        public bool Add(Movie movie);
        public bool Update(Movie movie);
        public void Delete(string id);
        public void DeleteMovieShow(int movieShowId);
        public void Save();
        Task<List<Schedule>> GetSchedulesAsync();
        Task<List<Models.Type>> GetTypesAsync();
        public List<Schedule> GetSchedulesByIds(List<int> ids);
        public List<Models.Type> GetTypesByIds(List<int> ids);
        Task<List<Movie>> GetAllMoviesAsync();
        Task<List<DateOnly>> GetShowDatesAsync(string movieId);
        Task<List<string>> GetShowTimesAsync(string movieId, DateTime date);
        public void AddMovieShow(MovieShow movieShow);
        public void AddMovieShows(List<MovieShow> movieShows);
        public List<MovieShow> GetMovieShowsByMovieId(string movieId);
        public bool IsScheduleAvailable(DateOnly showDate, int scheduleId, int cinemaRoomId, int movieDuration);
        public List<CinemaRoom> GetAllCinemaRooms();
        public List<Schedule> GetSchedules();
        public List<Models.Type> GetTypes();
        public List<MovieShow> GetMovieShow();
        public MovieShow? GetMovieShowById(int id);
        public List<DateOnly> GetShowDates(string movieId);
        public List<string> GetShowTimes(string movieId, DateTime date);
        Task<List<Schedule>> GetAvailableSchedulesAsync(DateOnly showDate, int cinemaRoomId);
        public List<MovieShow> GetMovieShowsByRoomAndDate(int cinemaRoomId, DateOnly showDate);
        public Dictionary<DateOnly, List<string>> GetMovieShowSummaryByMonth(int year, int month);
        List<Models.Version> GetAllVersions();
        Models.Version? GetVersionById(int versionId);
        

    }
}
