using Microsoft.AspNetCore.SignalR;

namespace MovieTheater.Hubs
{
    public class CinemaHub : Hub
    {
        public async Task JoinCinemaRoom(int cinemaRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"cinema_room_{cinemaRoomId}");
        }

        public async Task LeaveCinemaRoom(int cinemaRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cinema_room_{cinemaRoomId}");
        }

        // Static method to notify when a room is automatically enabled
        public static async Task NotifyRoomEnabled(IHubContext<CinemaHub> hubContext, int cinemaRoomId, string roomName)
        {
            if (hubContext == null)
                throw new ArgumentNullException(nameof(hubContext));
            await hubContext.Clients.Group($"cinema_room_{cinemaRoomId}").SendAsync("RoomAutoEnabled", cinemaRoomId, roomName);
        }

        // Static method to notify all admins about room status changes
        public static async Task NotifyAdmins(IHubContext<CinemaHub> hubContext, string message, string roomName)
        {
            if (hubContext == null)
                throw new ArgumentNullException(nameof(hubContext));
            await hubContext.Clients.All.SendAsync("AdminNotification", message, roomName);
        }
    }
} 