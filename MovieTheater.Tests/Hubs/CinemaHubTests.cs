//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.SignalR;
//using Moq;
//using MovieTheater.Hubs;
//using Xunit;

//namespace MovieTheater.Tests.Hubs
//{
//    public class CinemaHubTests
//    {
//        private readonly Mock<IHubCallerClients> _mockClients;
//        private readonly Mock<IGroupManager> _mockGroups;
//        private readonly Mock<HubCallerContext> _mockContext;
//        private readonly CinemaHub _hub;

//        public CinemaHubTests()
//        {
//            _mockClients = new Mock<IHubCallerClients>();
//            _mockGroups = new Mock<IGroupManager>();
//            _mockContext = new Mock<HubCallerContext>();

//            _hub = new CinemaHub();
//            _hub.Clients = _mockClients.Object;
//            _hub.Groups = _mockGroups.Object;
//            _hub.Context = _mockContext.Object;

//            // Setup default mock behaviors
//            _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
//                .Returns(Task.CompletedTask);
//            _mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
//                .Returns(Task.CompletedTask);
//        }

//        [Fact]
//        public async Task JoinCinemaRoom_AddsConnectionToGroup()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            var cinemaRoomId = 1;
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

//            // Act
//            await _hub.JoinCinemaRoom(cinemaRoomId);

//            // Assert
//            _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, $"cinema_room_{cinemaRoomId}", default), Times.Once);
//        }

//        [Fact]
//        public async Task JoinCinemaRoom_WithDifferentRoomIds_AddsToCorrectGroups()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

//            // Act
//            await _hub.JoinCinemaRoom(1);
//            await _hub.JoinCinemaRoom(2);

//            // Assert
//            _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, "cinema_room_1", default), Times.Once);
//            _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, "cinema_room_2", default), Times.Once);
//        }

//        [Fact]
//        public async Task LeaveCinemaRoom_RemovesConnectionFromGroup()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            var cinemaRoomId = 1;
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

//            // Act
//            await _hub.LeaveCinemaRoom(cinemaRoomId);

//            // Assert
//            _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, $"cinema_room_{cinemaRoomId}", default), Times.Once);
//        }

//        [Fact]
//        public async Task LeaveCinemaRoom_WithDifferentRoomIds_RemovesFromCorrectGroups()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

//            // Act
//            await _hub.LeaveCinemaRoom(1);
//            await _hub.LeaveCinemaRoom(2);

//            // Assert
//            _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, "cinema_room_1", default), Times.Once);
//            _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, "cinema_room_2", default), Times.Once);
//        }

//        [Fact]
//        public async Task NotifyRoomEnabled_StaticMethod_DoesNotThrowException()
//        {
//            // Arrange
//            var mockHubContext = new Mock<IHubContext<CinemaHub>>();
//            var mockClients = new Mock<IHubClients>();
//            var mockGroup = new Mock<IClientProxy>();
            
//            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
//            mockClients.Setup(c => c.Group("cinema_room_1")).Returns(mockGroup.Object);
//            mockGroup.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);

//            var cinemaRoomId = 1;
//            var roomName = "Test Room";

//            // Act & Assert
//            var exception = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyRoomEnabled(mockHubContext.Object, cinemaRoomId, roomName));
            
//            Assert.Null(exception);
//        }

//        [Fact]
//        public async Task NotifyRoomEnabled_WithDifferentRoomIds_DoesNotThrowException()
//        {
//            // Arrange
//            var mockHubContext = new Mock<IHubContext<CinemaHub>>();
//            var mockClients = new Mock<IHubClients>();
//            var mockGroup1 = new Mock<IClientProxy>();
//            var mockGroup2 = new Mock<IClientProxy>();
            
//            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
//            mockClients.Setup(c => c.Group("cinema_room_1")).Returns(mockGroup1.Object);
//            mockClients.Setup(c => c.Group("cinema_room_2")).Returns(mockGroup2.Object);
//            mockGroup1.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);
//            mockGroup2.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);

//            // Act & Assert
//            var exception1 = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyRoomEnabled(mockHubContext.Object, 1, "Room 1"));
//            var exception2 = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyRoomEnabled(mockHubContext.Object, 2, "Room 2"));
            
//            Assert.Null(exception1);
//            Assert.Null(exception2);
//        }

//        [Fact]
//        public async Task NotifyAdmins_StaticMethod_DoesNotThrowException()
//        {
//            // Arrange
//            var mockHubContext = new Mock<IHubContext<CinemaHub>>();
//            var mockClients = new Mock<IHubClients>();
//            var mockAll = new Mock<IClientProxy>();
            
//            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
//            mockClients.Setup(c => c.All).Returns(mockAll.Object);
//            mockAll.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);

//            var message = "Test notification message";
//            var roomName = "Test Room";

//            // Act & Assert
//            var exception = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyAdmins(mockHubContext.Object, message, roomName));
            
