using Microsoft.AspNetCore.SignalR;
using MovieTheater.Models;
using System.Collections.Concurrent;
using System.Security.Claims;

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

            var accountId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var heldByMe = new List<int>();
            var heldByOthers = new List<int>();

            if (_heldSeats.TryGetValue(movieShowId, out var seatsForShow))
            {
                var now = DateTime.UtcNow;
                foreach (var kv in seatsForShow)
                {
                    if ((now - kv.Value.HoldTime).TotalMinutes <= HoldMinutes)
                    {
                        if (kv.Value.AccountId == accountId)
                            heldByMe.Add(kv.Key);
                        else
                            heldByOthers.Add(kv.Key);
                    }
                    else
                    {
                        seatsForShow.TryRemove(kv.Key, out _);
                    }
                }
            }
            await Clients.Caller.SendAsync("HeldSeats", heldByMe, heldByOthers);
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

        // Static method để xóa trạng thái hold từ bên ngoài (repository/controller)
        public static void ReleaseHold(int movieShowId, int seatId)
        {
            if (_heldSeats.TryGetValue(movieShowId, out var seatsForShow))
            {
                seatsForShow.TryRemove(seatId, out _);
            }
        }
    }
}
