using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

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
                .OrderBy(comparer => comparer.MovieId)
                .ToList();
        }

        public Movie? GetById(string id)
        {
            return _context.Movies
                .Include(m => m.Types)
                .Include(m => m.MovieShows)
                    .ThenInclude(ms => ms.Schedule)
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
                    .FirstOrDefault(m => m.MovieId == movie.MovieId);
                    
                if (existingMovie != null)
                {
                    existingMovie.MovieNameEnglish = movie.MovieNameEnglish;
                    existingMovie.MovieNameVn = movie.MovieNameVn;
                    existingMovie.Actor = movie.Actor;
                    existingMovie.Director = movie.Director;
                    existingMovie.Duration = movie.Duration;
                    existingMovie.Version = movie.Version;
                    existingMovie.FromDate = movie.FromDate;
                    existingMovie.ToDate = movie.ToDate;
                    existingMovie.MovieProductionCompany = movie.MovieProductionCompany;
                    existingMovie.Content = movie.Content;
                    existingMovie.TrailerUrl = movie.TrailerUrl;
                    existingMovie.LargeImage = movie.LargeImage;
                    existingMovie.SmallImage = movie.SmallImage;
                    
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
            return await _context.Movies
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

        public List<MovieShow> GetMovieShowsByMovieId(string movieId)
        {
            return _context.MovieShows
                .Include(ms => ms.Schedule)
                .Include(ms => ms.CinemaRoom)
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
                .Include(ms => ms.CinemaRoom).ToList();
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
                .Where(ms => ms.CinemaRoomId == cinemaRoomId && ms.ShowDate == showDate)
                .OrderBy(ms => ms.Schedule.ScheduleTime)
                .ToList();
        }
    }
}
