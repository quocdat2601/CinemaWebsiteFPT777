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
    public class CoupleSeatServiceTests
    {
        private readonly Mock<ICoupleSeatRepository> _mockRepository;
        private readonly CoupleSeatService _service;

        public CoupleSeatServiceTests()
        {
            _mockRepository = new Mock<ICoupleSeatRepository>();
            _service = new CoupleSeatService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetAllCoupleSeatsAsync_ReturnsAllCoupleSeats()
        {
            // Arrange
            var expectedCoupleSeats = new List<CoupleSeat>
            {
                new CoupleSeat { CoupleSeatId = 1, FirstSeatId = 1, SecondSeatId = 2 },
                new CoupleSeat { CoupleSeatId = 2, FirstSeatId = 3, SecondSeatId = 4 }
            };
            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(expectedCoupleSeats);

            // Act
            var result = await _service.GetAllCoupleSeatsAsync();

            // Assert
            Assert.Equal(expectedCoupleSeats, result);
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllCoupleSeatsAsync_WhenRepositoryReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            var expectedCoupleSeats = new List<CoupleSeat>();
            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(expectedCoupleSeats);

            // Act
            var result = await _service.GetAllCoupleSeatsAsync();

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCoupleSeatByIdAsync_WithValidId_ReturnsCoupleSeat()
        {
            // Arrange
            var id = 1;
            var expectedCoupleSeat = new CoupleSeat
            {
                CoupleSeatId = id,
                FirstSeatId = 1,
                SecondSeatId = 2
            };
            _mockRepository.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(expectedCoupleSeat);

            // Act
            var result = await _service.GetCoupleSeatByIdAsync(id);

            // Assert
            Assert.Equal(expectedCoupleSeat, result);
            _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetCoupleSeatByIdAsync_WhenCoupleSeatNotFound_ReturnsNull()
        {
            // Arrange
            var id = 1;
            _mockRepository.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((CoupleSeat)null);

            // Act
            var result = await _service.GetCoupleSeatByIdAsync(id);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetCoupleSeatsBySeatIdAsync_WithValidSeatId_ReturnsCoupleSeats()
        {
            // Arrange
            var seatId = 1;
            var expectedCoupleSeats = new List<CoupleSeat>
            {
                new CoupleSeat { CoupleSeatId = 1, FirstSeatId = seatId, SecondSeatId = 2 },
                new CoupleSeat { CoupleSeatId = 2, FirstSeatId = 3, SecondSeatId = seatId }
            };
            _mockRepository.Setup(r => r.GetBySeatIdAsync(seatId))
                .ReturnsAsync(expectedCoupleSeats);

            // Act
            var result = await _service.GetCoupleSeatsBySeatIdAsync(seatId);

            // Assert
            Assert.Equal(expectedCoupleSeats, result);
            _mockRepository.Verify(r => r.GetBySeatIdAsync(seatId), Times.Once);
        }

        [Fact]
        public async Task GetCoupleSeatsBySeatIdAsync_WhenNoCoupleSeatsFound_ReturnsEmpty()
        {
            // Arrange
            var seatId = 1;
            var expectedCoupleSeats = new List<CoupleSeat>();
            _mockRepository.Setup(r => r.GetBySeatIdAsync(seatId))
                .ReturnsAsync(expectedCoupleSeats);

            // Act
            var result = await _service.GetCoupleSeatsBySeatIdAsync(seatId);

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetBySeatIdAsync(seatId), Times.Once);
        }

        [Fact]
        public async Task CreateCoupleSeatAsync_WithValidPair_ReturnsCreatedCoupleSeat()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;
            var expectedCoupleSeat = new CoupleSeat
            {
                CoupleSeatId = 1,
                FirstSeatId = firstSeatId,
                SecondSeatId = secondSeatId
            };

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.CreateAsync(It.Is<CoupleSeat>(cs => 
                cs.FirstSeatId == firstSeatId && cs.SecondSeatId == secondSeatId)))
                .ReturnsAsync(expectedCoupleSeat);

            // Act
            var result = await _service.CreateCoupleSeatAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.Equal(expectedCoupleSeat, result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(firstSeatId), Times.Once);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(secondSeatId), Times.Once);
            _mockRepository.Verify(r => r.CreateAsync(It.Is<CoupleSeat>(cs => 
                cs.FirstSeatId == firstSeatId && cs.SecondSeatId == secondSeatId)), Times.Once);
        }

        [Fact]
        public async Task CreateCoupleSeatAsync_WithReversedSeatIds_SwapsSeatIds()
        {
            // Arrange
            var firstSeatId = 5;
            var secondSeatId = 3;
            var expectedCoupleSeat = new CoupleSeat
            {
                CoupleSeatId = 1,
                FirstSeatId = 3, // Should be swapped to smaller ID
                SecondSeatId = 5
            };

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.CreateAsync(It.Is<CoupleSeat>(cs => 
                cs.FirstSeatId == 3 && cs.SecondSeatId == 5)))
                .ReturnsAsync(expectedCoupleSeat);

            // Act
            var result = await _service.CreateCoupleSeatAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.Equal(expectedCoupleSeat, result);
            _mockRepository.Verify(r => r.CreateAsync(It.Is<CoupleSeat>(cs => 
                cs.FirstSeatId == 3 && cs.SecondSeatId == 5)), Times.Once);
        }

        [Fact]
        public async Task CreateCoupleSeatAsync_WithSameSeatIds_ThrowsInvalidOperationException()
        {
            // Arrange
            var seatId = 1;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateCoupleSeatAsync(seatId, seatId));
        }

        [Fact]
        public async Task CreateCoupleSeatAsync_WhenFirstSeatInCouple_ThrowsInvalidOperationException()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateCoupleSeatAsync(firstSeatId, secondSeatId));
        }

        [Fact]
        public async Task CreateCoupleSeatAsync_WhenSecondSeatInCouple_ThrowsInvalidOperationException()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateCoupleSeatAsync(firstSeatId, secondSeatId));
        }

        [Fact]
        public async Task DeleteCoupleSeatAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var id = 1;
            _mockRepository.Setup(r => r.DeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteCoupleSeatAsync(id);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteCoupleSeatAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var id = 1;
            _mockRepository.Setup(r => r.DeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteCoupleSeatAsync(id);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task IsSeatInCoupleAsync_WithSeatInCouple_ReturnsTrue()
        {
            // Arrange
            var seatId = 1;
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(seatId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsSeatInCoupleAsync(seatId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(seatId), Times.Once);
        }

        [Fact]
        public async Task IsSeatInCoupleAsync_WithSeatNotInCouple_ReturnsFalse()
        {
            // Arrange
            var seatId = 1;
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(seatId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.IsSeatInCoupleAsync(seatId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(seatId), Times.Once);
        }

        [Fact]
        public async Task ValidateCoupleSeatPairAsync_WithValidPair_ReturnsTrue()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ValidateCoupleSeatPairAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(firstSeatId), Times.Once);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(secondSeatId), Times.Once);
        }

        [Fact]
        public async Task ValidateCoupleSeatPairAsync_WithSameSeatIds_ReturnsFalse()
        {
            // Arrange
            var seatId = 1;

            // Act
            var result = await _service.ValidateCoupleSeatPairAsync(seatId, seatId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ValidateCoupleSeatPairAsync_WhenFirstSeatInCouple_ReturnsFalse()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ValidateCoupleSeatPairAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(firstSeatId), Times.Once);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(secondSeatId), Times.Never);
        }

        [Fact]
        public async Task ValidateCoupleSeatPairAsync_WhenSecondSeatInCouple_ReturnsFalse()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ValidateCoupleSeatPairAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(firstSeatId), Times.Once);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(secondSeatId), Times.Once);
        }

        [Fact]
        public async Task ValidateCoupleSeatPairAsync_WhenBothSeatsInCouple_ReturnsFalse()
        {
            // Arrange
            var firstSeatId = 1;
            var secondSeatId = 2;

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ValidateCoupleSeatPairAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.IsSeatInCoupleAsync(firstSeatId), Times.Once);
            // Không verify secondSeatId vì service dừng ngay khi firstSeatId đã trong couple
        }

        [Fact]
        public async Task CreateCoupleSeatAsync_WithValidPairAndReversedIds_SwapsAndCreates()
        {
            // Arrange
            var firstSeatId = 10;
            var secondSeatId = 5;
            var expectedCoupleSeat = new CoupleSeat
            {
                CoupleSeatId = 1,
                FirstSeatId = 5, // Should be swapped to smaller ID
                SecondSeatId = 10
            };

            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(firstSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.IsSeatInCoupleAsync(secondSeatId))
                .ReturnsAsync(false);
            _mockRepository.Setup(r => r.CreateAsync(It.Is<CoupleSeat>(cs => 
                cs.FirstSeatId == 5 && cs.SecondSeatId == 10)))
                .ReturnsAsync(expectedCoupleSeat);

            // Act
            var result = await _service.CreateCoupleSeatAsync(firstSeatId, secondSeatId);

            // Assert
            Assert.Equal(expectedCoupleSeat, result);
            _mockRepository.Verify(r => r.CreateAsync(It.Is<CoupleSeat>(cs => 
                cs.FirstSeatId == 5 && cs.SecondSeatId == 10)), Times.Once);
        }
    }
} 