using MovieTheater.Models;
using Microsoft.EntityFrameworkCore;
using MovieTheater.ViewModels;

namespace MovieTheater.Repository
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly MovieTheaterContext _context;

        public ScheduleRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public List<Schedule> GetAllScheduleTimes()
        {
            return _context.Schedules
                .OrderBy(s => s.ScheduleTime)
                .ToList();
        }
        

        public AvailableSchedulesViewModel GetAvailableScheduleTimes(int cinemaRoomId, DateOnly showDate, int movieDurationMinutes, int cleaningTimeMinutes)
        {
            var existingShows = _context.MovieShows
                                        .Include(ms => ms.Movie)
                                        .Include(ms => ms.Schedule)
                                        .Where(ms => ms.CinemaRoomId == cinemaRoomId && ms.ShowDate == showDate)
                                        .ToList();

            var lastEndTime = new TimeSpan(8, 30, 0);
            bool hasExistingShows = existingShows.Any();

            if (hasExistingShows)
            {
                var lastShow = existingShows
                    .Select(s => new
                    {
                        Show = s,
                        EndTime = (s.Schedule.ScheduleTime.HasValue && s.Movie?.Duration != null)
                            ? s.Schedule.ScheduleTime.Value.ToTimeSpan().Add(TimeSpan.FromMinutes(s.Movie.Duration.Value + cleaningTimeMinutes))
                            : TimeSpan.Zero
                    })
                    .Where(s => s.EndTime != TimeSpan.Zero)
                    .OrderByDescending(s => s.EndTime)
                    .FirstOrDefault();

                if (lastShow != null)
                {
                    lastEndTime = lastShow.EndTime;
                }
            }

            var allSchedules = _context.Schedules.ToList();
            var availableSchedules = allSchedules
                .Where(s => s.ScheduleTime.HasValue && s.ScheduleTime.Value.ToTimeSpan() >= lastEndTime)
                .OrderBy(s => s.ScheduleTime)
                .ToList();

            return new AvailableSchedulesViewModel
            {
                Schedules = availableSchedules,
                LastShowEndTime = lastEndTime,
                HasExistingShows = hasExistingShows
            };
        }
    }
}