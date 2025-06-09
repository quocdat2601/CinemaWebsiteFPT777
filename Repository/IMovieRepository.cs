using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IMovieRepository
    {
        public IEnumerable<Movie> GetAll();
        public Movie? GetById(string id);
        public void Save();
        string GenerateMovieId();
        Task<List<Movie>> GetAllMoviesAsync();
        Task<List<Schedule>> GetSchedulesAsync();
        Task<List<ShowDate>> GetShowDatesAsync();
        Task<List<Models.Type>> GetTypesAsync();
        public List<Schedule> GetSchedulesByIds(List<int> ids);
        public List<ShowDate> GetShowDatesByIds(List<int> ids);
        public List<Models.Type> GetTypesByIds(List<int> ids);
        Task<List<DateTime>> GetShowDatesAsync(string movieId);
        Task<List<string>> GetShowTimesAsync(string movieId, DateTime date);
        
        // New methods for MovieShow
        public void AddMovieShow(MovieShow movieShow);
        public void AddMovieShows(List<MovieShow> movieShows);
        public List<MovieShow> GetMovieShowsByMovieId(string movieId);
        public bool IsScheduleAvailable(int showDateId, int scheduleId, int? cinemaRoomId);
        public void DeleteMovieShow(int movieShowId);
        bool Add(Movie movie);
        bool Update(Movie movie);
        void Delete(string id);
        List<CinemaRoom> GetAllCinemaRooms();
    }
}
