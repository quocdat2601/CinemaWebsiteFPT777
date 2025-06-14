using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MovieTheater.Service
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

        public bool AddMovie(Movie movie)
        {
            try
            {
                if (!_movieRepository.Add(movie))
                {
                    _logger.LogError("Failed to add movie: {MovieId}", movie.MovieId);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie: {MovieId}", movie.MovieId);
                return false;
            }
        }

        public bool UpdateMovie(Movie movie)
        {
            try
            {
                if (!_movieRepository.Update(movie))
                {
                    _logger.LogError("Failed to update movie: {MovieId}", movie.MovieId);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie: {MovieId}", movie.MovieId);
                return false;
            }
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

        public List<MovieShow> GetMovieShow()
        {
            return _movieRepository.GetMovieShow();
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

        public string ConvertToEmbedUrl(string trailerUrl)
        {
            if (string.IsNullOrWhiteSpace(trailerUrl))
                return trailerUrl;

            string videoId = null;

            var longUrlPattern = @"youtube\.com/watch\?v=([a-zA-Z0-9_-]+)";
            var longMatch = Regex.Match(trailerUrl, longUrlPattern);
            if (longMatch.Success)
            {
                videoId = longMatch.Groups[1].Value;
            }

            var shortUrlPattern = @"youtu\.be/([a-zA-Z0-9_-]+)";
            var shortMatch = Regex.Match(trailerUrl, shortUrlPattern);
            if (shortMatch.Success)
            {
                videoId = shortMatch.Groups[1].Value;
            }

            return videoId != null ? $"https://www.youtube.com/embed/{videoId}" : trailerUrl;
        }

        public List<MovieShow> GetMovieShows(string movieId)
        {
            return _movieRepository.GetMovieShowsByMovieId(movieId);
        }

        public bool IsScheduleAvailable(int showDateId, int scheduleId, int cinemaRoomId)
        {
            return _movieRepository.IsScheduleAvailable(showDateId, scheduleId, cinemaRoomId);
        }

        public bool AddMovieShow(MovieShow movieShow)
        {
            try
            {
                if (!movieShow.ShowDateId.HasValue || !movieShow.ScheduleId.HasValue || !movieShow.CinemaRoomId.HasValue)
                {
                    return false;
                }

                if (!IsScheduleAvailable(movieShow.ShowDateId.Value, movieShow.ScheduleId.Value, movieShow.CinemaRoomId.Value))
                {
                    return false;
                }

                _movieRepository.AddMovieShow(movieShow);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding movie show: {ex.Message}");
                return false;
            }
        }

        public bool AddMovieShows(List<MovieShow> movieShows)
        {
            try
            {
                // Check all schedules for availability
                foreach (var show in movieShows)
                {
                    if (!show.ShowDateId.HasValue || !show.ScheduleId.HasValue || !show.CinemaRoomId.HasValue)
                    {
                        return false;
                    }

                    if (!IsScheduleAvailable(show.ShowDateId.Value, show.ScheduleId.Value, show.CinemaRoomId.Value))
                    {
                        return false;
                    }
                }

                _movieRepository.AddMovieShows(movieShows);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding movie shows: {ex.Message}");
                return false;
            }
        }

        public List<Models.Type> GetAllTypes()
        {
            return _movieRepository.GetTypesAsync().Result;
        }

        public List<ShowDate> GetAllShowDates()
        {
            return _movieRepository.GetShowDatesAsync().Result;
        }

        public List<Schedule> GetAllSchedules()
        {
            return _movieRepository.GetSchedulesAsync().Result;
        }

        public List<CinemaRoom> GetAllCinemaRooms()
        {
            return _movieRepository.GetAllCinemaRooms();
        }

        public List<Schedule> GetSchedules()
        {
            return _movieRepository.GetSchedules();
        }

        public List<ShowDate> GetShowDates()
        {
            return _movieRepository.GetShowDates();
        }

        public List<Models.Type> GetTypes()
        {
            return _movieRepository.GetTypes();
        }

        public bool DeleteAllMovieShows(string movieId)
        {
            try
            {
                var existingShows = _movieRepository.GetMovieShowsByMovieId(movieId);
                foreach (var show in existingShows)
                {
                    _movieRepository.DeleteMovieShow(show.MovieShowId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie shows for movie: {MovieId}", movieId);
                return false;
            }
        }

        public async Task<List<Schedule>> GetAvailableSchedulesAsync(int showDateId, int cinemaRoomId)
        {
            return await _movieRepository.GetAvailableSchedulesAsync(showDateId, cinemaRoomId);
        }
    }
}
