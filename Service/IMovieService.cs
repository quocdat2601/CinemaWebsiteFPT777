using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Services
{
    public interface IMovieService
    {
        IEnumerable<Movie> GetAll();
        Movie? GetById(string id);
        bool AddMovie(MovieDetailViewModel movie);
        void UpdateMovie(Movie movie);
        void DeleteMovie(string id);
        void Save();
        Task<List<Schedule>> GetSchedulesAsync();
        Task<List<ShowDate>> GetShowDatesAsync();
        Task<List<Models.Type>> GetTypesAsync();
    }
}
