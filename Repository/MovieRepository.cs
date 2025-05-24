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
                return "ACC0001";
            }

            if (int.TryParse(lastestMovie.MovieId.Substring(3), out int number))
            {
                return $"ACC{(number + 1):D4}";
            }

            return $"ACC{DateTime.Now:yyyyMMddHHmmss}";
        }

        public IEnumerable<Movie> GetAll()
        {
            return _context.Movies
                .Include(m => m.Types)
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
            var movie = _context.Movies.FirstOrDefault(m => m.MovieId == id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
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
    }
}
