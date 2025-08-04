using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class SeatRepositoryTests : IDisposable
    {
        private readonly MovieTheaterContext _context;
        private readonly SeatRepository _repository;

        public SeatRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MovieTheaterContext(options);
            _repository = new SeatRepository(_context);
            
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add CinemaRoom
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room A" };
            _context.CinemaRooms.Add(cinemaRoom);

            // Add SeatStatuses
            var seatStatuses = new List<SeatStatus>
            {
                new SeatStatus { SeatStatusId = 1, StatusName = "Available" },
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" },
                new SeatStatus { SeatStatusId = 3, StatusName = "BeingHeld" }
            };
            _context.SeatStatuses.AddRange(seatStatuses);

            // Add SeatTypes
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 50000, ColorHex = "#6c757d" },
                new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 80000, ColorHex = "#ffc107" },
                new SeatType { SeatTypeId = 3, TypeName = "Couple", PricePercent = 120000, ColorHex = "#e83e8c" }
            };
            _context.SeatTypes.AddRange(seatTypes);

            // Add Seats
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, CinemaRoomId = 1, SeatName = "A1", SeatRow = 1, SeatColumn = "A", SeatStatusId = 1, SeatTypeId = 1 },
                new Seat { SeatId = 2, CinemaRoomId = 1, SeatName = "A2", SeatRow = 1, SeatColumn = "A", SeatStatusId = 1, SeatTypeId = 1 },
                new Seat { SeatId = 3, CinemaRoomId = 1, SeatName = "B1", SeatRow = 2, SeatColumn = "B", SeatStatusId = 2, SeatTypeId = 2 },
                new Seat { SeatId = 4, CinemaRoomId = 1, SeatName = "B2", SeatRow = 2, SeatColumn = "B", SeatStatusId = 1, SeatTypeId = 2 },
                new Seat { SeatId = 5, CinemaRoomId = 1, SeatName = "C1", SeatRow = 3, SeatColumn = "C", SeatStatusId = 1, SeatTypeId = 3 }
            };
            _context.Seats.AddRange(seats);

            // Add CoupleSeats
            var coupleSeats = new List<CoupleSeat>
            {
                new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 },
                new CoupleSeat { CoupleSeatId = 2, FirstSeatId = 3, SecondSeatId = 4 }
            };
            _context.CoupleSeats.AddRange(coupleSeats);

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllSeats()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetByCinemaRoomIdAsync_ValidRoomId_ReturnsSeats()
        {
            // Arrange
            int cinemaRoomId = 1;

            // Act
            var result = await _repository.GetByCinemaRoomIdAsync(cinemaRoomId);

            // Assert
            Assert.Equal(5, result.Count);
            Assert.All(result, seat => Assert.Equal(cinemaRoomId, seat.CinemaRoomId));
            Assert.All(result, seat => Assert.NotNull(seat.SeatType));
        }

        [Fact]
        public async Task GetByCinemaRoomIdAsync_InvalidRoomId_ReturnsEmptyList()
        {
            // Arrange
            int cinemaRoomId = 999;

            // Act
            var result = await _repository.GetByCinemaRoomIdAsync(cinemaRoomId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetById_ExistingSeat_ReturnsSeat()
        {
            // Arrange
            int seatId = 1;

            // Act
            var result = _repository.GetById(seatId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(seatId, result.SeatId);
            Assert.Equal("A1", result.SeatName);
            Assert.NotNull(result.SeatType);
        }

        [Fact]
        public void GetById_NonExistingSeat_ReturnsNull()
        {
            // Arrange
            int seatId = 999;

            // Act
            var result = _repository.GetById(seatId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_ValidSeat_AddsToContext()
        {
            // Arrange
            var seat = new Seat
            {
                CinemaRoomId = 1,
                SeatName = "D1",
                SeatRow = 4,
                SeatColumn = "D",
                SeatStatusId = 1,
                SeatTypeId = 1
            };

            // Act
            _repository.Add(seat);
            _repository.Save(); // Save changes to database

            // Assert
            var addedSeat = _context.Seats.FirstOrDefault(s => s.SeatName == "D1");
            Assert.NotNull(addedSeat);
        }

        [Fact]
        public void Update_ExistingSeat_UpdatesSuccessfully()
        {
            // Arrange
            var seat = _repository.GetById(1);
            seat.SeatName = "A1_Updated";

            // Act
            _repository.Update(seat);

            // Assert
            var updatedSeat = _repository.GetById(1);
            Assert.Equal("A1_Updated", updatedSeat.SeatName);
        }

        [Fact]
        public async Task DeleteAsync_ExistingSeat_RemovesFromContext()
        {
            // Arrange
            int seatId = 1;

            // Remove related CoupleSeats first to avoid foreign key constraint
            var coupleSeats = _context.CoupleSeats
                .Where(cs => cs.FirstSeatId == seatId || cs.SecondSeatId == seatId)
                .ToList();
            _context.CoupleSeats.RemoveRange(coupleSeats);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(seatId);
            _repository.Save(); // Save changes to database

            // Assert
            var deletedSeat = _repository.GetById(seatId);
            Assert.Null(deletedSeat);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingSeat_DoesNothing()
        {
            // Arrange
            int seatId = 999;

            // Act
            await _repository.DeleteAsync(seatId);

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public async Task GetSeatTypesAsync_ReturnsAllSeatTypes()
        {
            // Act
            var result = await _repository.GetSeatTypesAsync();

            // Assert
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void GetSeatByName_ExistingSeat_ReturnsSeat()
        {
            // Arrange
            string seatName = "A1";

            // Act
            var result = _repository.GetSeatByName(seatName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(seatName, result.SeatName);
        }

        [Fact]
        public void GetSeatByName_NonExistingSeat_ReturnsNull()
        {
            // Arrange
            string seatName = "NonExistent";

            // Act
            var result = _repository.GetSeatByName(seatName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteCoupleSeatBySeatIdsAsync_ExistingCoupleSeat_RemovesFromContext()
        {
            // Arrange
            int seatId1 = 1;
            int seatId2 = 2;

            // Act
            await _repository.DeleteCoupleSeatBySeatIdsAsync(seatId1, seatId2);

            // Assert
            var coupleSeat = _context.CoupleSeats
                .FirstOrDefault(cs => 
                    (cs.FirstSeatId == seatId1 && cs.SecondSeatId == seatId2) ||
                    (cs.FirstSeatId == seatId2 && cs.SecondSeatId == seatId1));
            Assert.Null(coupleSeat);
        }

        [Fact]
        public async Task DeleteCoupleSeatBySeatIdsAsync_NonExistingCoupleSeat_DoesNothing()
        {
            // Arrange
            int seatId1 = 999;
            int seatId2 = 998;

            // Act
            await _repository.DeleteCoupleSeatBySeatIdsAsync(seatId1, seatId2);

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void GetSeatsWithTypeByIds_ValidIds_ReturnsSeats()
        {
            // Arrange
            var seatIds = new List<int> { 1, 2, 3 };

            // Act
            var result = _repository.GetSeatsWithTypeByIds(seatIds);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, seat => Assert.NotNull(seat.SeatType));
        }

        [Fact]
        public void GetSeatsWithTypeByIds_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var seatIds = new List<int>();

            // Act
            var result = _repository.GetSeatsWithTypeByIds(seatIds);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetSeatsByNames_ValidNames_ReturnsSeats()
        {
            // Arrange
            var seatNames = new List<string> { "A1", "A2", "B1" };

            // Act
            var result = _repository.GetSeatsByNames(seatNames);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, seat => Assert.NotNull(seat.SeatType));
        }

        [Fact]
        public void GetSeatsByNames_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var seatNames = new List<string>();

            // Act
            var result = _repository.GetSeatsByNames(seatNames);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateSeatsStatusToBookedAsync_ValidSeatIds_UpdatesStatus()
        {
            // Arrange
            var seatIds = new List<int> { 1, 2 };

            // Act
            await _repository.UpdateSeatsStatusToBookedAsync(seatIds);

            // Assert
            var updatedSeats = _context.Seats.Where(s => seatIds.Contains(s.SeatId)).ToList();
            Assert.All(updatedSeats, seat => Assert.Equal(2, seat.SeatStatusId)); // 2 = Booked
        }

        [Fact]
        public async Task UpdateSeatsStatusToBookedAsync_BookedStatusNotFound_ThrowsException()
        {
            // Arrange
            _context.SeatStatuses.RemoveRange(_context.SeatStatuses.ToList());
            await _context.SaveChangesAsync();
            var seatIds = new List<int> { 1 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _repository.UpdateSeatsStatusToBookedAsync(seatIds));
        }

        [Fact]
        public void GetSeatStatusByName_ExistingStatus_ReturnsStatus()
        {
            // Arrange
            string statusName = "Available";

            // Act
            var result = _repository.GetSeatStatusByName(statusName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(statusName, result.StatusName);
        }

        [Fact]
        public void GetSeatStatusByName_NonExistingStatus_ReturnsNull()
        {
            // Arrange
            string statusName = "NonExistent";

            // Act
            var result = _repository.GetSeatStatusByName(statusName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Save_SavesChangesToDatabase()
        {
            // Arrange
            var seat = new Seat
            {
                CinemaRoomId = 1,
                SeatName = "SaveTest",
                SeatRow = 5,
                SeatColumn = "E",
                SeatStatusId = 1,
                SeatTypeId = 1
            };
            _context.Seats.Add(seat);

            // Act
            _repository.Save();

            // Assert
            var savedSeat = _repository.GetSeatByName("SaveTest");
            Assert.NotNull(savedSeat);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 