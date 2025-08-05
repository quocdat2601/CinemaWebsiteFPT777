using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class SeatTypeRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly SeatTypeRepository _repository;

        public SeatTypeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new SeatTypeRepository(_context);
        }

        [Fact]
        public void GetAll_ReturnsAllSeatTypes()
        {
            // Arrange
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" },
                new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 150000, ColorHex = "#00FF00" },
                new SeatType { SeatTypeId = 3, TypeName = "Premium", PricePercent = 200000, ColorHex = "#0000FF" }
            };
            _context.SeatTypes.AddRange(seatTypes);
            _context.SaveChanges();

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void GetAll_WhenNoSeatTypes_ReturnsEmptyList()
        {
            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetById_WhenSeatTypeExists_ReturnsSeatType()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" };
            _context.SeatTypes.Add(seatType);
            _context.SaveChanges();

            // Act
            var result = _repository.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SeatTypeId);
            Assert.Equal("Standard", result.TypeName);
            Assert.Equal(100000, result.PricePercent);
            Assert.Equal("#FF0000", result.ColorHex);
        }

        [Fact]
        public void GetById_WhenSeatTypeDoesNotExist_ReturnsNull()
        {
            // Act
            var result = _repository.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Update_WhenSeatTypeExists_UpdatesSeatType()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" };
            _context.SeatTypes.Add(seatType);
            _context.SaveChanges();

            var updatedSeatType = new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 150000, ColorHex = "#00FF00" };

            // Act
            _repository.Update(updatedSeatType);
            _repository.Save();

            // Assert
            var result = _context.SeatTypes.Find(1);
            Assert.NotNull(result);
            Assert.Equal("Standard", result.TypeName); // TypeName should not change
            Assert.Equal(150000, result.PricePercent); // PricePercent should change
            Assert.Equal("#00FF00", result.ColorHex); // ColorHex should change
        }

        [Fact]
        public void Update_WhenSeatTypeDoesNotExist_DoesNothing()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 999, TypeName = "Non-existent", PricePercent = 100000, ColorHex = "#FF0000" };

            // Act & Assert - Should not throw exception
            var exception = Record.Exception(() => _repository.Update(seatType));
            Assert.Null(exception);
        }

        [Fact]
        public void Update_WhenSeatTypeExists_OnlyUpdatesPriceAndColor()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" };
            _context.SeatTypes.Add(seatType);
            _context.SaveChanges();

            var updatedSeatType = new SeatType { SeatTypeId = 1, TypeName = "Changed Name", PricePercent = 150000, ColorHex = "#00FF00" };

            // Act
            _repository.Update(updatedSeatType);
            _repository.Save();

            // Assert
            var result = _context.SeatTypes.Find(1);
            Assert.NotNull(result);
            Assert.Equal("Standard", result.TypeName); // Should not change
            Assert.Equal(150000, result.PricePercent); // Should change
            Assert.Equal("#00FF00", result.ColorHex); // Should change
        }

        [Fact]
        public void Save_SavesChangesToDatabase()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" };
            _context.SeatTypes.Add(seatType);

            // Act
            _repository.Save();

            // Assert
            var savedSeatType = _context.SeatTypes.Find(1);
            Assert.NotNull(savedSeatType);
            Assert.Equal("Standard", savedSeatType.TypeName);
        }
    }
} 