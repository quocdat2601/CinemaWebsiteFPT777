using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Repository;
using MovieTheater.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieTheater.Tests.Repository
{
    public class CinemaRepositoryTests
    {
        private Mock<ISeatRepository> _seatRepositoryMock;
        private DbContextOptions<MovieTheaterContext> _options;

        public CinemaRepositoryTests()
        {
            _seatRepositoryMock = new Mock<ISeatRepository>();
            _options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private MovieTheaterContext CreateContext()
        {
            var context = new MovieTheaterContext(_options);
            context.Database.EnsureCreated();
            return context;
        }

        private void SeedData(MovieTheaterContext context)
        {
            // Add versions
            var version1 = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var version2 = new MovieTheater.Models.Version { VersionId = 2, VersionName = "3D" };
            context.Versions.AddRange(version1, version2);

            // Add statuses
            var status1 = new Status { StatusId = 1, StatusName = "Active" };
            var status2 = new Status { StatusId = 2, StatusName = "Deleted" };
            var status3 = new Status { StatusId = 3, StatusName = "Disabled" };
            context.Statuses.AddRange(status1, status2, status3);

            // Add cinema rooms
            var cinemaRoom1 = new CinemaRoom
            {
                CinemaRoomId = 1,
                CinemaRoomName = "Room A",
                VersionId = 1,
                StatusId = 1,
                SeatLength = 5,
                SeatWidth = 5
            };
            var cinemaRoom2 = new CinemaRoom
            {
                CinemaRoomId = 2,
                CinemaRoomName = "Room B",
                VersionId = 2,
                StatusId = 1,
                SeatLength = 6,
                SeatWidth = 4
            };
            var cinemaRoom3 = new CinemaRoom
            {
                CinemaRoomId = 3,
                CinemaRoomName = "Room C",
                VersionId = 1,
                StatusId = 2,
                SeatLength = 4,
                SeatWidth = 4
            };
            context.CinemaRooms.AddRange(cinemaRoom1, cinemaRoom2, cinemaRoom3);

            context.SaveChanges();
        }

        [Fact]
        public void GetAll_ReturnsActiveAndDisabledRooms()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = repository.GetAll().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.CinemaRoomId == 1);
            Assert.Contains(result, r => r.CinemaRoomId == 2);
            Assert.DoesNotContain(result, r => r.CinemaRoomId == 3); // StatusId = 2 (deleted)
        }

        [Fact]
        public void GetById_ExistingId_ReturnsCinemaRoom()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = repository.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CinemaRoomId);
            Assert.Equal("Room A", result.CinemaRoomName);
            Assert.NotNull(result.Version);
            Assert.NotNull(result.Status);
        }

        [Fact]
        public void GetById_NonExistingId_ReturnsNull()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = repository.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetById_NullId_ReturnsNull()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = repository.GetById(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCinemaRoom()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CinemaRoomId);
            Assert.Equal("Room A", result.CinemaRoomName);
            Assert.NotNull(result.Version);
            Assert.NotNull(result.Status);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = await repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_ValidCinemaRoom_AddsToDatabase()
        {
            // Arrange
            using var context = CreateContext();
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var newCinemaRoom = new CinemaRoom
            {
                CinemaRoomName = "New Room",
                VersionId = 1,
                StatusId = 1,
                SeatLength = 4,
                SeatWidth = 4
            };

            // Act
            repository.Add(newCinemaRoom);

            // Assert
            var addedRoom = context.CinemaRooms.FirstOrDefault(r => r.CinemaRoomName == "New Room");
            Assert.NotNull(addedRoom);
            Assert.Equal("New Room", addedRoom.CinemaRoomName);

            // Check that seats were generated
            var seats = context.Seats.Where(s => s.CinemaRoomId == addedRoom.CinemaRoomId).ToList();
            Assert.Equal(16, seats.Count); // 4x4 = 16 seats
        }

        [Fact]
        public void Update_ExistingCinemaRoom_UpdatesSuccessfully()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var updatedRoom = new CinemaRoom
            {
                CinemaRoomId = 1,
                CinemaRoomName = "Updated Room A",
                VersionId = 2,
                SeatLength = 6,
                SeatWidth = 4
            };

            // Act
            repository.Update(updatedRoom);

            // Assert
            var result = context.CinemaRooms.FirstOrDefault(r => r.CinemaRoomId == 1);
            Assert.NotNull(result);
            Assert.Equal("Updated Room A", result.CinemaRoomName);
            Assert.Equal(2, result.VersionId);
            Assert.Equal(6, result.SeatLength);
            Assert.Equal(4, result.SeatWidth);

            // Check that seats were regenerated
            var seats = context.Seats.Where(s => s.CinemaRoomId == 1).ToList();
            Assert.Equal(24, seats.Count); // 6x4 = 24 seats
        }

        [Fact]
        public async Task Update_NonExistingCinemaRoom_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var nonExistingRoom = new CinemaRoom
            {
                CinemaRoomId = 999,
                CinemaRoomName = "Non Existing Room",
                VersionId = 1,
                SeatLength = 4,
                SeatWidth = 4
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.Update(nonExistingRoom));
            Assert.Contains("Cinema room with ID 999 not found", exception.Message);
        }

        [Fact]
        public void Update_SameSeatDimensions_DoesNotRegenerateSeats()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Get initial seat count
            var initialSeatCount = context.Seats.Where(s => s.CinemaRoomId == 1).Count();

            var updatedRoom = new CinemaRoom
            {
                CinemaRoomId = 1,
                CinemaRoomName = "Updated Room A",
                VersionId = 2,
                SeatLength = 5, // Same as original
                SeatWidth = 5   // Same as original
            };

            // Act
            repository.Update(updatedRoom);

            // Assert
            var finalSeatCount = context.Seats.Where(s => s.CinemaRoomId == 1).Count();
            Assert.Equal(initialSeatCount, finalSeatCount);
        }

        [Fact]
        public async Task Update_DatabaseException_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);

            // Create a mock context that throws an exception on SaveChanges
            var mockContext = new Mock<MovieTheaterContext>(_options);
            mockContext.Setup(c => c.CinemaRooms).Returns(context.CinemaRooms);
            mockContext.Setup(c => c.Seats).Returns(context.Seats);
            mockContext.Setup(c => c.SaveChanges()).Throws(new Exception("Database connection failed"));

            var repository = new CinemaRepository(mockContext.Object, _seatRepositoryMock.Object);
            var updatedRoom = new CinemaRoom
            {
                CinemaRoomId = 1,
                CinemaRoomName = "Updated Room A",
                VersionId = 2,
                SeatLength = 6,
                SeatWidth = 4
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => repository.Update(updatedRoom));
            Assert.Contains("Error updating cinema room", exception.Message);
            Assert.Contains("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task Delete_ExistingCinemaRoom_SetsStatusToDeleted()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            await repository.Delete(1);

            // Assert
            var deletedRoom = context.CinemaRooms.FirstOrDefault(r => r.CinemaRoomId == 1);
            Assert.NotNull(deletedRoom);
            Assert.Equal(2, deletedRoom.StatusId); // StatusId = 2 means deleted
        }

        [Fact]
        public async Task Delete_NonExistingCinemaRoom_DoesNothing()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            await repository.Delete(999);

            // Assert - should not throw exception
            Assert.True(true);
        }

        [Fact]
        public async Task Enable_ExistingCinemaRoom_EnablesSuccessfully()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // First disable the room
            var roomToDisable = new CinemaRoom
            {
                CinemaRoomId = 1,
                StatusId = 3,
                UnavailableStartDate = DateTime.Now,
                UnavailableEndDate = DateTime.Now.AddDays(7),
                DisableReason = "Maintenance"
            };
            await repository.Disable(roomToDisable);

            // Act
            var roomToEnable = new CinemaRoom { CinemaRoomId = 1 };
            await repository.Enable(roomToEnable);

            // Assert
            var enabledRoom = context.CinemaRooms.FirstOrDefault(r => r.CinemaRoomId == 1);
            Assert.NotNull(enabledRoom);
            Assert.Equal(1, enabledRoom.StatusId); // StatusId = 1 means active
            Assert.Null(enabledRoom.UnavailableStartDate);
            Assert.Null(enabledRoom.UnavailableEndDate);
            Assert.Null(enabledRoom.DisableReason);
        }

        [Fact]
        public async Task Enable_NonExistingCinemaRoom_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var nonExistingRoom = new CinemaRoom { CinemaRoomId = 999 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.Enable(nonExistingRoom));
            Assert.Contains("Cinema room with ID 999 not found", exception.Message);
        }

        [Fact]
        public async Task Enable_DatabaseException_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);

            // Create a mock context that throws an exception on SaveChangesAsync
            var mockContext = new Mock<MovieTheaterContext>(_options);
            mockContext.Setup(c => c.CinemaRooms).Returns(context.CinemaRooms);
            mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Database connection failed"));

            var repository = new CinemaRepository(mockContext.Object, _seatRepositoryMock.Object);
            var roomToEnable = new CinemaRoom { CinemaRoomId = 1 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => repository.Enable(roomToEnable));
            Assert.Contains("Error activating cinema room", exception.Message);
            Assert.Contains("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task Disable_ExistingCinemaRoom_DisablesSuccessfully()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var roomToDisable = new CinemaRoom
            {
                CinemaRoomId = 1,
                UnavailableStartDate = DateTime.Now,
                UnavailableEndDate = DateTime.Now.AddDays(7),
                DisableReason = "Maintenance"
            };

            // Act
            await repository.Disable(roomToDisable);

            // Assert
            var disabledRoom = context.CinemaRooms.FirstOrDefault(r => r.CinemaRoomId == 1);
            Assert.NotNull(disabledRoom);
            Assert.Equal(3, disabledRoom.StatusId); // StatusId = 3 means disabled
            Assert.NotNull(disabledRoom.UnavailableStartDate);
            Assert.NotNull(disabledRoom.UnavailableEndDate);
            Assert.Equal("Maintenance", disabledRoom.DisableReason);
        }

        [Fact]
        public async Task Disable_NonExistingCinemaRoom_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var nonExistingRoom = new CinemaRoom { CinemaRoomId = 999 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.Disable(nonExistingRoom));
            Assert.Contains("Cinema room with ID 999 not found", exception.Message);
        }

        [Fact]
        public async Task Disable_DatabaseException_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);

            // Create a mock context that throws an exception on SaveChangesAsync
            var mockContext = new Mock<MovieTheaterContext>(_options);
            mockContext.Setup(c => c.CinemaRooms).Returns(context.CinemaRooms);
            mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Database connection failed"));

            var repository = new CinemaRepository(mockContext.Object, _seatRepositoryMock.Object);
            var roomToDisable = new CinemaRoom
            {
                CinemaRoomId = 1,
                UnavailableStartDate = DateTime.Now,
                UnavailableEndDate = DateTime.Now.AddDays(7),
                DisableReason = "Maintenance"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => repository.Disable(roomToDisable));
            Assert.Contains("Error activating cinema room", exception.Message);
            Assert.Contains("Database connection failed", exception.Message);
        }

        [Fact]
        public async Task Save_SavesChangesToDatabase()
        {
            // Arrange
            using var context = CreateContext();
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);
            var newRoom = new CinemaRoom
            {
                CinemaRoomName = "Test Room",
                VersionId = 1,
                StatusId = 1,
                SeatLength = 3,
                SeatWidth = 3
            };
            context.CinemaRooms.Add(newRoom);

            // Act
            await repository.Save();

            // Assert
            var savedRoom = context.CinemaRooms.FirstOrDefault(r => r.CinemaRoomName == "Test Room");
            Assert.NotNull(savedRoom);
        }

        [Fact]
        public void GetRoomsByVersion_ValidVersionId_ReturnsAllMatchingRooms()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = repository.GetRoomsByVersion(1).ToList();

            // Assert
            Assert.True(result.Count >= 1); // At least one room
            Assert.All(result, r => Assert.Equal(1, r.VersionId)); // All rooms should match version
            Assert.Equal(1, result[0].StatusId); // Only active rooms
        }

        [Fact]
        public void GetRoomsByVersion_NonExistingVersionId_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Act
            var result = repository.GetRoomsByVersion(999).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetRoomsByVersion_ReturnsAllRoomsRegardlessOfStatus()
        {
            // Arrange
            using var context = CreateContext();
            SeedData(context);
            var repository = new CinemaRepository(context, _seatRepositoryMock.Object);

            // Add a disabled room with version 1
            var disabledRoom = new CinemaRoom
            {
                CinemaRoomId = 4,
                CinemaRoomName = "Disabled Room",
                VersionId = 1,
                SeatLength = 4,
                SeatWidth = 4,
                StatusId = 3 // Optional: for clarity
            };
            context.CinemaRooms.Add(disabledRoom);
            context.SaveChanges();

            // Act
            var result = repository.GetRoomsByVersion(1).ToList();

            Assert.Equal(2, result.Count); // Expecting 2 active rooms with VersionId = 1 (excluding deleted room)
            Assert.Contains(result, r => r.CinemaRoomId == 1); // Active room
            Assert.Contains(result, r => r.CinemaRoomId == 4); // Disabled room
            Assert.DoesNotContain(result, r => r.CinemaRoomId == 3); // Deleted room should not be included
        }
    }
}