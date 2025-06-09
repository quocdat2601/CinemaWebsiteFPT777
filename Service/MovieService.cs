using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

        public bool AddMovie(Movie movie, List<int> showDateIds, List<int> scheduleIds)
        {
            try
            {
                // First add the movie
                if (!_movieRepository.Add(movie))
                {
                    _logger.LogError("Failed to add movie: {MovieId}", movie.MovieId);
                    return false;
                }

                // Create MovieShow records for each combination of show date and schedule
                var movieShows = new List<MovieShow>();
                foreach (var showDateId in showDateIds)
                {
                    foreach (var scheduleId in scheduleIds)
                    {
                        // Check if the schedule is available
                        if (!_movieRepository.IsScheduleAvailable(showDateId, scheduleId, movie.CinemaRoomId))
                        {
                            _logger.LogWarning("Schedule conflict detected for MovieId: {MovieId}, ShowDateId: {ShowDateId}, ScheduleId: {ScheduleId}, CinemaRoomId: {CinemaRoomId}",
                                movie.MovieId, showDateId, scheduleId, movie.CinemaRoomId);
                            continue;
                        }

                        movieShows.Add(new MovieShow
                        {
                            MovieId = movie.MovieId,
                            ShowDateId = showDateId,
                            ScheduleId = scheduleId,
                            CinemaRoomId = movie.CinemaRoomId
                        });
                    }
                }

                // Add all MovieShow records
                if (movieShows.Any())
                {
                    try
                    {
                        _movieRepository.AddMovieShows(movieShows);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to add movie shows for movie: {MovieId}", movie.MovieId);
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("No valid movie shows could be created for movie: {MovieId}", movie.MovieId);
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

        public bool UpdateMovie(Movie movie, List<int> showDateIds, List<int> scheduleIds)
        {
            try
            {
                // First update the movie
                if (!_movieRepository.Update(movie))
                {
                    _logger.LogError("Failed to update movie: {MovieId}", movie.MovieId);
                    return false;
                }

                // Remove existing MovieShow records
                var existingShows = _movieRepository.GetMovieShowsByMovieId(movie.MovieId);
                foreach (var show in existingShows)
                {
                    _movieRepository.DeleteMovieShow(show.MovieShowId);
                }

                // Create new MovieShow records
                var movieShows = new List<MovieShow>();
                foreach (var showDateId in showDateIds)
                {
                    foreach (var scheduleId in scheduleIds)
                    {
                        // Check if the schedule is available
                        if (!_movieRepository.IsScheduleAvailable(showDateId, scheduleId, movie.CinemaRoomId))
                        {
                            _logger.LogWarning("Schedule conflict detected for MovieId: {MovieId}, ShowDateId: {ShowDateId}, ScheduleId: {ScheduleId}, CinemaRoomId: {CinemaRoomId}",
                                movie.MovieId, showDateId, scheduleId, movie.CinemaRoomId);
                            continue;
                        }

                        movieShows.Add(new MovieShow
                        {
                            MovieId = movie.MovieId,
                            ShowDateId = showDateId,
                            ScheduleId = scheduleId,
                            CinemaRoomId = movie.CinemaRoomId
                        });
                    }
                }

                // Add all MovieShow records
                if (movieShows.Any())
                {
                    try
                    {
                        _movieRepository.AddMovieShows(movieShows);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to add movie shows for movie: {MovieId}", movie.MovieId);
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("No valid movie shows could be created for movie: {MovieId}", movie.MovieId);
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
    }
}
