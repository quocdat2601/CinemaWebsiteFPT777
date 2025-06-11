using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IMovieRepository
    {
        public IEnumerable<Movie> GetAll();
        public Movie? GetById(string id);
        public void Save();
        string GenerateMovieId();
        public bool Add(Movie movie);
        public bool Update(Movie movie);
        public void Delete(string id);
        public void DeleteMovieShow(int movieShowId);
        public List<Schedule> GetSchedules();
        public List<ShowDate> GetShowDates();
        public List<Models.Type> GetTypes();
        public List<Schedule> GetSchedulesByIds(List<int> ids);
        public List<ShowDate> GetShowDatesByIds(List<int> ids);
        public List<Models.Type> GetTypesByIds(List<int> ids);
        public List<DateTime> GetShowDates(string movieId);
        public List<string> GetShowTimes(string movieId, DateTime date);
        public void AddMovieShow(MovieShow movieShow);
        public void AddMovieShows(List<MovieShow> movieShows);
        public List<MovieShow> GetMovieShowsByMovieId(string movieId);
        public bool IsScheduleAvailable(int showDateId, int scheduleId, int? cinemaRoomId);
        public List<CinemaRoom> GetAllCinemaRooms();
        public Task<List<Schedule>> GetSchedulesAsync();
        public Task<List<ShowDate>> GetShowDatesAsync();
        public Task<List<Models.Type>> GetTypesAsync();
        public Task<List<Movie>> GetAllMoviesAsync();
        public Task<List<DateTime>> GetShowDatesAsync(string movieId);
        public Task<List<string>> GetShowTimesAsync(string movieId, DateTime date);
        public Task<List<Schedule>> GetAvailableSchedulesAsync(int showDateId, int cinemaRoomId);
    }
}
