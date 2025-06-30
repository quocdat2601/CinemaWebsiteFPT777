using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace MovieTheater.Hubs
{
    public class SeatHub : Hub
    {
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> _heldSeats = new();

        public async Task JoinShowtime(int movieShowId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieShowId.ToString());

            // Lấy danh sách seatId đang bị hold cho movieShowId này
            if (_heldSeats.TryGetValue(movieShowId, out var seatsForShow))
            {
                var heldSeatIds = seatsForShow.Keys.ToList();
                await Clients.Caller.SendAsync("HeldSeats", heldSeatIds);
            }
            else
            {
                await Clients.Caller.SendAsync("HeldSeats", new List<int>());
            }
        }

        public async Task SelectSeat(int movieShowId, int seatId)
        {
            var connectionId = Context.ConnectionId;
            var seatsForShow = _heldSeats.GetOrAdd(movieShowId, _ => new());
            if (seatsForShow.TryAdd(seatId, connectionId))
            {
                await Clients.Group(movieShowId.ToString()).SendAsync("SeatSelected", seatId);
            }
        }

        public async Task DeselectSeat(int movieShowId, int seatId)
        {
            if (_heldSeats.TryGetValue(movieShowId, out var seats))
            {
                if (seats.TryRemove(seatId, out _))
                {
                    await Clients.Group(movieShowId.ToString()).SendAsync("SeatDeselected", seatId);
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            foreach (var (movieShowId, seatDict) in _heldSeats)
            {
                var released = seatDict.Where(s => s.Value == connectionId).Select(s => s.Key).ToList();
                foreach (var seatId in released)
                {
                    seatDict.TryRemove(seatId, out _);
                }
                if (released.Any())
                {
                    await Clients.Group(movieShowId.ToString()).SendAsync("SeatsReleased", released);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Thông báo realtime khi trạng thái ghế thay đổi
        public async Task NotifySeatStatusChanged(int movieShowId, int seatId, int newStatusId)
        {
            await Clients.Group(movieShowId.ToString()).SendAsync("SeatStatusChanged", seatId, newStatusId);
        }
    }
}
