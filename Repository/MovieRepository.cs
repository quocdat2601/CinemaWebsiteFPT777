using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Repository
{
    public class MovieRepository : IMovieRepository
    {
        private readonly MovieTheaterContext _context;
        public MovieRepository(MovieTheaterContext context)
        {
            _context = context;
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
                .Include(m => m.Schedules)
                .Include(m => m.ShowDates)
                .Include(m => m.Types)
                .FirstOrDefault(m => m.MovieId == id);
        }


        public void Add(Movie movie)
        {
            if (string.IsNullOrEmpty(movie.MovieId))
            {
                movie.MovieId = GenerateMovieId();
            }
            _context.Movies.Add(movie);
        }

        public void Update(Movie movie)
        {
            var existingMovie = _context.Movies.FirstOrDefault(m => m.MovieId == movie.MovieId);
            if (existingMovie != null)
            {
                existingMovie.MovieNameEnglish = movie.MovieNameEnglish;
                existingMovie.Duration = movie.Duration;
                existingMovie.Types = movie.Types; 

            }
        }
        public void Delete(string id)
        {
            var movie = _context.Movies
                .Include(m => m.Schedules)
                .Include(m => m.Types)
                .Include(m => m.ShowDates)
                .FirstOrDefault(m => m.MovieId == id);
                
            if (movie != null)
            {
                // Clear all relationships
                movie.Schedules?.Clear();
                movie.Types?.Clear();
                movie.ShowDates?.Clear();
                
                // Remove the movie
                _context.Movies.Remove(movie);
                
                // Save changes immediately
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

        //LẤY SHOWDATE DỰA TRÊN MOVIEID
        public async Task<List<DateTime>> GetShowDatesAsync(string movieId)
        {
            var dates = await _context.Movies
                .Where(m => m.MovieId == movieId)
                .SelectMany(m => m.ShowDates)
                .Where(sd => sd.ShowDate1.HasValue)
                .Select(sd => sd.ShowDate1.Value.ToDateTime(TimeOnly.MinValue)) // Convert DateOnly → DateTime
                .Distinct()
                .ToListAsync();

            return dates;
        }


        //LẤY SHOWTIME DỰA TRÊN MOVIEID
        public async Task<List<string>> GetShowTimesAsync(string movieId, DateTime date)
        {
            var movie = await _context.Movies
                .Include(m => m.Schedules)
                .Include(m => m.ShowDates)
                .FirstOrDefaultAsync(m => m.MovieId == movieId);

            if (movie == null)
                return new List<string>();

            // Kiểm tra xem ngày đó có trong ShowDates không
            var isValidDate = movie.ShowDates.Any(sd =>
                sd.ShowDate1.HasValue &&
                sd.ShowDate1.Value.ToDateTime(TimeOnly.MinValue).Date == date.Date);

            if (!isValidDate)
                return new List<string>();

            // Trả về danh sách giờ chiếu
            return movie.Schedules
                .Select(s => s.ScheduleTime)
                .Distinct()
                .ToList();
        }

    }
}
