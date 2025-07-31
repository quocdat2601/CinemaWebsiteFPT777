using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Service;

namespace MovieTheater.Service
{
    public class CinemaAutoEnableService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CinemaAutoEnableService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

        public CinemaAutoEnableService(IServiceProvider serviceProvider, ILogger<CinemaAutoEnableService> logger)
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
                    await CheckAndEnableExpiredRooms();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CinemaAutoEnableService");
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
        }

        private async Task CheckAndEnableExpiredRooms()
        {
            using var scope = _serviceProvider.CreateScope();
            var cinemaService = scope.ServiceProvider.GetRequiredService<ICinemaService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CinemaHub>>();

            var now = DateTime.Now;
            var disabledRooms = cinemaService.GetAll()
                .Where(r => r.StatusId == 3 && 
                           r.UnavailableEndDate.HasValue && 
                           r.UnavailableEndDate.Value <= now)
                .ToList();

            foreach (var room in disabledRooms)
            {
                // Create a new scope for each room operation to avoid DbContext concurrency issues
                using var roomScope = _serviceProvider.CreateScope();
                var roomCinemaService = roomScope.ServiceProvider.GetRequiredService<ICinemaService>();
                
                try
                {
                    // Get the room again in the new scope
                    var roomToEnable = roomCinemaService.GetById(room.CinemaRoomId);
                    if (roomToEnable == null)
                    {
                        _logger.LogWarning($"Room {room.CinemaRoomName} (ID: {room.CinemaRoomId}) not found in new scope");
                        continue;
                    }

                    // Enable the room
                    bool success = await roomCinemaService.Enable(roomToEnable);

                    if (success)
                    {
                        _logger.LogInformation($"Auto-enabled cinema room {room.CinemaRoomName} (ID: {room.CinemaRoomId})");
                        
                        // Notify via SignalR
                        await CinemaHub.NotifyRoomEnabled(hubContext, room.CinemaRoomId, room.CinemaRoomName);
                        await CinemaHub.NotifyAdmins(hubContext, 
                            $"Room '{room.CinemaRoomName}' has been automatically enabled after its disable period ended.", 
                            room.CinemaRoomName);
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to auto-enable cinema room {room.CinemaRoomName} (ID: {room.CinemaRoomId})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error auto-enabling cinema room {room.CinemaRoomName} (ID: {room.CinemaRoomId})");
                }
            }
        }
    }
} 