using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;
using MovieTheater.Models;

namespace MovieTheater.Hubs
{
    public class HoldInfo
    {
        public string AccountId { get; set; }
        public DateTime HoldTime { get; set; }
    }

    public class SeatHub : Hub
    {
        // movieShowId -> (seatId -> HoldInfo)
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, HoldInfo>> _heldSeats = new();
        private const int HoldMinutes = 5;
        private readonly MovieTheaterContext _context;

        public SeatHub(MovieTheaterContext context)
        {
            _context = context;
        }

        public async Task JoinShowtime(int movieShowId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, movieShowId.ToString());

            if (_heldSeats.TryGetValue(movieShowId, out var seatsForShow))
            {
                var now = DateTime.UtcNow;
                var heldSeatIds = new List<int>();
                foreach (var kv in seatsForShow)
                {
                    var seat = _context.Seats.FirstOrDefault(s => s.SeatId == kv.Key);
                    if (seat != null && seat.SeatStatusId != 1 && (now - kv.Value.HoldTime).TotalMinutes <= HoldMinutes)
                    {
                        heldSeatIds.Add(kv.Key);
                    }
                    else
                    {
                        // Nếu ghế đã available hoặc hết thời gian hold, xóa khỏi hold
                        seatsForShow.TryRemove(kv.Key, out _);
                    }
                }
                await Clients.Caller.SendAsync("HeldSeats", heldSeatIds);
            }
            else
            {
                await Clients.Caller.SendAsync("HeldSeats", new List<int>());
            }
        }

        public async Task SelectSeat(int movieShowId, int seatId)
        {
            var accountId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId)) return;

            var seatsForShow = _heldSeats.GetOrAdd(movieShowId, _ => new());
            var now = DateTime.UtcNow;
            // Chỉ hold nếu ghế chưa ai giữ hoặc đã hết hạn hold
            if (!seatsForShow.TryGetValue(seatId, out var holdInfo) || (now - holdInfo.HoldTime).TotalMinutes > HoldMinutes)
            {
                seatsForShow[seatId] = new HoldInfo { AccountId = accountId, HoldTime = now };
                await Clients.Group(movieShowId.ToString()).SendAsync("SeatSelected", seatId);
            }
        }

        public async Task DeselectSeat(int movieShowId, int seatId)
        {
            var accountId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId)) return;

            if (_heldSeats.TryGetValue(movieShowId, out var seats))
            {
                if (seats.TryGetValue(seatId, out var holdInfo) && holdInfo.AccountId == accountId)
                {
                    seats.TryRemove(seatId, out _);
                    await Clients.Group(movieShowId.ToString()).SendAsync("SeatDeselected", seatId);
                }
            }
        }

        // KHÔNG release ghế trong OnDisconnectedAsync nữa, chỉ giữ logic thông báo SeatsReleased nếu cần
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Không release ghế ở đây nữa
            await base.OnDisconnectedAsync(exception);
        }

        // Thông báo realtime khi trạng thái ghế thay đổi
        public async Task NotifySeatStatusChanged(int movieShowId, int seatId, int newStatusId)
        {
            // Xóa trạng thái hold của seatId này nếu có
            if (_heldSeats.TryGetValue(movieShowId, out var seatsForShow))
            {
                seatsForShow.TryRemove(seatId, out _);
            }
            await Clients.Group(movieShowId.ToString()).SendAsync("SeatStatusChanged", seatId, newStatusId);
        }
    }
}
