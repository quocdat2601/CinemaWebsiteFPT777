using MovieTheater.Models;
using MovieTheater.Repository;
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
        public MovieShow? GetMovieShowById(int id)
        {
            return _movieRepository.GetMovieShowById(id);
        }

        public List<MovieShow> GetMovieShows(string movieId)
        {
            return _movieRepository.GetMovieShowsByMovieId(movieId);
        }

        public bool IsScheduleAvailable(DateOnly showDate, int scheduleId, int cinemaRoomId, int movieDuration)
        {
            return _movieRepository.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration);
        }

        public bool AddMovieShow(MovieShow movieShow)
        {
            try
            {
                var movie = _movieRepository.GetById(movieShow.MovieId);
                if (movie == null || movie.Duration == null)
                {
                    _logger.LogWarning("AddMovieShow: Movie not found or has no duration for movie ID {MovieId}", movieShow.MovieId);
                    return false;
                }

                if (!IsScheduleAvailable(movieShow.ShowDate, movieShow.ScheduleId, movieShow.CinemaRoomId, movie.Duration.Value))
                {
                    _logger.LogWarning("AddMovieShow: Schedule is not available for Movie {MovieId}, Room {CinemaRoomId}, Date {ShowDate}, Schedule {ScheduleId}", movieShow.MovieId, movieShow.CinemaRoomId, movieShow.ShowDate, movieShow.ScheduleId);
                    return false;
                }

                _movieRepository.AddMovieShow(movieShow);
                _movieRepository.Save();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie show");
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
                    var movie = _movieRepository.GetById(show.MovieId);
                    if (movie == null || movie.Duration == null)
                    {
                        _logger.LogWarning("AddMovieShows: Movie not found or has no duration for movie ID {MovieId}", show.MovieId);
                        return false;
                    }

                    if (!IsScheduleAvailable(show.ShowDate, show.ScheduleId, show.CinemaRoomId, movie.Duration.Value))
                    {
                        _logger.LogWarning("AddMovieShows: Schedule is not available for Movie {MovieId}, Room {CinemaRoomId}, Date {ShowDate}, Schedule {ScheduleId}", show.MovieId, show.CinemaRoomId, show.ShowDate, show.ScheduleId);
                        return false;
                    }
                }

                _movieRepository.AddMovieShows(movieShows);
                _movieRepository.Save();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie shows");
                return false;
            }
        }

        public List<Models.Type> GetAllTypes()
        {
            return _movieRepository.GetTypesAsync().Result;
        }

        public List<Models.Version> GetAllVersions()
        {
            return _movieRepository.GetAllVersions();
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
                _movieRepository.Save();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie shows for movie: {MovieId}", movieId);
                return false;
            }
        }
        public bool DeleteMovieShows(int movieShowId)
        {
            try
            {
                _movieRepository.DeleteMovieShow(movieShowId);
                _movieRepository.Save();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie shows:", movieShowId);
                return false;
            }
        }


        public async Task<List<Schedule>> GetAvailableSchedulesAsync(DateOnly showDate, int cinemaRoomId)
        {
            return await _movieRepository.GetAvailableSchedulesAsync(showDate, cinemaRoomId);
        }

        public List<DateOnly> GetShowDates(string movieId)
        {
            return _movieRepository.GetShowDates(movieId);
        }

        public List<MovieShow> GetMovieShowsByRoomAndDate(int cinemaRoomId, DateOnly showDate)
        {
            return _movieRepository.GetMovieShowsByRoomAndDate(cinemaRoomId, showDate);
        }

        public List<MovieShow> GetMovieShowsByMovieVersionDate(string movieId, int versionId, DateOnly showDate)
        {
            return _movieRepository.GetMovieShowsByMovieVersionDate(movieId, versionId, showDate);
        }

        public List<MovieShow> GetMovieShowsByMovieId(string movieId)
        {
            return _movieRepository.GetMovieShowsByMovieId(movieId);
        }

        public Models.Version? GetVersionById(int versionId)
        {
            return _movieRepository.GetVersionById(versionId);
        }

        // New methods for categorizing movies
        public List<Movie> GetCurrentlyShowingMovies()
        {
            try
            {
                return _movieRepository.GetCurrentlyShowingMovies();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currently showing movies");
                return new List<Movie>();
            }
        }

        public List<Movie> GetComingSoonMovies()
        {
            try
            {
                return _movieRepository.GetComingSoonMovies();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coming soon movies");
                return new List<Movie>();
            }
        }

        public List<Movie> GetCurrentlyShowingMoviesWithDetails()
        {
            try
            {
                return _movieRepository.GetCurrentlyShowingMoviesWithDetails();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currently showing movies with details");
                return new List<Movie>();
            }
        }

        public List<Movie> GetComingSoonMoviesWithDetails()
        {
            try
            {
                return _movieRepository.GetComingSoonMoviesWithDetails();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coming soon movies with details");
                return new List<Movie>();
            }
        }

        public MovieShow? GetMovieShowByCinemaRoomId(int cinemaRoomId)
        {
            return _movieRepository.GetMovieShowByCinemaRoomId(cinemaRoomId);
        }
        public IEnumerable<Invoice> GetInvoicesByMovieShow(int movieShowId)
        {
            return _movieRepository.GetInvoicesByMovieShow(movieShowId);
        }

        public List<Movie> GetMoviesBySameGenre(string movieId, int limit = 4)
        {
            try
            {
                var currentMovie = GetById(movieId);
                if (currentMovie == null)
                    return new List<Movie>();

                // Lấy các thể loại của phim hiện tại
                var currentMovieTypeIds = currentMovie.Types.Select(t => t.TypeId).ToList();

                if (!currentMovieTypeIds.Any())
                    return new List<Movie>();

                // Lấy tất cả phim đang chiếu và sắp chiếu
                var allMovies = GetCurrentlyShowingMoviesWithDetails().Concat(GetComingSoonMoviesWithDetails()).ToList();

                // Lọc phim cùng thể loại (loại trừ phim hiện tại)
                var similarMovies = allMovies
                    .Where(m => m.MovieId != movieId &&
                               m.Types.Any(t => currentMovieTypeIds.Contains(t.TypeId)))
                    .OrderByDescending(m => m.Types.Count(t => currentMovieTypeIds.Contains(t.TypeId))) // Ưu tiên phim có nhiều thể loại trùng
                    .ThenBy(m => m.MovieNameEnglish) // Sắp xếp theo tên
                    .Take(limit)
                    .ToList();

                return similarMovies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting movies by same genre for movie: {MovieId}", movieId);
                return new List<Movie>();
            }
        }
    }
}
