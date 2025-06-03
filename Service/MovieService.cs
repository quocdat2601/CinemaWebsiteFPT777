using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MovieTheater.Services
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly ILogger<MovieService> _logger;

        public MovieService(IMovieRepository movieRepository, ILogger<MovieService> logger)
        {
            _movieRepository = movieRepository;
            _logger = logger;
        }

        public IEnumerable<Movie> GetAll()
        {
            return _movieRepository.GetAll();
        }

        public Movie? GetById(string id)
        {
            return _movieRepository.GetById(id);
        }

        public bool AddMovie(MovieDetailViewModel model)
        {
            var movie = new Movie
            {
                MovieNameEnglish = model.MovieNameEnglish,
                MovieNameVn = model.MovieNameVn,
                Actor = model.Actor,
                Director = model.Director,
                Duration = model.Duration,
                Version = model.Version,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                CinemaRoomId = model.CinemaRoomId,
                Content = model.Content,
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,

                Schedules = _movieRepository.GetSchedulesByIds(model.SelectedScheduleIds),
                ShowDates = _movieRepository.GetShowDatesByIds(model.SelectedShowDateIds),
                Types = _movieRepository.GetTypesByIds(model.SelectedTypeIds)
            };

            _movieRepository.Add(movie);
            _movieRepository.Save();

            return true;
        }


        public bool UpdateMovie(string id, MovieDetailViewModel model)
        {
            var movie = _movieRepository.GetById(id);
            if (movie == null) return false;

            movie.MovieNameEnglish = model.MovieNameEnglish;
            movie.MovieNameVn = model.MovieNameVn;
            movie.Actor = model.Actor;
            movie.Director = model.Director;
            movie.Duration = model.Duration;
            movie.Version = model.Version;
            movie.FromDate = model.FromDate;
            movie.ToDate = model.ToDate;
            movie.MovieProductionCompany = model.MovieProductionCompany;
            movie.CinemaRoomId = model.CinemaRoomId;
            movie.Content = model.Content;

            // Only update images if new ones are provided
            if (!string.IsNullOrEmpty(model.SmallImage))
                movie.SmallImage = model.SmallImage;

            if (!string.IsNullOrEmpty(model.LargeImage))
                movie.LargeImage = model.LargeImage;

            movie.Schedules = _movieRepository.GetSchedulesByIds(model.SelectedScheduleIds);
            movie.ShowDates = _movieRepository.GetShowDatesByIds(model.SelectedShowDateIds);
            movie.Types = _movieRepository.GetTypesByIds(model.SelectedTypeIds);

            _movieRepository.Update(movie);
            _movieRepository.Save();
            return true;
        }


        public bool DeleteMovie(string id)
        {
            _movieRepository.Delete(id);
            _movieRepository.Save();
            return true;
        }

        public void Save()
        {
            _movieRepository.Save();
        }

        public async Task<List<Schedule>> GetSchedulesAsync()
        {
            return await _movieRepository.GetSchedulesAsync();
        }
        public async Task<List<ShowDate>> GetShowDatesAsync()
        {
            return await _movieRepository.GetShowDatesAsync();
        }
        public async Task<List<Models.Type>> GetTypesAsync()
        {
            return await _movieRepository.GetTypesAsync();
        }

        public IEnumerable<Movie> SearchMovies(string searchTerm)
        {
            var movies = _movieRepository.GetAll();
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return movies.OrderBy(m => m.MovieNameEnglish);
            }

            searchTerm = searchTerm.Trim().ToLower();
            return movies
                .Where(m => m.MovieNameEnglish != null && m.MovieNameEnglish.ToLower().Contains(searchTerm))
                .OrderBy(m => m.MovieNameEnglish);
        }
    }
}
