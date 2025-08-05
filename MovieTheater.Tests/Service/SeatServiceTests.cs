using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class SeatServiceTests
    {
        private readonly Mock<ISeatRepository> _mockRepository;
        private readonly SeatService _service;

        public SeatServiceTests()
        {
            _mockRepository = new Mock<ISeatRepository>();
            _service = new SeatService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetAllSeatsAsync_ReturnsAllSeats()
        {
            // Arrange
            var expectedSeats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", CinemaRoomId = 1 },
                new Seat { SeatId = 2, SeatName = "A2", CinemaRoomId = 1 }
            };
            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(expectedSeats);

            // Act
            var result = await _service.GetAllSeatsAsync();

            // Assert
            Assert.Equal(expectedSeats, result);
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllSeatsAsync_WhenRepositoryReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            var expectedSeats = new List<Seat>();
            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(expectedSeats);

            // Act
            var result = await _service.GetAllSeatsAsync();

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetSeatsByRoomIdAsync_WithValidRoomId_ReturnsSeats()
        {
            // Arrange
            var roomId = 1;
            var expectedSeats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", CinemaRoomId = roomId },
                new Seat { SeatId = 2, SeatName = "A2", CinemaRoomId = roomId }
            };
            _mockRepository.Setup(r => r.GetByCinemaRoomIdAsync(roomId))
                .ReturnsAsync(expectedSeats);

            // Act
            var result = await _service.GetSeatsByRoomIdAsync(roomId);

            // Assert
            Assert.Equal(expectedSeats, result);
            _mockRepository.Verify(r => r.GetByCinemaRoomIdAsync(roomId), Times.Once);
        }

        [Fact]
        public async Task GetSeatsByRoomIdAsync_WhenRepositoryReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            var roomId = 1;
            var expectedSeats = new List<Seat>();
            _mockRepository.Setup(r => r.GetByCinemaRoomIdAsync(roomId))
                .ReturnsAsync(expectedSeats);

            // Act
            var result = await _service.GetSeatsByRoomIdAsync(roomId);

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetByCinemaRoomIdAsync(roomId), Times.Once);
        }

        [Fact]
        public async Task GetSeatTypesAsync_ReturnsSeatTypes()
        {
            // Arrange
            var expectedSeatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "Standard" },
                new SeatType { SeatTypeId = 2, TypeName = "VIP" }
            };
            _mockRepository.Setup(r => r.GetSeatTypesAsync())
                .ReturnsAsync(expectedSeatTypes);

            // Act
            var result = await _service.GetSeatTypesAsync();

            // Assert
            Assert.Equal(expectedSeatTypes, result);
            _mockRepository.Verify(r => r.GetSeatTypesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetSeatTypesAsync_WhenRepositoryReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            var expectedSeatTypes = new List<SeatType>();
            _mockRepository.Setup(r => r.GetSeatTypesAsync())
                .ReturnsAsync(expectedSeatTypes);

            // Act
            var result = await _service.GetSeatTypesAsync();

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetSeatTypesAsync(), Times.Once);
        }

        [Fact]
        public void AddSeatAsync_WithValidSeat_CallsRepositoryAndSave()
        {
            // Arrange
            var seat = new Seat
            {
                SeatName = "A1",
                CinemaRoomId = 1,
                SeatTypeId = 1
            };

            // Act
            _service.AddSeatAsync(seat);

            // Assert
            _mockRepository.Verify(r => r.Add(seat), Times.Once);
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void UpdateSeatAsync_WithValidSeat_CallsRepositoryAndSave()
        {
            // Arrange
            var seat = new Seat
            {
                SeatId = 1,
                SeatName = "A1",
                CinemaRoomId = 1,
                SeatTypeId = 1
            };

            // Act
            _service.UpdateSeatAsync(seat);

            // Assert
            _mockRepository.Verify(r => r.Update(seat), Times.Once);
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void DeleteSeatAsync_WithValidId_CallsRepositoryAndSave()
        {
            // Arrange
            var seatId = 1;

            // Act
            _service.DeleteSeatAsync(seatId);

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(seatId), Times.Once);
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void Save_CallsRepositorySave()
        {
            // Act
            _service.Save();

            // Assert
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void GetSeatByName_WithValidName_ReturnsSeat()
        {
            // Arrange
            var seatName = "A1";
            var expectedSeat = new Seat
            {
                SeatId = 1,
                SeatName = seatName,
                CinemaRoomId = 1
            };
            _mockRepository.Setup(r => r.GetSeatByName(seatName))
                .Returns(expectedSeat);

            // Act
            var result = _service.GetSeatByName(seatName);

            // Assert
            Assert.Equal(expectedSeat, result);
            _mockRepository.Verify(r => r.GetSeatByName(seatName), Times.Once);
        }

        [Fact]
        public void GetSeatByName_WhenSeatNotFound_ReturnsNull()
        {
            // Arrange
            var seatName = "A1";
            _mockRepository.Setup(r => r.GetSeatByName(seatName))
                .Returns((Seat)null);

            // Act
            var result = _service.GetSeatByName(seatName);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetSeatByName(seatName), Times.Once);
        }

        [Fact]
        public void GetSeatById_WithValidId_ReturnsSeat()
        {
            // Arrange
            var seatId = 1;
            var expectedSeat = new Seat
            {
                SeatId = seatId,
                SeatName = "A1",
                CinemaRoomId = 1
            };
            _mockRepository.Setup(r => r.GetById(seatId))
                .Returns(expectedSeat);

            // Act
            var result = _service.GetSeatById(seatId);

            // Assert
            Assert.Equal(expectedSeat, result);
            _mockRepository.Verify(r => r.GetById(seatId), Times.Once);
        }

        [Fact]
        public void GetSeatById_WithNullId_ReturnsNull()
        {
            // Arrange
            int? seatId = null;
            _mockRepository.Setup(r => r.GetById(seatId))
                .Returns((Seat)null);

            // Act
            var result = _service.GetSeatById(seatId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetById(seatId), Times.Once);
        }

        [Fact]
        public void GetSeatById_WhenSeatNotFound_ReturnsNull()
        {
            // Arrange
            var seatId = 1;
            _mockRepository.Setup(r => r.GetById(seatId))
                .Returns((Seat)null);

            // Act
            var result = _service.GetSeatById(seatId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetById(seatId), Times.Once);
        }

        [Fact]
        public async Task DeleteCoupleSeatBySeatIdsAsync_WithValidIds_CallsRepository()
        {
            // Arrange
            var seatId1 = 1;
            var seatId2 = 2;

            // Act
            await _service.DeleteCoupleSeatBySeatIdsAsync(seatId1, seatId2);

            // Assert
            _mockRepository.Verify(r => r.DeleteCoupleSeatBySeatIdsAsync(seatId1, seatId2), Times.Once);
        }

        [Fact]
        public void GetSeatsWithTypeByIds_WithValidIds_ReturnsSeats()
        {
            // Arrange
            var seatIds = new List<int> { 1, 2, 3 };
            var expectedSeats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1 },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1 },
                new Seat { SeatId = 3, SeatName = "A3", SeatTypeId = 2 }
            };
            _mockRepository.Setup(r => r.GetSeatsWithTypeByIds(seatIds))
                .Returns(expectedSeats);

            // Act
            var result = _service.GetSeatsWithTypeByIds(seatIds);

            // Assert
            Assert.Equal(expectedSeats, result);
            _mockRepository.Verify(r => r.GetSeatsWithTypeByIds(seatIds), Times.Once);
        }

        [Fact]
        public void GetSeatsWithTypeByIds_WithEmptyIds_ReturnsEmpty()
        {
            // Arrange
            var seatIds = new List<int>();
            var expectedSeats = new List<Seat>();
            _mockRepository.Setup(r => r.GetSeatsWithTypeByIds(seatIds))
                .Returns(expectedSeats);

            // Act
            var result = _service.GetSeatsWithTypeByIds(seatIds);

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetSeatsWithTypeByIds(seatIds), Times.Once);
        }

        [Fact]
        public void GetSeatsByNames_WithValidNames_ReturnsSeats()
        {
            // Arrange
            var seatNames = new List<string> { "A1", "A2", "A3" };
            var expectedSeats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" },
                new Seat { SeatId = 3, SeatName = "A3" }
            };
            _mockRepository.Setup(r => r.GetSeatsByNames(seatNames))
                .Returns(expectedSeats);

            // Act
            var result = _service.GetSeatsByNames(seatNames);

            // Assert
            Assert.Equal(expectedSeats, result);
            _mockRepository.Verify(r => r.GetSeatsByNames(seatNames), Times.Once);
        }

        [Fact]
        public void GetSeatsByNames_WithEmptyNames_ReturnsEmpty()
        {
            // Arrange
            var seatNames = new List<string>();
            var expectedSeats = new List<Seat>();
            _mockRepository.Setup(r => r.GetSeatsByNames(seatNames))
                .Returns(expectedSeats);

            // Act
            var result = _service.GetSeatsByNames(seatNames);

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetSeatsByNames(seatNames), Times.Once);
        }

        [Fact]
        public async Task UpdateSeatsStatusToBookedAsync_WithValidIds_CallsRepository()
        {
            // Arrange
            var seatIds = new List<int> { 1, 2, 3 };

            // Act
            await _service.UpdateSeatsStatusToBookedAsync(seatIds);

            // Assert
            _mockRepository.Verify(r => r.UpdateSeatsStatusToBookedAsync(seatIds), Times.Once);
        }

        [Fact]
        public async Task UpdateSeatsStatusToBookedAsync_WithEmptyIds_CallsRepository()
        {
            // Arrange
            var seatIds = new List<int>();

            // Act
            await _service.UpdateSeatsStatusToBookedAsync(seatIds);

            // Assert
            _mockRepository.Verify(r => r.UpdateSeatsStatusToBookedAsync(seatIds), Times.Once);
        }

        [Fact]
        public void GetSeatStatusByName_WithValidName_ReturnsSeatStatus()
        {
            // Arrange
            var statusName = "Available";
            var expectedStatus = new SeatStatus
            {
                SeatStatusId = 1,
                StatusName = statusName
            };
            _mockRepository.Setup(r => r.GetSeatStatusByName(statusName))
                .Returns(expectedStatus);

            // Act
            var result = _service.GetSeatStatusByName(statusName);

            // Assert
            Assert.Equal(expectedStatus, result);
            _mockRepository.Verify(r => r.GetSeatStatusByName(statusName), Times.Once);
        }

        [Fact]
        public void GetSeatStatusByName_WhenStatusNotFound_ReturnsNull()
        {
            // Arrange
            var statusName = "InvalidStatus";
            _mockRepository.Setup(r => r.GetSeatStatusByName(statusName))
                .Returns((SeatStatus)null);

            // Act
            var result = _service.GetSeatStatusByName(statusName);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetSeatStatusByName(statusName), Times.Once);
        }

        [Fact]
        public void GetSeatStatusByName_WithNullName_ReturnsNull()
        {
            // Arrange
            string statusName = null;
            _mockRepository.Setup(r => r.GetSeatStatusByName(statusName))
                .Returns((SeatStatus)null);

            // Act
            var result = _service.GetSeatStatusByName(statusName);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetSeatStatusByName(statusName), Times.Once);
        }
    }
} 