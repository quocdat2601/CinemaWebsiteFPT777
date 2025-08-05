using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class CoupleSeatRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly CoupleSeatRepository _repository;

        public CoupleSeatRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new CoupleSeatRepository(_context);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllCoupleSeats()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" },
                new Seat { SeatId = 3, SeatName = "B1" },
                new Seat { SeatId = 4, SeatName = "B2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeats = new List<CoupleSeat>
            {
                new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 },
                new CoupleSeat { CoupleSeatId = 2, FirstSeatId = 3, SecondSeatId = 4 }
            };
            _context.CoupleSeats.AddRange(coupleSeats);
            _context.SaveChanges();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllAsync_WhenNoCoupleSeats_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCoupleSeatExists_ReturnsCoupleSeat()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeat = new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 };
            _context.CoupleSeats.Add(coupleSeat);
            _context.SaveChanges();

            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.CoupleSeatId);
            Assert.Equal(1, result.FirstSeatId);
            Assert.Equal(2, result.SecondSeatId);
            Assert.NotNull(result.FirstSeat);
            Assert.NotNull(result.SecondSeat);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCoupleSeatDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBySeatIdAsync_WhenSeatIsInCouple_ReturnsCoupleSeats()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" },
                new Seat { SeatId = 3, SeatName = "B1" },
                new Seat { SeatId = 4, SeatName = "B2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeats = new List<CoupleSeat>
            {
                new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 },
                new CoupleSeat { CoupleSeatId = 2, FirstSeatId = 3, SecondSeatId = 4 }
            };
            _context.CoupleSeats.AddRange(coupleSeats);
            _context.SaveChanges();

            // Act
            var result = await _repository.GetBySeatIdAsync(1);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result.First().CoupleSeatId);
        }

        [Fact]
        public async Task GetBySeatIdAsync_WhenSeatIsNotInCouple_ReturnsEmptyList()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeat = new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 };
            _context.CoupleSeats.Add(coupleSeat);
            _context.SaveChanges();

            // Act
            var result = await _repository.GetBySeatIdAsync(3);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateAsync_AddsCoupleSeatToDatabase()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" }
            };
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            var coupleSeat = new CoupleSeat { FirstSeatId = 1, SecondSeatId = 2 };

            // Act
            var result = await _repository.CreateAsync(coupleSeat);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.FirstSeatId);
            Assert.Equal(2, result.SecondSeatId);
            Assert.True(result.CoupleSeatId > 0); // Auto-generated ID

            var savedCoupleSeat = await _context.CoupleSeats.FindAsync(result.CoupleSeatId);
            Assert.NotNull(savedCoupleSeat);
        }

        [Fact]
        public async Task DeleteAsync_WhenCoupleSeatExists_RemovesCoupleSeat()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeat = new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 };
            _context.CoupleSeats.Add(coupleSeat);
            _context.SaveChanges();

            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            Assert.True(result);
            var deletedCoupleSeat = await _context.CoupleSeats.FindAsync(1);
            Assert.Null(deletedCoupleSeat);
        }

        [Fact]
        public async Task DeleteAsync_WhenCoupleSeatDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _repository.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsSeatInCoupleAsync_WhenSeatIsInCouple_ReturnsTrue()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeat = new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 };
            _context.CoupleSeats.Add(coupleSeat);
            _context.SaveChanges();

            // Act
            var result = await _repository.IsSeatInCoupleAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsSeatInCoupleAsync_WhenSeatIsNotInCouple_ReturnsFalse()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" },
                new Seat { SeatId = 3, SeatName = "A3" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeat = new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 };
            _context.CoupleSeats.Add(coupleSeat);
            _context.SaveChanges();

            // Act
            var result = await _repository.IsSeatInCoupleAsync(3);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsSeatInCoupleAsync_WhenSeatIsSecondSeat_ReturnsTrue()
        {
            // Arrange
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" }
            };
            _context.Seats.AddRange(seats);

            var coupleSeat = new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 };
            _context.CoupleSeats.Add(coupleSeat);
            _context.SaveChanges();

            // Act
            var result = await _repository.IsSeatInCoupleAsync(2);

            // Assert
            Assert.True(result);
        }
    }
} 