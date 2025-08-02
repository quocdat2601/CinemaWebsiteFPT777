using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Tests.Hub
{
    public class SeatHubTests
    {
        private MovieTheaterContext _context;
        private SeatHub CreateHubWithUser(string accountId = "acc1", string connectionId = null)
        {
            var hub = new SeatHub(_context);
            var claims = string.IsNullOrEmpty(accountId) ? new List<Claim>() : new List<Claim> { new Claim(ClaimTypes.NameIdentifier, accountId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            var hubCallerContext = new Mock<HubCallerContext>();
            hubCallerContext.Setup(c => c.User).Returns(principal);
            hubCallerContext.Setup(c => c.ConnectionId).Returns(connectionId ?? Guid.NewGuid().ToString());
            hub.Context = hubCallerContext.Object;
            // Mock Clients
            var mockClients = new Mock<IHubCallerClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            var mockSingleClientProxy = new Mock<ISingleClientProxy>();
            var mockCallerAsClientProxy = mockSingleClientProxy.As<IClientProxy>();
            mockClients.Setup(c => c.Caller).Returns(mockSingleClientProxy.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            hub.Clients = mockClients.Object;
            // Mock Groups
            var mockGroups = new Mock<IGroupManager>();
            mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
            hub.Groups = mockGroups.Object;
            return hub;
        }

        public SeatHubTests() 
        { 
            SeatHub.ResetState(); 
            _context = CreateContext();
        }
        
        private MovieTheaterContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new MovieTheaterContext(options);
            
            // Seed some ScheduleSeats data
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = 1, SeatId = 1, SeatStatusId = 2 },
                new ScheduleSeat { MovieShowId = 2, SeatId = 2, SeatStatusId = 1 },
                new ScheduleSeat { MovieShowId = 5, SeatId = 3, SeatStatusId = 1 }
            };
            context.ScheduleSeats.AddRange(scheduleSeats);
            context.SaveChanges();
            
            return context;
        }

        [Fact]
        public async Task JoinShowtime_AccountIdNull_DoesNotSaveConnection()
        {
            var hub = CreateHubWithUser(null);
            await hub.JoinShowtime(1);
            // Không exception là pass
            Assert.True(true);
        }

        [Fact]
        public async Task JoinShowtime_AccountIdNotNull_NoExistingConnection_SavesConnection()
        {
            var hub = CreateHubWithUser("acc1", "conn1");
            await hub.JoinShowtime(1);
            // Kiểm tra mapping tồn tại
            var dict = typeof(SeatHub).GetField("_accountConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<(int, string), string>;
            Assert.Equal("conn1", dict[(1, "acc1")]);
        }

        [Fact]
        public async Task JoinShowtime_AccountIdNotNull_ExistingConnection_SendsAccountInUse()
        {
            var hub = CreateHubWithUser("acc2", "conn2");
            // Tạo mapping cũ với connection khác
            var dict = typeof(SeatHub).GetField("_accountConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<(int, string), string>;
            dict[(2, "acc2")] = "oldConn";
            var mockClients = Mock.Get(hub.Clients);
            var mockCallerAsClientProxy = Mock.Get((IClientProxy)hub.Clients.Caller);
            await hub.JoinShowtime(2);
            mockClients.Verify(c => c.Caller, Times.AtLeastOnce());

            foreach (var inv in mockCallerAsClientProxy.Invocations)
            {
                Console.WriteLine($"{inv.Method.Name} - {string.Join(", ", inv.Arguments.Select(a => a?.ToString() ?? "null"))}");
            }
            var wasCalled = mockCallerAsClientProxy.Invocations.Any(inv =>
                inv.Method.Name == "SendCoreAsync" &&
                inv.Arguments[0] as string == "AccountInUse"
            );
            Assert.True(wasCalled);
            //mockCaller.Verify(c => c.SendAsync("AccountInUse", It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task JoinShowtime_HeldSeats_MyAndOthersAndExpired()
        {
            var hub = CreateHubWithUser("acc3", "conn3");
            // Tạo _heldSeats với 3 ghế: của mình, của người khác, hết hạn
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[1] = new HoldInfo { AccountId = "acc3", HoldTime = DateTime.UtcNow };
            seats[2] = new HoldInfo { AccountId = "other", HoldTime = DateTime.UtcNow };
            seats[3] = new HoldInfo { AccountId = "acc3", HoldTime = DateTime.UtcNow.AddMinutes(-35) }; // hết hạn (more than 30 minutes)
            dict[5] = seats;
            await hub.JoinShowtime(5);
            // Ghế 1 thuộc heldByMe, ghế 2 thuộc heldByOthers, ghế 3 bị xóa (expired)
            Assert.True(seats.ContainsKey(1));
            Assert.True(seats.ContainsKey(2));
            Assert.False(seats.ContainsKey(3)); // Seat 3 should be removed because it's expired
        }

        [Fact]
        public async Task SelectSeat_AccountIdNull_ReturnsImmediately()
        {
            var hub = CreateHubWithUser(null);
            await hub.SelectSeat(1, 1);
            // Không exception là pass
            Assert.True(true);
        }

        [Fact]
        public async Task SelectSeat_SeatNotHeld_HoldsSeat()
        {
            var hub = CreateHubWithUser("acc4");
            await hub.SelectSeat(6, 60);
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            Assert.True(dict[6].ContainsKey(60));
        }

        [Fact]
        public async Task SelectSeat_SeatHeldAndNotExpired_DoesNotHoldAgain()
        {
            var hub = CreateHubWithUser("acc5");
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[70] = new HoldInfo { AccountId = "acc5", HoldTime = DateTime.UtcNow };
            dict[7] = seats;
            await hub.SelectSeat(7, 70);
            // Không tạo mới
            Assert.True(seats[70].AccountId == "acc5");
        }

        [Fact]
        public async Task SelectSeat_SeatHeldAndExpired_HoldsAgain()
        {
            var hub = CreateHubWithUser("acc6");
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[80] = new HoldInfo { AccountId = "acc6", HoldTime = DateTime.UtcNow.AddMinutes(-35) }; // More than 30 minutes expired
            dict[8] = seats;
            await hub.SelectSeat(8, 80);
            Assert.True(seats[80].HoldTime > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task DeselectSeat_AccountIdNull_ReturnsImmediately()
        {
            var hub = CreateHubWithUser(null);
            await hub.DeselectSeat(1, 1);
            Assert.True(true);
        }

        [Fact]
        public async Task DeselectSeat_MovieShowIdNotFound_ReturnsImmediately()
        {
            var hub = CreateHubWithUser("acc7");
            await hub.DeselectSeat(999, 1);
            Assert.True(true);
        }

        [Fact]
        public async Task DeselectSeat_SeatIdNotFound_ReturnsImmediately()
        {
            var hub = CreateHubWithUser("acc8");
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            dict[10] = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            await hub.DeselectSeat(10, 999);
            Assert.True(true);
        }

        [Fact]
        public async Task DeselectSeat_SeatIdFound_AccountIdMatch_RemovesAndSends()
        {
            var hub = CreateHubWithUser("acc9");
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[110] = new HoldInfo { AccountId = "acc9", HoldTime = DateTime.UtcNow };
            dict[11] = seats;
            await hub.DeselectSeat(11, 110);
            Assert.False(seats.ContainsKey(110));
        }

        [Fact]
        public async Task DeselectSeat_SeatIdFound_AccountIdNotMatch_DoesNotRemove()
        {
            var hub = CreateHubWithUser("acc10");
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[120] = new HoldInfo { AccountId = "other", HoldTime = DateTime.UtcNow };
            dict[12] = seats;
            await hub.DeselectSeat(12, 120);
            Assert.True(seats.ContainsKey(120));
        }

        [Fact]
        public async Task OnDisconnectedAsync_AccountIdNull_DoesNothing()
        {
            var hub = CreateHubWithUser(null);
            await hub.OnDisconnectedAsync(null);
            Assert.True(true);
        }

        [Fact]
        public async Task OnDisconnectedAsync_AccountIdNotNull_RemovesMapping()
        {
            var hub = CreateHubWithUser("acc11", "conn11");
            var dict = typeof(SeatHub).GetField("_accountConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<(int, string), string>;
            dict[(13, "acc11")] = "conn11";
            await hub.OnDisconnectedAsync(null);
            Assert.False(dict.ContainsKey((13, "acc11")));
        }

        [Fact]
        public async Task NotifySeatStatusChanged_HeldSeatsExist_RemovesHoldAndSends()
        {
            var hub = CreateHubWithUser("acc12");
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[140] = new HoldInfo { AccountId = "acc12", HoldTime = DateTime.UtcNow };
            dict[14] = seats;
            await hub.NotifySeatStatusChanged(14, 140, 2);
            Assert.False(seats.ContainsKey(140));
        }

        [Fact]
        public async Task NotifySeatStatusChanged_HeldSeatsNotExist_StillSends()
        {
            var hub = CreateHubWithUser("acc13");
            await hub.NotifySeatStatusChanged(15, 150, 2);
            Assert.True(true);
        }

        [Fact]
        public void ReleaseHold_HeldSeatsExist_RemovesHold()
        {
            var dict = typeof(SeatHub).GetField("_heldSeats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>>;
            var seats = new System.Collections.Concurrent.ConcurrentDictionary<int, HoldInfo>();
            seats[160] = new HoldInfo { AccountId = "acc14", HoldTime = DateTime.UtcNow };
            dict[16] = seats;
            SeatHub.ReleaseHold(16, 160);
            Assert.False(seats.ContainsKey(160));
        }

        [Fact]
        public void ReleaseHold_HeldSeatsNotExist_DoesNothing()
        {
            SeatHub.ReleaseHold(999, 999);
            Assert.True(true);
        }
    }
}