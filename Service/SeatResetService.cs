using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Service
{
    public class SeatResetService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SeatResetService> _logger;

        public SeatResetService(IServiceProvider serviceProvider, ILogger<SeatResetService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<MovieTheaterContext>();
                        var seatService = scope.ServiceProvider.GetRequiredService<ISeatService>();

                        // Get all past shows that haven't had their seats reset
                        var currentTime = DateTime.Now;
                        var currentDate = DateOnly.FromDateTime(currentTime);
                        var currentTimeString = currentTime.ToString("HH:mm");

                        var pastShows = await context.MovieShows
                            .Include(ms => ms.ShowDate)
                            .Include(ms => ms.Schedule)
                            .Where(ms => ms.ShowDate.ShowDate1 < currentDate || 
                                   (ms.ShowDate.ShowDate1 == currentDate))
                            .Where(ms => context.ScheduleSeats
                                .Where(ss => ss.ScheduleId == ms.ScheduleId)
                                .Any(ss => ss.SeatStatusId == 2)) // Booked status
                            .Select(ms => new { ms.MovieId, ShowDate = ms.ShowDate.ShowDate1.Value.ToDateTime(TimeOnly.MinValue), ms.Schedule.ScheduleTime })
                            .Distinct()
                            .ToListAsync(stoppingToken);

                        // Filter today's shows by time on the client side
                        pastShows = pastShows.Where(show => 
                            show.ShowDate.Date < currentDate.ToDateTime(TimeOnly.MinValue) || 
                            (show.ShowDate.Date == currentDate.ToDateTime(TimeOnly.MinValue) && 
                             show.ScheduleTime.CompareTo(currentTimeString) < 0))
                            .ToList();

                        foreach (var show in pastShows)
                        {
                            await seatService.ResetSeatsAfterShowAsync(
                                show.MovieId,
                                show.ShowDate,
                                show.ScheduleTime
                            );
                            _logger.LogInformation($"Reset seats for movie {show.MovieId} on {show.ShowDate} at {show.ScheduleTime}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while resetting seats");
                }

                // Wait for 1 hour before checking again
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
} 