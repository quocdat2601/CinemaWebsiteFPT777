using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class InvoiceRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly InvoiceRepository _repository;

        public InvoiceRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new InvoiceRepository(_context);
            SeedData();
        }

        private void SeedData()
        {
            var accounts = new List<Account>
            {
                new Account { AccountId = "acc1", Username = "user1", Email = "user1@test.com" },
                new Account { AccountId = "acc2", Username = "user2", Email = "user2@test.com" }
            };

            var employees = new List<Employee>
            {
                new Employee { EmployeeId = "emp1", AccountId = "acc1", Status = true }
            };

            var movies = new List<Movie>
            {
                new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie 1" },
                new Movie { MovieId = "MV002", MovieNameEnglish = "Test Movie 2" }
            };

            var cinemaRooms = new List<CinemaRoom>
            {
                new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1", StatusId = 1 }
            };

            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(14, 0) }
            };

            var versions = new List<MovieTheater.Models.Version>
            {
                new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" }
            };

            var movieShows = new List<MovieShow>
            {
                new MovieShow 
                { 
                    MovieShowId = 1, 
                    MovieId = "MV001", 
                    CinemaRoomId = 1, 
                    ScheduleId = 1, 
                    VersionId = 1,
                    Movie = movies[0],
                    CinemaRoom = cinemaRooms[0],
                    Schedule = schedules[0],
                    Version = versions[0]
                }
            };

            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FF0000" }
            };

            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", CinemaRoomId = 1, SeatTypeId = 1, SeatType = seatTypes[0] }
            };

            var invoices = new List<Invoice>
            {
                new Invoice 
                { 
                    InvoiceId = "INV001", 
                    AccountId = "acc1", 
                    EmployeeId = "emp1",
                    MovieShowId = 1,
                    TotalMoney = 100000,
                    Status = InvoiceStatus.Incomplete,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = movieShows[0],
                    Account = accounts[0],
                    Employee = employees[0]
                },
                new Invoice 
                { 
                    InvoiceId = "INV002", 
                    AccountId = "acc1", 
                    EmployeeId = "emp1",
                    MovieShowId = 1,
                    TotalMoney = 150000,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-2),
                    MovieShow = movieShows[0],
                    Account = accounts[0],
                    Employee = employees[0]
                },
                new Invoice 
                { 
                    InvoiceId = "INV003", 
                    AccountId = "acc2", 
                    EmployeeId = "emp1",
                MovieShowId = 1,
                    TotalMoney = 200000,
                    Status = InvoiceStatus.Incomplete,
                    Cancel = true,
                    BookingDate = DateTime.Now.AddDays(-3),
                    MovieShow = movieShows[0],
                    Account = accounts[1],
                    Employee = employees[0]
                }
            };

            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat 
                { 
                    ScheduleSeatId = 1, 
                    InvoiceId = "INV001", 
                    SeatId = 1,
                    Seat = seats[0]
                }
            };

            var vouchers = new List<Voucher>
            {
                new Voucher 
                { 
                    VoucherId = "1", 
                    AccountId = "acc1", 
                    Code = "VOUCHER001",
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(30)
                }
            };

            _context.Accounts.AddRange(accounts);
            _context.Employees.AddRange(employees);
            _context.Movies.AddRange(movies);
            _context.CinemaRooms.AddRange(cinemaRooms);
            _context.Schedules.AddRange(schedules);
            _context.Versions.AddRange(versions);
            _context.MovieShows.AddRange(movieShows);
            _context.SeatTypes.AddRange(seatTypes);
            _context.Seats.AddRange(seats);
            _context.Invoices.AddRange(invoices);
            _context.ScheduleSeats.AddRange(scheduleSeats);
            _context.Vouchers.AddRange(vouchers);
            _context.SaveChanges();
        }

        [Fact]
        public void GetAll_ReturnsAllInvoicesWithIncludes()
        {
            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            var firstInvoice = result.First();
            Assert.NotNull(firstInvoice.Account);
            Assert.NotNull(firstInvoice.Employee);
            Assert.NotNull(firstInvoice.MovieShow);
            Assert.NotNull(firstInvoice.MovieShow.Movie);
            Assert.NotNull(firstInvoice.MovieShow.CinemaRoom);
            Assert.NotNull(firstInvoice.MovieShow.Schedule);
            Assert.NotNull(firstInvoice.MovieShow.Version);
        }

        [Fact]
        public void GetById_WithValidId_ReturnsInvoiceWithIncludes()
        {
            // Act
            var result = _repository.GetById("INV001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("INV001", result.InvoiceId);
            Assert.NotNull(result.Account);
            Assert.NotNull(result.Employee);
            Assert.NotNull(result.MovieShow);
            Assert.NotNull(result.MovieShow.Movie);
            Assert.NotNull(result.Account.Members);
        }

        [Fact]
        public void GetById_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = _repository.GetById("INVALID");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetById_WithNullId_ReturnsNull()
        {
            // Act
            var result = _repository.GetById(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithValidAccountId_ReturnsInvoices()
        {
            // Act
            var result = await _repository.GetByAccountIdAsync("acc1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, invoice => Assert.Equal("acc1", invoice.AccountId));
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithStatusFilter_ReturnsFilteredInvoices()
        {
            // Act
            var result = await _repository.GetByAccountIdAsync("acc1", InvoiceStatus.Incomplete);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(InvoiceStatus.Incomplete, result.First().Status);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithCancelFilter_ReturnsFilteredInvoices()
        {
            // Act
            var result = await _repository.GetByAccountIdAsync("acc1", null, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, invoice => Assert.False(invoice.Cancel));
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithBothFilters_ReturnsFilteredInvoices()
        {
            // Act
            var result = await _repository.GetByAccountIdAsync("acc1", InvoiceStatus.Incomplete, false);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(InvoiceStatus.Incomplete, result.First().Status);
            Assert.False(result.First().Cancel);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WithInvalidAccountId_ReturnsEmpty()
        {
            // Act
            var result = await _repository.GetByAccountIdAsync("invalid");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByDateRangeAsync_WithValidRange_ReturnsFilteredInvoices()
        {
            // Arrange
            var fromDate = DateTime.Now.AddDays(-2);
            var toDate = DateTime.Now.AddDays(-1);

            // Act
            var result = await _repository.GetByDateRangeAsync("acc1", fromDate, toDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count()); // Only 1 invoice in the date range
            Assert.All(result, invoice => 
            {
                Assert.True(invoice.BookingDate >= fromDate);
                Assert.True(invoice.BookingDate <= toDate);
            });
        }

        [Fact]
        public async Task GetByDateRangeAsync_WithFromDateOnly_ReturnsFilteredInvoices()
        {
            // Arrange
            var fromDate = DateTime.Now.AddDays(-2);

            // Act
            var result = await _repository.GetByDateRangeAsync("acc1", fromDate, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count()); // Only 1 invoice from the date
            Assert.All(result, invoice => Assert.True(invoice.BookingDate >= fromDate));
        }

        [Fact]
        public async Task GetByDateRangeAsync_WithToDateOnly_ReturnsFilteredInvoices()
        {
            // Arrange
            var toDate = DateTime.Now.AddDays(-1);

            // Act
            var result = await _repository.GetByDateRangeAsync("acc1", null, toDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, invoice => Assert.True(invoice.BookingDate <= toDate));
        }

        [Fact]
        public async Task GetByDateRangeAsync_WithNoDates_ReturnsAllInvoices()
        {
            // Act
            var result = await _repository.GetByDateRangeAsync("acc1", null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetDetailsAsync_WithValidIds_ReturnsInvoiceWithDetails()
        {
            // Act
            var result = await _repository.GetDetailsAsync("INV001", "acc1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("INV001", result.InvoiceId);
            Assert.Equal("acc1", result.AccountId);
            Assert.NotNull(result.MovieShow);
            Assert.NotNull(result.MovieShow.Movie);
            Assert.NotNull(result.ScheduleSeats);
            Assert.Single(result.ScheduleSeats);
        }

        [Fact]
        public async Task GetDetailsAsync_WithInvalidInvoiceId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetDetailsAsync("INVALID", "acc1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDetailsAsync_WithInvalidAccountId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetDetailsAsync("INV001", "invalid");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetForCancelAsync_WithValidIds_ReturnsInvoice()
        {
            // Act
            var result = await _repository.GetForCancelAsync("INV001", "acc1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("INV001", result.InvoiceId);
            Assert.Equal("acc1", result.AccountId);
            Assert.NotNull(result.MovieShow);
            Assert.NotNull(result.MovieShow.Schedule);
        }

        [Fact]
        public async Task GetForCancelAsync_WithInvalidIds_ReturnsNull()
        {
            // Act
            var result = await _repository.GetForCancelAsync("INVALID", "invalid");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_WithValidInvoice_UpdatesSuccessfully()
        {
            // Arrange
            var invoice = _repository.GetById("INV001");
            invoice.TotalMoney = 200000;

            // Act
            await _repository.UpdateAsync(invoice);

            // Assert
            var updatedInvoice = _repository.GetById("INV001");
            Assert.Equal(200000, updatedInvoice.TotalMoney);
        }

        [Fact]
        public void Update_WithValidInvoice_SetsModifiedState()
        {
            // Arrange
            var invoice = _repository.GetById("INV001");
            invoice.TotalMoney = 200000;

            // Act
            _repository.Update(invoice);

            // Assert
            var entry = _context.Entry(invoice);
            Assert.Equal(EntityState.Modified, entry.State);
        }

        [Fact]
        public void Save_SavesChanges()
        {
            // Arrange
            var invoice = _repository.GetById("INV001");
            var originalMoney = invoice.TotalMoney;
            invoice.TotalMoney = 200000;
            _repository.Update(invoice);

            // Act
            _repository.Save();

            // Assert
            var updatedInvoice = _repository.GetById("INV001");
            Assert.Equal(200000, updatedInvoice.TotalMoney);
        }

        [Fact]
        public void FindInvoiceByOrderId_WithExactMatch_ReturnsInvoice()
        {
            // Act
            var result = _repository.FindInvoiceByOrderId("INV001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("INV001", result.InvoiceId);
        }

        [Fact]
        public void FindInvoiceByOrderId_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = _repository.FindInvoiceByOrderId("INVALID");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithValidAmount_ReturnsInvoice()
        {
            // Act
            var result = _repository.FindInvoiceByAmountAndTime(100000);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100000, result.TotalMoney);
            Assert.NotEqual(InvoiceStatus.Completed, result.Status);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithAmountAndTime_ReturnsFilteredInvoice()
        {
            // Arrange
            var recentTime = DateTime.Now.AddDays(-2);

            // Act
            var result = _repository.FindInvoiceByAmountAndTime(100000, recentTime);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100000, result.TotalMoney);
            Assert.True(result.BookingDate >= recentTime);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithInvalidAmount_ReturnsNull()
        {
            // Act
            var result = _repository.FindInvoiceByAmountAndTime(999999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithCompletedInvoice_ReturnsNull()
        {
            // Act
            var result = _repository.FindInvoiceByAmountAndTime(150000);

            // Assert
            Assert.Null(result); // Should return null because status is Completed
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithZeroAmount_ReturnsNull()
        {
            // Act
            var result = _repository.FindInvoiceByAmountAndTime(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithNegativeAmount_ReturnsNull()
        {
            // Act
            var result = _repository.FindInvoiceByAmountAndTime(-100);

            // Assert
            Assert.Null(result);
        }
    }
} 