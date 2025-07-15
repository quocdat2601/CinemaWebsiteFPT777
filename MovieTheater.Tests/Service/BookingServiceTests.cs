using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class BookingServiceTests
    {
        private readonly Mock<IMovieRepository> _repoMock = new();
        private readonly MovieTheaterContext _context;
        private readonly BookingService _svc;

        public BookingServiceTests()
        {
            var opts = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(opts);

            _svc = new BookingService(_repoMock.Object, _context);
        }

        [Fact]
        public async Task GetAvailableMoviesAsync_DelegatesToRepository()
        {
            // Arrange
            var movies = new List<Movie> {
                new() { MovieId = "A" },
                new() { MovieId = "B" }
            };
            _repoMock.Setup(r => r.GetAllMoviesAsync())
                     .ReturnsAsync(movies);

            // Act
            var result = await _svc.GetAvailableMoviesAsync();

            // Assert
            Assert.Same(movies, result);
            _repoMock.Verify(r => r.GetAllMoviesAsync(), Times.Once);
        }

        [Fact]
        public void GetById_DelegatesToRepository()
        {
            // Arrange
            var m = new Movie { MovieId = "X" };
            _repoMock.Setup(r => r.GetById("X"))
                     .Returns(m);

            // Act
            var result = _svc.GetById("X");

            // Assert
            Assert.Equal("X", result.MovieId);
            _repoMock.Verify(r => r.GetById("X"), Times.Once);
        }

        [Fact]
        public void GetSchedulesByIds_DelegatesToRepository()
        {
            // Arrange
            var schedules = new List<Schedule> {
                new() { ScheduleId = 1 },
                new() { ScheduleId = 2 }
            };
            _repoMock.Setup(r => r.GetSchedulesByIds(It.IsAny<List<int>>()))
                     .Returns(schedules);

            var ids = new List<int> { 1, 2, 3 };
            // Act
            var result = _svc.GetSchedulesByIds(ids);

            // Assert
            Assert.Same(schedules, result);
            _repoMock.Verify(r => r.GetSchedulesByIds(ids), Times.Once);
        }

        [Fact]
        public async Task GetShowDatesAsync_DelegatesToRepository()
        {
            // Arrange
            var dates = new List<DateOnly> {
                DateOnly.Parse("2025-07-20"),
                DateOnly.Parse("2025-07-21")
            };
            _repoMock.Setup(r => r.GetShowDatesAsync("M1"))
                     .ReturnsAsync(dates);

            // Act
            var result = await _svc.GetShowDatesAsync("M1");

            // Assert
            Assert.Equal(dates, result);
            _repoMock.Verify(r => r.GetShowDatesAsync("M1"), Times.Once);
        }

        [Fact]
        public async Task GetShowTimesAsync_DelegatesToRepository()
        {
            // Arrange
            var times = new List<string> { "09:00", "12:00" };
            var date = DateTime.Today;
            _repoMock.Setup(r => r.GetShowTimesAsync("M1", date))
                     .ReturnsAsync(times);

            // Act
            var result = await _svc.GetShowTimesAsync("M1", date);

            // Assert
            Assert.Equal(times, result);
            _repoMock.Verify(r => r.GetShowTimesAsync("M1", date), Times.Once);
        }

        [Fact]
        public async Task SaveInvoiceAsync_PersistsInvoice()
        {
            // Arrange
            var invoice = new Invoice { InvoiceId = "I1", TotalMoney = 100m };

            // Act
            await _svc.SaveInvoiceAsync(invoice);

            // Assert
            var saved = await _context.Invoices.FindAsync("I1");
            Assert.NotNull(saved);
            Assert.Equal(100m, saved.TotalMoney);
        }

        [Fact]
        public async Task UpdateInvoiceAsync_UpdatesExistingInvoice()
        {
            // Arrange
            var invoice = new Invoice { InvoiceId = "I2", TotalMoney = 50m };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Act
            invoice.TotalMoney = 75m;
            await _svc.UpdateInvoiceAsync(invoice);

            // Assert
            var updated = await _context.Invoices.FindAsync("I2");
            Assert.Equal(75m, updated.TotalMoney);
        }

        [Fact]
        public async Task GenerateInvoiceIdAsync_Returns001_WhenNoExisting()
        {
            // Arrange
            foreach (var inv in _context.Invoices) _context.Invoices.Remove(inv);
            await _context.SaveChangesAsync();

            // Act
            var next = await _svc.GenerateInvoiceIdAsync();

            // Assert
            Assert.Equal("INV001", next);
        }

        [Fact]
        public async Task GenerateInvoiceIdAsync_IncrementsMaxExisting()
        {
            // Arrange
            _context.Invoices.AddRange(new[]
            {
                new Invoice { InvoiceId = "INV001" },
                new Invoice { InvoiceId = "INV010" },
                new Invoice { InvoiceId = "INV002" },
                new Invoice { InvoiceId = "XXX123" }
            });
            await _context.SaveChangesAsync();

            // Act
            var next = await _svc.GenerateInvoiceIdAsync();

            // Assert
            Assert.Equal("INV011", next);
        }
    }
}
