using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MovieTheater.Hubs
{
    public class SeatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, string>> _heldSeats = new();

        public async Task JoinShowtime(string movieId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieId);

            // Lấy danh sách seatId đang bị hold cho movieId này
            if (_heldSeats.TryGetValue(movieId, out var seatsForMovie))
            {
                var heldSeatIds = seatsForMovie.Keys.ToList();
                await Clients.Caller.SendAsync("HeldSeats", heldSeatIds);
            }
            else
            {
                await Clients.Caller.SendAsync("HeldSeats", new List<int>());
            }
        }

        public async Task SelectSeat(string movieId, int seatId)
        {
            var connectionId = Context.ConnectionId;
            var seatsForMovie = _heldSeats.GetOrAdd(movieId, _ => new());
            if (seatsForMovie.TryAdd(seatId, connectionId))
            {
                await Clients.Group(movieId).SendAsync("SeatSelected", seatId);
            }
        }

        public async Task DeselectSeat(string movieId, int seatId)
        {
            if (_heldSeats.TryGetValue(movieId, out var seats))
            {
                if (seats.TryRemove(seatId, out _))
                {
                    await Clients.Group(movieId).SendAsync("SeatDeselected", seatId);
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            foreach (var (movieId, seatDict) in _heldSeats)
            {
                var released = seatDict.Where(s => s.Value == connectionId).Select(s => s.Key).ToList();
                foreach (var seatId in released)
                {
                    seatDict.TryRemove(seatId, out _);
                }
                if (released.Any())
                {
                    await Clients.Group(movieId).SendAsync("SeatsReleased", released);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
