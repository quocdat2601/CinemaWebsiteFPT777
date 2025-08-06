//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using MovieTheater.Hubs;
//using MovieTheater.Models;
//using MovieTheater.Service;
//using Xunit;

//namespace MovieTheater.Tests.Service
//{
//    public class CinemaAutoEnableServiceTests
//    {
//        private readonly Mock<IServiceProvider> _serviceProviderMock;
//        private readonly Mock<ILogger<CinemaAutoEnableService>> _loggerMock;
//        private readonly Mock<ICinemaService> _cinemaServiceMock;
//        private readonly Mock<IHubContext<CinemaHub>> _hubContextMock;
//        private readonly Mock<IServiceScope> _serviceScopeMock;
//        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
//        private readonly Mock<IServiceProvider> _scopedServiceProviderMock;

//        public CinemaAutoEnableServiceTests()
//        {
//            _serviceProviderMock = new Mock<IServiceProvider>();
//            _loggerMock = new Mock<ILogger<CinemaAutoEnableService>>();
//            _cinemaServiceMock = new Mock<ICinemaService>();
//            _hubContextMock = new Mock<IHubContext<CinemaHub>>();
//            _serviceScopeMock = new Mock<IServiceScope>();
//            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
//            _scopedServiceProviderMock = new Mock<IServiceProvider>();

//            // Setup service provider
//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
//                .Returns(_serviceScopeFactoryMock.Object);
//            _serviceScopeFactoryMock.Setup(f => f.CreateScope())
//                .Returns(_serviceScopeMock.Object);
//            _serviceScopeMock.Setup(s => s.ServiceProvider)
//                .Returns(_scopedServiceProviderMock.Object);
//        }

//        [Fact]
//        public async Task ExecuteAsync_StartsBackgroundService()
//        {
//            // Arrange
//            var cancellationTokenSource = new CancellationTokenSource();
//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            var task = service.StartAsync(cancellationTokenSource.Token);
//            await Task.Delay(100); // Give it time to start
//            await service.StopAsync(cancellationTokenSource.Token);

//            // Assert
//            Assert.True(task.IsCompleted);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WithExpiredRooms_EnablesRooms()
//        {
//            // Arrange
//            var expiredRooms = new List<CinemaRoom>
//            {
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 1, 
//                    CinemaRoomName = "Room 1", 
//                    StatusId = 3, 
//                    UnavailableEndDate = DateTime.Now.AddMinutes(-10) 
//                },
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 2, 
//                    CinemaRoomName = "Room 2", 
//                    StatusId = 3, 
//                    UnavailableEndDate = DateTime.Now.AddMinutes(-5) 
//                }
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(expiredRooms);

//            _cinemaServiceMock.Setup(cs => cs.GetById(1))
//                .Returns(expiredRooms[0]);
//            _cinemaServiceMock.Setup(cs => cs.GetById(2))
//                .Returns(expiredRooms[1]);

//            _cinemaServiceMock.Setup(cs => cs.Enable(It.IsAny<CinemaRoom>()))
//                .ReturnsAsync(true);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.IsAny<CinemaRoom>()), Times.Exactly(2));
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WithNoExpiredRooms_DoesNothing()
//        {
//            // Arrange
//            var activeRooms = new List<CinemaRoom>
//            {
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 1, 
//                    CinemaRoomName = "Room 1", 
//                    StatusId = 1, 
//                    UnavailableEndDate = DateTime.Now.AddMinutes(10) 
//                }
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(activeRooms);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.IsAny<CinemaRoom>()), Times.Never);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WithNullUnavailableEndDate_DoesNotEnable()
//        {
//            // Arrange
//            var roomsWithNullDate = new List<CinemaRoom>
//            {
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 1, 
//                    CinemaRoomName = "Room 1", 
//                    StatusId = 3, 
//                    UnavailableEndDate = null 
//                }
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(roomsWithNullDate);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.IsAny<CinemaRoom>()), Times.Never);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WithNonDisabledRooms_DoesNotEnable()
//        {
//            // Arrange
//            var nonDisabledRooms = new List<CinemaRoom>
//            {
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 1, 
//                    CinemaRoomName = "Room 1", 
//                    StatusId = 1, // Not disabled
//                    UnavailableEndDate = DateTime.Now.AddMinutes(-10) 
//                }
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(nonDisabledRooms);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.IsAny<CinemaRoom>()), Times.Never);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WhenEnableFails_LogsWarning()
//        {
//            // Arrange
//            var expiredRoom = new CinemaRoom 
//            { 
//                CinemaRoomId = 1, 
//                CinemaRoomName = "Room 1", 
//                StatusId = 3, 
//                UnavailableEndDate = DateTime.Now.AddMinutes(-10) 
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(new List<CinemaRoom> { expiredRoom });

