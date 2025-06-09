using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

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
                .Include(m => m.MovieShows)
                    .ThenInclude(ms => ms.ShowDate)
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
                var existingMovie = _context.Movies.FirstOrDefault(m => m.MovieId == movie.MovieId);
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
                    existingMovie.CinemaRoomId = movie.CinemaRoomId;
                    existingMovie.Content = movie.Content;
                    existingMovie.TrailerUrl = movie.TrailerUrl;
                    existingMovie.LargeImage = movie.LargeImage;
                    existingMovie.SmallImage = movie.SmallImage;
                    existingMovie.Types = movie.Types;
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
                movie.Types?.Clear();
                movie.MovieShows?.Clear();
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
                _context.SaveChanges();
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

        public async Task<List<ShowDate>> GetShowDatesAsync()
        {
            return await _context.ShowDates.ToListAsync();
        }

        public async Task<List<Models.Type>> GetTypesAsync()
        {
            return await _context.Types.ToListAsync();
        }

        public List<Schedule> GetSchedulesByIds(List<int> ids)
        {
            return _context.Schedules.Where(s => ids.Contains(s.ScheduleId)).ToList();
        }

        public List<ShowDate> GetShowDatesByIds(List<int> ids)
        {
            return _context.ShowDates.Where(d => ids.Contains(d.ShowDateId)).ToList();
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

        public async Task<List<DateTime>> GetShowDatesAsync(string movieId)
        {
            var dates = await _context.MovieShows
                .Where(ms => ms.MovieId == movieId)
                .Select(ms => ms.ShowDate.ShowDate1.Value.ToDateTime(TimeOnly.MinValue))
                .Distinct()
                .ToListAsync();

            return dates;
        }

        public async Task<List<string>> GetShowTimesAsync(string movieId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var movieShows = await _context.MovieShows
                .Include(ms => ms.ShowDate)
                .Include(ms => ms.Schedule)
                .Where(ms => ms.MovieId == movieId && 
                       ms.ShowDate.ShowDate1 == dateOnly)
                .ToListAsync();

            if (!movieShows.Any())
                return new List<string>();

            return movieShows
                .Select(ms => ms.Schedule.ScheduleTime)
                .Distinct()
                .ToList();
        }

        public void AddMovieShow(MovieShow movieShow)
        {
            try
            {
                _context.MovieShows.Add(movieShow);
                _context.SaveChanges();
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
                _context.SaveChanges();
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
                .Include(ms => ms.ShowDate)
                .Include(ms => ms.Schedule)
                .Include(ms => ms.CinemaRoom)
                .Where(ms => ms.MovieId == movieId)
                .ToList();
        }

        public bool IsScheduleAvailable(int showDateId, int scheduleId, int? cinemaRoomId)
        {
            return !_context.MovieShows
                .Any(ms => ms.ShowDateId == showDateId 
                    && ms.ScheduleId == scheduleId 
                    && ms.CinemaRoomId == cinemaRoomId);
        }

        public List<CinemaRoom> GetAllCinemaRooms()
        {
            return _context.CinemaRooms.ToList();
        }
    }
}
