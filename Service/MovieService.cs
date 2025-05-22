using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Services
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;

        public MovieService(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
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


        public void UpdateMovie(Movie movie)
        {
            _movieRepository.Update(movie);
            _movieRepository.Save();
        }

        public void DeleteMovie(string id)
        {
            _movieRepository.Delete(id);
            _movieRepository.Save();
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

    }
}