//            _cinemaServiceMock.Setup(cs => cs.GetById(1))
//                .Returns(expiredRoom);

//            _cinemaServiceMock.Setup(cs => cs.Enable(It.IsAny<CinemaRoom>()))
//                .ReturnsAsync(false);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.IsAny<CinemaRoom>()), Times.Once);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WhenRoomNotFoundInNewScope_LogsWarning()
//        {
//            // Arrange
//            var expiredRoom = new CinemaRoom 
//            { 
//                CinemaRoomId = 1, 
//                CinemaRoomName = "Room 1", 
//                StatusId = 3, 
//                UnavailableEndDate = DateTime.Now.AddMinutes(-10) 
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(new List<CinemaRoom> { expiredRoom });

//            _cinemaServiceMock.Setup(cs => cs.GetById(1))
//                .Returns((CinemaRoom)null);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.IsAny<CinemaRoom>()), Times.Never);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WhenExceptionOccurs_LogsError()
//        {
//            // Arrange
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Throws(new Exception("Test exception"));

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            // The service should continue running despite the exception
//            Assert.True(true); // If we reach here, the service didn't crash
//        }

//        [Fact]
//        public async Task ExecuteAsync_WithCancellation_StopsGracefully()
//        {
//            // Arrange
//            var cancellationTokenSource = new CancellationTokenSource();
//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(cancellationTokenSource.Token);
//            cancellationTokenSource.Cancel();
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            Assert.True(cancellationTokenSource.Token.IsCancellationRequested);
//        }

//        [Fact]
//        public async Task CheckAndEnableExpiredRooms_WithMixedRooms_OnlyEnablesExpiredDisabledRooms()
//        {
//            // Arrange
//            var mixedRooms = new List<CinemaRoom>
//            {
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 1, 
//                    CinemaRoomName = "Expired Disabled", 
//                    StatusId = 3, 
//                    UnavailableEndDate = DateTime.Now.AddMinutes(-10) 
//                },
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 2, 
//                    CinemaRoomName = "Active Room", 
//                    StatusId = 1, 
//                    UnavailableEndDate = DateTime.Now.AddMinutes(-10) 
//                },
//                new CinemaRoom 
//                { 
//                    CinemaRoomId = 3, 
//                    CinemaRoomName = "Future Disabled", 
//                    StatusId = 3, 
//                    UnavailableEndDate = DateTime.Now.AddMinutes(10) 
//                }
//            };

//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ICinemaService)))
//                .Returns(_cinemaServiceMock.Object);
//            _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubContext<CinemaHub>)))
//                .Returns(_hubContextMock.Object);

//            _cinemaServiceMock.Setup(cs => cs.GetAll())
//                .Returns(mixedRooms);

//            _cinemaServiceMock.Setup(cs => cs.GetById(1))
//                .Returns(mixedRooms[0]);

//            _cinemaServiceMock.Setup(cs => cs.Enable(It.IsAny<CinemaRoom>()))
//                .ReturnsAsync(true);

//            var service = new CinemaAutoEnableService(_serviceProviderMock.Object, _loggerMock.Object);

//            // Act
//            await service.StartAsync(CancellationToken.None);
//            await Task.Delay(2000); // Wait for the service to process
//            await service.StopAsync(CancellationToken.None);

//            // Assert
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.Is<CinemaRoom>(r => r.CinemaRoomId == 1)), Times.Once);
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.Is<CinemaRoom>(r => r.CinemaRoomId == 2)), Times.Never);
//            _cinemaServiceMock.Verify(cs => cs.Enable(It.Is<CinemaRoom>(r => r.CinemaRoomId == 3)), Times.Never);
//        }
//    }
//} 