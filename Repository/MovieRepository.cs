using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Repository
{
    public class MovieRepository : IMovieRepository
    {
        private readonly MovieTheaterContext _context;
        private readonly ILogger<MovieRepository> _logger;

        public MovieRepository(MovieTheaterContext context, ILogger<MovieRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string GenerateMovieId()
        {
            var lastestMovie = _context.Movies
                .OrderByDescending(a => a.MovieId)
                .FirstOrDefault();

            if (lastestMovie == null)
            {
                return "MV001";
            }

            if (int.TryParse(lastestMovie.MovieId.Substring(2, 3), out int number))
            {
                return $"MV{(number + 1):D3}";
            }

            return $"MV{DateTime.Now:yyyyMMddHHmmss}";
        }

        public IEnumerable<Movie> GetAll()
        {
            return _context.Movies
                .Include(m => m.Types)
                .Include(m => m.Versions)
                .Include(m => m.People)
                .OrderBy(comparer => comparer.MovieId)
                .ToList();
        }

        public Movie? GetById(string id)
        {
            return _context.Movies
                .Include(m => m.Types)
                .Include(m => m.Versions)
                .Include(m => m.MovieShows)
                    .ThenInclude(ms => ms.Schedule)
                .Include(m => m.MovieShows)
                    .ThenInclude(ms => ms.Version)
                .Include(m => m.People)
                .FirstOrDefault(m => m.MovieId == id);
        }

        public bool Add(Movie movie)
        {
            try
            {
                if (string.IsNullOrEmpty(movie.MovieId))
                {
                    movie.MovieId = GenerateMovieId();
                }

                _context.Movies.Add(movie);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie: {MovieId}", movie.MovieId);
                return false;
            }
        }

        public bool Update(Movie movie)
        {
            try
            {
                var existingMovie = _context.Movies
                    .Include(m => m.Types)
                    .Include(m => m.Versions)
                    .Include(m => m.People)
                    .FirstOrDefault(m => m.MovieId == movie.MovieId);

                if (existingMovie != null)
                {
                    existingMovie.MovieNameEnglish = movie.MovieNameEnglish;
                    existingMovie.Duration = movie.Duration;
                    existingMovie.FromDate = movie.FromDate;
                    existingMovie.ToDate = movie.ToDate;
                    existingMovie.MovieProductionCompany = movie.MovieProductionCompany;
                    existingMovie.Content = movie.Content;
                    existingMovie.TrailerUrl = movie.TrailerUrl;
                    existingMovie.LargeImage = movie.LargeImage;
                    existingMovie.SmallImage = movie.SmallImage;
                    existingMovie.LogoImage = movie.LogoImage;

                    // Update People collection
                    existingMovie.People.Clear();
                    foreach (var person in movie.People)
                    {
                        var existingPerson = _context.People.Find(person.PersonId);
                        if (existingPerson != null)
                        {
                            existingMovie.People.Add(existingPerson);
                        }
                    }

                    // Clear existing types
                    existingMovie.Types.Clear();

                    // Add new types
                    foreach (var type in movie.Types)
                    {
                        var existingType = _context.Types.Find(type.TypeId);
                        if (existingType != null)
                        {
                            existingMovie.Types.Add(existingType);
                        }
                    }

                    existingMovie.Versions.Clear();
                    foreach (var version in movie.Versions)
                    {
                        var existingVersion = _context.Versions.Find(version.VersionId);
                        if (existingVersion != null)
                        {
                            existingMovie.Versions.Add(existingVersion);
                        }
                    }

                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie: {MovieId}", movie.MovieId);
                return false;
            }
        }

        public void Delete(string id)
        {
            var movie = _context.Movies
                .Include(m => m.Types)
                .Include(m => m.MovieShows)
                .FirstOrDefault(m => m.MovieId == id);

            if (movie != null)
            {
                // Remove all related movie shows
                if (movie.MovieShows != null)
                {
                    _context.MovieShows.RemoveRange(movie.MovieShows);
                }

                // Remove all related types
                if (movie.Types != null)
                {
                    _context.Types.RemoveRange(movie.Types);
                }

                if (movie.Versions != null)
                {
                    _context.Versions.RemoveRange(movie.Versions);
                }

                _context.Movies.Remove(movie);

                try
                {
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting movie: {MovieId}", id);
                    throw;
                }
            }
        }

        public void DeleteMovieShow(int movieShowId)
        {
            var movieShow = _context.MovieShows.Find(movieShowId);
            if (movieShow != null)
            {
                _context.MovieShows.Remove(movieShow);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task<List<Schedule>> GetSchedulesAsync()
        {
            return await _context.Schedules.ToListAsync();
        }

        public async Task<List<Models.Type>> GetTypesAsync()
        {
            return await _context.Types.ToListAsync();
        }

        public List<Schedule> GetSchedulesByIds(List<int> ids)
        {
            return _context.Schedules.Where(s => ids.Contains(s.ScheduleId)).ToList();
        }

        public List<Models.Type> GetTypesByIds(List<int> ids)
        {
            return _context.Types.Where(t => ids.Contains(t.TypeId)).ToList();
        }

        public async Task<List<Movie>> GetAllMoviesAsync()
        {
            // Only return movies that have at least one MovieShow
            return await _context.Movies
                .Where(m => m.MovieShows.Any())
                .GroupBy(m => m.MovieId)
                .Select(g => g.First())
                .ToListAsync();
        }

        public async Task<List<DateOnly>> GetShowDatesAsync(string movieId)
        {
            var dates = await _context.MovieShows
                .Where(ms => ms.MovieId == movieId)
                .Select(ms => ms.ShowDate)
                .Distinct()
                .ToListAsync();

            return dates;
        }

        public async Task<List<string>> GetShowTimesAsync(string movieId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);

            var showTimes = await _context.MovieShows
                .Include(ms => ms.Schedule)
                .Where(ms => ms.MovieId == movieId &&
                       ms.ShowDate == dateOnly &&
                       ms.Schedule != null &&
                       ms.Schedule.ScheduleTime.HasValue)
                .Select(ms => ms.Schedule.ScheduleTime.Value)
                .Distinct()
                .OrderBy(t => t)
                .Select(t => t.ToString("HH:mm"))
                .ToListAsync();

            return showTimes;
        }

        public void AddMovieShow(MovieShow movieShow)
        {
            try
            {
                _context.MovieShows.Add(movieShow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie show: {MovieShowId}", movieShow.MovieShowId);
                throw;
            }
        }

        public void AddMovieShows(List<MovieShow> movieShows)
        {
            try
            {
                _context.MovieShows.AddRange(movieShows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie shows");
                throw;
            }
        }

        public MovieShow? GetMovieShowById(int id)
        {
            return _context.MovieShows
                .Include(ms => ms.Movie)
                .Include(ms => ms.Schedule)
                .Include(ms => ms.CinemaRoom)
                .Include(ms => ms.Version)
                .FirstOrDefault(ms => ms.MovieShowId == id);
        }

        public List<MovieShow> GetMovieShowsByMovieId(string movieId)
        {
            return _context.MovieShows
                .Include(ms => ms.Schedule)
                .Include(ms => ms.CinemaRoom)
                .Include(ms => ms.Version)
                .Where(ms => ms.MovieId == movieId)
                .OrderBy(ms => ms.ShowDate)
                .ThenBy(ms => ms.Schedule.ScheduleTime)
                .ToList();
        }

        public bool IsScheduleAvailable(DateOnly showDate, int scheduleId, int cinemaRoomId, int movieDuration)
        {
            const int CLEANING_TIME_MINUTES = 15;

            var scheduleToAdd = _context.Schedules.Find(scheduleId);
            if (scheduleToAdd == null || !scheduleToAdd.ScheduleTime.HasValue)
            {
                return false;
            }
            var scheduleTimeToAdd = scheduleToAdd.ScheduleTime.Value;

            var existingShowsInRoom = _context.MovieShows
                .Include(ms => ms.Schedule)
                .Include(ms => ms.Movie) // Need this for duration
                .Where(ms => ms.ShowDate == showDate && ms.CinemaRoomId == cinemaRoomId)
                .ToList();

            var proposedStartTime = scheduleTimeToAdd;
            var proposedEndTime = proposedStartTime.AddMinutes(movieDuration + CLEANING_TIME_MINUTES);

            foreach (var existingShow in existingShowsInRoom)
            {
                if (existingShow.Schedule?.ScheduleTime == null || existingShow.Movie?.Duration == null)
                {
                    continue; // Skip invalid records
                }

                var existingShowStartTime = existingShow.Schedule.ScheduleTime.Value;
                var existingShowEndTime = existingShowStartTime.AddMinutes((existingShow.Movie.Duration.Value) + CLEANING_TIME_MINUTES);

                // Check for overlap:
                // A new show is not available if it starts before an existing one ends
                // AND it ends after that existing one starts.
                if (proposedStartTime < existingShowEndTime && proposedEndTime > existingShowStartTime)
                {
                    return false; // Conflict found
                }
            }

            return true; // No conflicts
        }

        public List<CinemaRoom> GetAllCinemaRooms()
        {
            return _context.CinemaRooms.ToList();
        }

        public List<Schedule> GetSchedules()
        {
            return _context.Schedules.ToList();
        }

        public List<Models.Type> GetTypes()
        {
            return _context.Types.ToList();
        }
        public List<MovieShow> GetMovieShow()
        {
            return _context.MovieShows
                .Include(ms => ms.Movie)
                .Include(ms => ms.Schedule)
                .Include(ms => ms.CinemaRoom)
                .Include(ms => ms.Version)
                .ToList();
        }

        public List<DateOnly> GetShowDates(string movieId)
        {
            return _context.MovieShows
                .Where(ms => ms.MovieId == movieId)
                .Select(ms => ms.ShowDate)
                .Distinct()
                .ToList();
        }

        public List<string> GetShowTimes(string movieId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);

            return _context.MovieShows
                .Include(ms => ms.Schedule)
                .Where(ms => ms.MovieId == movieId &&
                       ms.ShowDate == dateOnly &&
                       ms.Schedule != null &&
                        ms.Schedule.ScheduleTime.HasValue)
                .Select(ms => ms.Schedule.ScheduleTime.Value)
                .Distinct()
                .OrderBy(t => t)
                .Select(t => t.ToString("HH:mm"))
                .ToList();
        }

        public async Task<List<Schedule>> GetAvailableSchedulesAsync(DateOnly showDate, int cinemaRoomId)
        {
            // Get all schedules
            var allSchedules = await _context.Schedules.OrderBy(s => s.ScheduleTime).ToListAsync();

            // Get booked schedules for this room and date
            var bookedScheduleIds = await _context.MovieShows
                .Where(ms => ms.ShowDate == showDate && ms.CinemaRoomId == cinemaRoomId)
                .Select(ms => ms.ScheduleId)
                .ToListAsync();

            // Return schedules that are not booked
            return allSchedules
                .Where(s => !bookedScheduleIds.Contains(s.ScheduleId))
                .ToList();
        }

        public List<MovieShow> GetMovieShowsByRoomAndDate(int cinemaRoomId, DateOnly showDate)
        {
            return _context.MovieShows
                .Include(ms => ms.Schedule)
                .Include(ms => ms.Movie)
                .Include(ms => ms.CinemaRoom)
                .Include(ms => ms.Version)
                .Where(ms => ms.CinemaRoomId == cinemaRoomId && ms.ShowDate == showDate)
                .OrderBy(ms => ms.Schedule.ScheduleTime)
                .ToList();
        }

        /// <summary>
        /// Gets a summary of unique movie names for each date in a given month.
        /// </summary>
        /// <param name="year">The year of the month to query.</param>
        /// <param name="month">The month to query (1-12).</param>
        /// <returns>A dictionary where the key is the date and the value is a list of unique movie names for that date.</returns>
        public Dictionary<DateOnly, List<string>> GetMovieShowSummaryByMonth(int year, int month)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1);

            // Query only the relevant movie shows in the month
            var query = _context.MovieShows
                .Where(ms => ms.ShowDate >= startDate && ms.ShowDate < endDate)
                .Select(ms => new { ms.ShowDate, ms.Movie.MovieNameEnglish })
                .AsEnumerable() // Grouping in-memory for EF Core limitations with DateOnly
                .GroupBy(x => x.ShowDate)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.MovieNameEnglish?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList()
                );

            return query;
        }

        public List<Models.Version> GetAllVersions()
        {
            return _context.Versions.ToList();
        }

        public Models.Version? GetVersionById(int versionId)
        {
            return _context.Versions.FirstOrDefault(v => v.VersionId == versionId);
        }

        public IEnumerable<MovieShow> GetSelectMovieShow(DateOnly today)
        {
            return (IEnumerable<MovieShow>)_context.MovieShows
            .Where(ms => ms.ShowDate >= today)
            .Include(ms => ms.Movie)
            .Include(ms => ms.Schedule)
            .Include(ms => ms.Version)
            .ToList()
            .GroupBy(ms => ms.Movie)
            .Where(g => g.Key != null)
            .Select(g => new MovieShowtimeInfo
            {
                MovieId = g.Key.MovieId,
                MovieName = g.Key.MovieNameEnglish ?? "Unknown",
                PosterUrl = g.Key.LargeImage ?? g.Key.SmallImage ?? "/images/default-movie.png",
                VersionShowtimes = g.Where(ms => ms.Schedule != null && ms.Version != null)
                                .GroupBy(ms => new { ms.VersionId, ms.Version.VersionName })
                                .Select(versionGroup => new VersionShowtimeInfo
                                {
                                    VersionId = versionGroup.Key.VersionId,
                                    VersionName = versionGroup.Key.VersionName,
                                    Showtimes = versionGroup
                                        .Where(ms => ms.ShowDate >= today)
                                        .Select(ms => ms.Schedule.ScheduleTime.HasValue ? ms.Schedule.ScheduleTime.Value.ToString("HH:mm") : null)
                                        .Where(t => !string.IsNullOrEmpty(t))
                                        .OrderBy(t => t)
                                        .ToList()
                                })
                                .Where(v => v.Showtimes.Any())
                                .OrderBy(v => v.VersionName)
                                .ToList()
            })
        .Where(m => m.VersionShowtimes.Any())
        .ToList();
        }

        public IEnumerable<MovieShow> GetSelectDates(DateOnly today, string movieId)
        {
            return (IEnumerable<MovieShow>)_context.MovieShows
            .Where(ms => ms.ShowDate >= today)
            .Select(ms => ms.ShowDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        }

        // New methods for categorizing movies
        public List<Movie> GetCurrentlyShowingMovies()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // Get movies that have at least one MovieShow with ShowDate >= today
            return _context.Movies
                .Where(m => m.MovieShows.Any(ms => ms.ShowDate >= today))
                .Distinct()
                .ToList();
        }

        public List<Movie> GetComingSoonMovies()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // Get movies that have no MovieShow with ShowDate >= today
            return _context.Movies
                .Where(m => !m.MovieShows.Any(ms => ms.ShowDate >= today))
                .Distinct()
                .ToList();
        }

        public List<Movie> GetCurrentlyShowingMoviesWithDetails()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // Get movies with all related data that have at least one MovieShow with ShowDate >= today
            return _context.Movies
                .Include(m => m.MovieShows)
                .Include(m => m.People)
                .Include(m => m.Types)
                .Include(m => m.Versions)
                .Where(m => m.MovieShows.Any(ms => ms.ShowDate >= today))
                .Distinct()
                .ToList();
        }

        public List<Movie> GetComingSoonMoviesWithDetails()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // Get movies with all related data that have no MovieShow with ShowDate >= today
            return _context.Movies
                .Include(m => m.MovieShows)
                .Include(m => m.People)
                .Include(m => m.Types)
                .Include(m => m.Versions)
                .Where(m => !m.MovieShows.Any(ms => ms.ShowDate >= today))
                .Distinct()
                .ToList();
        }
    }
}