//            Assert.Null(exception);
//        }

//        [Fact]
//        public async Task NotifyAdmins_WithDifferentMessages_DoesNotThrowException()
//        {
//            // Arrange
//            var mockHubContext = new Mock<IHubContext<CinemaHub>>();
//            var mockClients = new Mock<IHubClients>();
//            var mockAll = new Mock<IClientProxy>();
            
//            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
//            mockClients.Setup(c => c.All).Returns(mockAll.Object);
//            mockAll.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);

//            // Act & Assert
//            var exception1 = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyAdmins(mockHubContext.Object, "Message 1", "Room 1"));
//            var exception2 = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyAdmins(mockHubContext.Object, "Message 2", "Room 2"));
            
//            Assert.Null(exception1);
//            Assert.Null(exception2);
//        }

//        [Fact]
//        public async Task JoinCinemaRoom_WhenGroupManagerThrowsException_PropagatesException()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            var cinemaRoomId = 1;
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
//            _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
//                .ThrowsAsync(new Exception("Test exception"));

//            // Act & Assert
//            await Assert.ThrowsAsync<Exception>(() => _hub.JoinCinemaRoom(cinemaRoomId));
//        }

//        [Fact]
//        public async Task LeaveCinemaRoom_WhenGroupManagerThrowsException_PropagatesException()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            var cinemaRoomId = 1;
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
//            _mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
//                .ThrowsAsync(new Exception("Test exception"));

//            // Act & Assert
//            await Assert.ThrowsAsync<Exception>(() => _hub.LeaveCinemaRoom(cinemaRoomId));
//        }

//        [Fact]
//        public async Task NotifyRoomEnabled_WhenHubContextIsNull_ThrowsArgumentNullException()
//        {
//            // Arrange
//            IHubContext<CinemaHub> hubContext = null;
//            var cinemaRoomId = 1;
//            var roomName = "Test Room";

//            // Act & Assert
//            await Assert.ThrowsAsync<ArgumentNullException>(() => 
//                CinemaHub.NotifyRoomEnabled(hubContext, cinemaRoomId, roomName));
//        }

//        [Fact]
//        public async Task NotifyAdmins_WhenHubContextIsNull_ThrowsArgumentNullException()
//        {
//            // Arrange
//            IHubContext<CinemaHub> hubContext = null;
//            var message = "Test message";
//            var roomName = "Test Room";

//            // Act & Assert
//            await Assert.ThrowsAsync<ArgumentNullException>(() => 
//                CinemaHub.NotifyAdmins(hubContext, message, roomName));
//        }

//        [Fact]
//        public async Task JoinCinemaRoom_WithZeroRoomId_AddsToGroupWithZero()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            var cinemaRoomId = 0;
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

//            // Act
//            await _hub.JoinCinemaRoom(cinemaRoomId);

//            // Assert
//            _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, "cinema_room_0", default), Times.Once);
//        }

//        [Fact]
//        public async Task LeaveCinemaRoom_WithNegativeRoomId_RemovesFromGroupWithNegative()
//        {
//            // Arrange
//            var connectionId = "test-connection-id";
//            var cinemaRoomId = -1;
//            _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

//            // Act
//            await _hub.LeaveCinemaRoom(cinemaRoomId);

//            // Assert
//            _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, "cinema_room_-1", default), Times.Once);
//        }

//        [Fact]
//        public async Task NotifyRoomEnabled_WithNullRoomName_DoesNotThrowException()
//        {
//            // Arrange
//            var mockHubContext = new Mock<IHubContext<CinemaHub>>();
//            var mockClients = new Mock<IHubClients>();
//            var mockGroup = new Mock<IClientProxy>();
            
//            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
//            mockClients.Setup(c => c.Group("cinema_room_1")).Returns(mockGroup.Object);
//            mockGroup.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);

//            var cinemaRoomId = 1;
//            string roomName = null;

//            // Act & Assert
//            var exception = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyRoomEnabled(mockHubContext.Object, cinemaRoomId, roomName));
            
//            Assert.Null(exception);
//        }

//        [Fact]
//        public async Task NotifyAdmins_WithNullMessage_DoesNotThrowException()
//        {
//            // Arrange
//            var mockHubContext = new Mock<IHubContext<CinemaHub>>();
//            var mockClients = new Mock<IHubClients>();
//            var mockAll = new Mock<IClientProxy>();
            
//            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
//            mockClients.Setup(c => c.All).Returns(mockAll.Object);
//            mockAll.Setup(g => g.SendAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>()))
//                .Returns(Task.CompletedTask);

//            string message = null;
//            var roomName = "Test Room";

//            // Act & Assert
//            var exception = await Record.ExceptionAsync(() => 
//                CinemaHub.NotifyAdmins(mockHubContext.Object, message, roomName));
            
//            Assert.Null(exception);
//        }
//    }
//} 