using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly MovieTheaterContext _context;

        public ScheduleRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public List<string> GetAllScheduleTimes()
        {
            return _context.Schedules
                .OrderBy(s => s.ScheduleTime)
                .Select(s => s.ScheduleTime)
                .ToList();
        }

        public List<DateTime> GetAllShowDates()
        {
            return _context.ShowDates
                .Where(d => d.ShowDate1.HasValue)
                .Select(d => d.ShowDate1)
                .ToList()
                .Select(d => d.Value.ToDateTime(TimeOnly.MinValue))
                .OrderBy(d => d)
                .ToList();
        }
    }
}