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
    public class ScheduleSeatServiceTests
    {
        private readonly Mock<IScheduleSeatRepository> _mockRepository;
        private readonly ScheduleSeatService _service;

        public ScheduleSeatServiceTests()
        {
            _mockRepository = new Mock<IScheduleSeatRepository>();
            _service = new ScheduleSeatService(_mockRepository.Object);
        }

        [Fact]
        public async Task CreateScheduleSeatAsync_WithValidScheduleSeat_ReturnsTrue()
        {
            // Arrange
            var scheduleSeat = new ScheduleSeat
            {
                MovieShowId = 1,
                SeatId = 1,
                SeatStatusId = 1,
                BookedPrice = 100000
            };
            _mockRepository.Setup(r => r.CreateScheduleSeatAsync(scheduleSeat))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateScheduleSeatAsync(scheduleSeat);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.CreateScheduleSeatAsync(scheduleSeat), Times.Once);
        }

        [Fact]
        public async Task CreateScheduleSeatAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var scheduleSeat = new ScheduleSeat
            {
                MovieShowId = 1,
                SeatId = 1,
                SeatStatusId = 1,
                BookedPrice = 100000
            };
            _mockRepository.Setup(r => r.CreateScheduleSeatAsync(scheduleSeat))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CreateScheduleSeatAsync(scheduleSeat);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.CreateScheduleSeatAsync(scheduleSeat), Times.Once);
        }

        [Fact]
        public async Task CreateMultipleScheduleSeatsAsync_WithValidScheduleSeats_ReturnsTrue()
        {
            // Arrange
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = 1, SeatId = 1, SeatStatusId = 1 },
                new ScheduleSeat { MovieShowId = 1, SeatId = 2, SeatStatusId = 1 }
            };
            _mockRepository.Setup(r => r.CreateMultipleScheduleSeatsAsync(scheduleSeats))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateMultipleScheduleSeatsAsync(scheduleSeats);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.CreateMultipleScheduleSeatsAsync(scheduleSeats), Times.Once);
        }

        [Fact]
        public async Task CreateMultipleScheduleSeatsAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = 1, SeatId = 1, SeatStatusId = 1 },
                new ScheduleSeat { MovieShowId = 1, SeatId = 2, SeatStatusId = 1 }
            };
            _mockRepository.Setup(r => r.CreateMultipleScheduleSeatsAsync(scheduleSeats))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CreateMultipleScheduleSeatsAsync(scheduleSeats);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.CreateMultipleScheduleSeatsAsync(scheduleSeats), Times.Once);
        }

        [Fact]
        public async Task GetScheduleSeatAsync_WithValidParameters_ReturnsScheduleSeat()
        {
            // Arrange
            var movieShowId = 1;
            var seatId = 1;
            var expectedScheduleSeat = new ScheduleSeat
            {
                MovieShowId = movieShowId,
                SeatId = seatId,
                SeatStatusId = 1,
                BookedPrice = 100000
            };
            _mockRepository.Setup(r => r.GetScheduleSeatAsync(movieShowId, seatId))
                .ReturnsAsync(expectedScheduleSeat);

            // Act
            var result = await _service.GetScheduleSeatAsync(movieShowId, seatId);

            // Assert
            Assert.Equal(expectedScheduleSeat, result);
            _mockRepository.Verify(r => r.GetScheduleSeatAsync(movieShowId, seatId), Times.Once);
        }

        [Fact]
        public async Task GetScheduleSeatAsync_WhenRepositoryReturnsNull_ReturnsNull()
        {
            // Arrange
            var movieShowId = 1;
            var seatId = 1;
            _mockRepository.Setup(r => r.GetScheduleSeatAsync(movieShowId, seatId))
                .ReturnsAsync((ScheduleSeat)null);

            // Act
            var result = await _service.GetScheduleSeatAsync(movieShowId, seatId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetScheduleSeatAsync(movieShowId, seatId), Times.Once);
        }

        [Fact]
        public async Task GetScheduleSeatsByMovieShowAsync_WithValidMovieShowId_ReturnsScheduleSeats()
        {
            // Arrange
            var movieShowId = 1;
            var expectedScheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = movieShowId, SeatId = 1, SeatStatusId = 1 },
                new ScheduleSeat { MovieShowId = movieShowId, SeatId = 2, SeatStatusId = 1 }
            };
            _mockRepository.Setup(r => r.GetScheduleSeatsByMovieShowAsync(movieShowId))
                .ReturnsAsync(expectedScheduleSeats);

            // Act
            var result = await _service.GetScheduleSeatsByMovieShowAsync(movieShowId);

            // Assert
            Assert.Equal(expectedScheduleSeats, result);
            _mockRepository.Verify(r => r.GetScheduleSeatsByMovieShowAsync(movieShowId), Times.Once);
        }

        [Fact]
        public async Task GetScheduleSeatsByMovieShowAsync_WhenRepositoryReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            var movieShowId = 1;
            var expectedScheduleSeats = new List<ScheduleSeat>();
            _mockRepository.Setup(r => r.GetScheduleSeatsByMovieShowAsync(movieShowId))
                .ReturnsAsync(expectedScheduleSeats);

            // Act
            var result = await _service.GetScheduleSeatsByMovieShowAsync(movieShowId);

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetScheduleSeatsByMovieShowAsync(movieShowId), Times.Once);
        }

        [Fact]
        public async Task UpdateSeatStatusAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var movieShowId = 1;
            var seatId = 1;
            var statusId = 2;
            _mockRepository.Setup(r => r.UpdateSeatStatusAsync(movieShowId, seatId, statusId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateSeatStatusAsync(movieShowId, seatId, statusId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.UpdateSeatStatusAsync(movieShowId, seatId, statusId), Times.Once);
        }

        [Fact]
        public async Task UpdateSeatStatusAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
        {
            // Arrange
            var movieShowId = 1;
            var seatId = 1;
            var statusId = 2;
            _mockRepository.Setup(r => r.UpdateSeatStatusAsync(movieShowId, seatId, statusId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateSeatStatusAsync(movieShowId, seatId, statusId);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.UpdateSeatStatusAsync(movieShowId, seatId, statusId), Times.Once);
        }

        [Fact]
        public void GetByInvoiceId_WithValidInvoiceId_ReturnsScheduleSeats()
        {
            // Arrange
            var invoiceId = "INV001";
            var expectedScheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = 1, SeatId = 1, SeatStatusId = 1, InvoiceId = invoiceId },
                new ScheduleSeat { MovieShowId = 1, SeatId = 2, SeatStatusId = 1, InvoiceId = invoiceId }
            };
            _mockRepository.Setup(r => r.GetByInvoiceId(invoiceId))
                .Returns(expectedScheduleSeats);

            // Act
            var result = _service.GetByInvoiceId(invoiceId);

            // Assert
            Assert.Equal(expectedScheduleSeats, result);
            _mockRepository.Verify(r => r.GetByInvoiceId(invoiceId), Times.Once);
        }

        [Fact]
        public void GetByInvoiceId_WhenRepositoryReturnsEmpty_ReturnsEmpty()
        {
            // Arrange
            var invoiceId = "INV001";
            var expectedScheduleSeats = new List<ScheduleSeat>();
            _mockRepository.Setup(r => r.GetByInvoiceId(invoiceId))
                .Returns(expectedScheduleSeats);

            // Act
            var result = _service.GetByInvoiceId(invoiceId);

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetByInvoiceId(invoiceId), Times.Once);
        }

        [Fact]
        public void Update_WithValidScheduleSeat_CallsRepositoryUpdate()
        {
            // Arrange
            var scheduleSeat = new ScheduleSeat
            {
                MovieShowId = 1,
                SeatId = 1,
                SeatStatusId = 1
            };

            // Act
            _service.Update(scheduleSeat);

            // Assert
            _mockRepository.Verify(r => r.Update(scheduleSeat), Times.Once);
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
        public async Task UpdateScheduleSeatsStatusAsync_WithValidParameters_UpdatesAllSeats()
        {
            // Arrange
            var invoiceId = "INV001";
            var statusId = 2;
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = 1, SeatId = 1, SeatStatusId = 1, InvoiceId = invoiceId },
                new ScheduleSeat { MovieShowId = 1, SeatId = 2, SeatStatusId = 1, InvoiceId = invoiceId }
            };
            _mockRepository.Setup(r => r.GetByInvoiceId(invoiceId))
                .Returns(scheduleSeats);

            // Act
            await _service.UpdateScheduleSeatsStatusAsync(invoiceId, statusId);

            // Assert
            _mockRepository.Verify(r => r.GetByInvoiceId(invoiceId), Times.Once);
            _mockRepository.Verify(r => r.Update(It.IsAny<ScheduleSeat>()), Times.Exactly(2));
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public async Task UpdateScheduleSeatsStatusAsync_WithEmptyScheduleSeats_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INV001";
            var statusId = 2;
            var scheduleSeats = new List<ScheduleSeat>();
            _mockRepository.Setup(r => r.GetByInvoiceId(invoiceId))
                .Returns(scheduleSeats);

            // Act
            await _service.UpdateScheduleSeatsStatusAsync(invoiceId, statusId);

            // Assert
            _mockRepository.Verify(r => r.GetByInvoiceId(invoiceId), Times.Once);
            _mockRepository.Verify(r => r.Update(It.IsAny<ScheduleSeat>()), Times.Never);
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public async Task UpdateScheduleSeatsBookedPriceAsync_WithValidParameters_UpdatesAllSeats()
        {
            // Arrange
            var invoiceId = "INV001";
            var bookedPrice = 150000m;
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { MovieShowId = 1, SeatId = 1, BookedPrice = 100000, InvoiceId = invoiceId },
                new ScheduleSeat { MovieShowId = 1, SeatId = 2, BookedPrice = 100000, InvoiceId = invoiceId }
            };
            _mockRepository.Setup(r => r.GetByInvoiceId(invoiceId))
                .Returns(scheduleSeats);

            // Act
            await _service.UpdateScheduleSeatsBookedPriceAsync(invoiceId, bookedPrice);

            // Assert
            _mockRepository.Verify(r => r.GetByInvoiceId(invoiceId), Times.Once);
            _mockRepository.Verify(r => r.Update(It.IsAny<ScheduleSeat>()), Times.Exactly(2));
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public async Task UpdateScheduleSeatsBookedPriceAsync_WithEmptyScheduleSeats_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INV001";
            var bookedPrice = 150000m;
            var scheduleSeats = new List<ScheduleSeat>();
            _mockRepository.Setup(r => r.GetByInvoiceId(invoiceId))
                .Returns(scheduleSeats);

            // Act
            await _service.UpdateScheduleSeatsBookedPriceAsync(invoiceId, bookedPrice);

            // Assert
            _mockRepository.Verify(r => r.GetByInvoiceId(invoiceId), Times.Once);
            _mockRepository.Verify(r => r.Update(It.IsAny<ScheduleSeat>()), Times.Never);
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public async Task UpdateScheduleSeatsToBookedAsync_WithValidParameters_CallsRepository()
        {
            // Arrange
            var invoiceId = "INV001";
            var movieShowId = 1;
            var seatIds = new List<int> { 1, 2, 3 };

            // Act
            await _service.UpdateScheduleSeatsToBookedAsync(invoiceId, movieShowId, seatIds);

            // Assert
            _mockRepository.Verify(r => r.UpdateScheduleSeatsToBookedAsync(invoiceId, movieShowId, seatIds), Times.Once);
        }

        [Fact]
        public async Task UpdateScheduleSeatsToBookedAsync_WithEmptySeatIds_CallsRepository()
        {
            // Arrange
            var invoiceId = "INV001";
            var movieShowId = 1;
            var seatIds = new List<int>();

            // Act
            await _service.UpdateScheduleSeatsToBookedAsync(invoiceId, movieShowId, seatIds);

            // Assert
            _mockRepository.Verify(r => r.UpdateScheduleSeatsToBookedAsync(invoiceId, movieShowId, seatIds), Times.Once);
        }
    }
} 