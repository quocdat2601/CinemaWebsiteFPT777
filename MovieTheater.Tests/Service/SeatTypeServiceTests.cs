using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class SeatTypeServiceTests
    {
        private readonly Mock<ISeatTypeRepository> _mockRepository;
        private readonly SeatTypeService _service;

        public SeatTypeServiceTests()
        {
            _mockRepository = new Mock<ISeatTypeRepository>();
            _service = new SeatTypeService(_mockRepository.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllSeatTypes()
        {
            // Arrange
            var expectedSeatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" },
                new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 150000, ColorHex = "#00FF00" },
                new SeatType { SeatTypeId = 3, TypeName = "Premium", PricePercent = 200000, ColorHex = "#0000FF" }
            };
            _mockRepository.Setup(r => r.GetAll()).Returns(expectedSeatTypes);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            _mockRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAll_WhenNoSeatTypes_ReturnsEmptyList()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAll()).Returns(new List<SeatType>());

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WhenSeatTypeExists_ReturnsSeatType()
        {
            // Arrange
            var expectedSeatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" };
            _mockRepository.Setup(r => r.GetById(1)).Returns(expectedSeatType);

            // Act
            var result = _service.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SeatTypeId);
            Assert.Equal("Standard", result.TypeName);
            _mockRepository.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void GetById_WhenSeatTypeDoesNotExist_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetById(999)).Returns((SeatType?)null);

            // Act
            var result = _service.GetById(999);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetById(999), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100000, ColorHex = "#FF0000" };

            // Act
            _service.Update(seatType);

            // Assert
            _mockRepository.Verify(r => r.Update(seatType), Times.Once);
        }

        [Fact]
        public void Save_CallsRepositorySave()
        {
            // Act
            _service.Save();

            // Assert
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }
    }
} 