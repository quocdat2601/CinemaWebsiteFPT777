using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IMovieRepository
    {
        public IEnumerable<Movie> GetAll();
        public Movie? GetById(string id);
        public void Add(Movie movie);
        public void Update(Movie movie);
        public void Delete(string id);
        public void Save();
        string GenerateMovieId();
        Task<List<Schedule>> GetSchedulesAsync();
        Task<List<ShowDate>> GetShowDatesAsync();
        Task<List<Models.Type>> GetTypesAsync();
        public List<Schedule> GetSchedulesByIds(List<int> ids);
        public List<ShowDate> GetShowDatesByIds(List<int> ids);
        public List<Models.Type> GetTypesByIds(List<int> ids);
    }
}
