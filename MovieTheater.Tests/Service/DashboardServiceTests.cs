using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ISeatService> _mockSeatService;
        private readonly Mock<IMemberRepository> _mockMemberRepository;
        private readonly DashboardService _dashboardService;
        private readonly MovieTheaterContext _context;

        public DashboardServiceTests()
        {
            _mockInvoiceService = new Mock<IInvoiceService>();
            _mockSeatService = new Mock<ISeatService>();
            _mockMemberRepository = new Mock<IMemberRepository>();

            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);

            _dashboardService = new DashboardService(_mockInvoiceService.Object, _mockSeatService.Object, _mockMemberRepository.Object, _context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public void GetDashboardViewModel_WithValidData_ReturnsCorrectViewModel()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();
            var testMembers = CreateTestMembers(today);

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(testMembers);

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AdminDashboardViewModel>(result);
            Assert.Equal(1400m, result.RevenueToday); // 2 tickets * 750 each - 100 food cost
            Assert.Equal(2, result.TotalBookings);
            Assert.Equal(2, result.BookingsToday);
            Assert.Equal(2, result.TicketsSoldToday);
            Assert.Equal(20m, result.OccupancyRateToday); // 2 seats / 10 total seats * 100
            Assert.Equal(1400m, result.GrossRevenue);
            Assert.Equal(1400m, result.NetRevenue);
            Assert.Equal(0m, result.TotalVouchersIssued);
            Assert.Equal(0m, result.VouchersToday);
            Assert.Equal(1400m, result.NetRevenueToday);
        }

        [Fact]
        public void GetDashboardViewModel_WithNoData_ReturnsEmptyViewModel()
        {
            // Arrange
            var testSeats = CreateTestSeats();
            _mockInvoiceService.Setup(x => x.GetAll()).Returns(new List<Invoice>());
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AdminDashboardViewModel>(result);
            Assert.Equal(0m, result.RevenueToday);
            Assert.Equal(0, result.TotalBookings);
            Assert.Equal(0, result.BookingsToday);
            Assert.Equal(0, result.TicketsSoldToday);
            Assert.Equal(0m, result.OccupancyRateToday);
            Assert.Equal(0m, result.GrossRevenue);
            Assert.Equal(0m, result.NetRevenue);
            Assert.Equal(0m, result.TotalVouchersIssued);
            Assert.Equal(0m, result.VouchersToday);
            Assert.Equal(0m, result.NetRevenueToday);
        }

        [Fact]
        public void GetDashboardViewModel_WithCancelledInvoices_CalculatesVoucherMetrics()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithCancellations(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1400m, result.GrossRevenue); // 1500 - 100 food cost (1 valid + 1 cancelled)
            Assert.Equal(700m, result.NetRevenue); // Only valid invoices (750 - 50 food cost for valid invoice)
            Assert.Equal(700m, result.TotalVouchersIssued); // Cancelled invoice amount (750 - 50 food cost)
            Assert.Equal(700m, result.VouchersToday); // Cancelled invoice today (750 - 50 food cost)
        }

        [Fact]
        public void GetDashboardViewModel_WithFoodData_CalculatesFoodMetrics()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FoodAnalytics);
            Assert.Equal(100m, result.FoodAnalytics.GrossRevenue); // 2 food items * 50 each
            Assert.Equal(0m, result.FoodAnalytics.VouchersIssued);
            Assert.Equal(100m, result.FoodAnalytics.NetRevenue);
            Assert.Equal(2, result.FoodAnalytics.TotalOrders);
            Assert.Equal(100m, result.FoodAnalytics.GrossRevenueToday);
            Assert.Equal(0m, result.FoodAnalytics.VouchersToday);
            Assert.Equal(100m, result.FoodAnalytics.NetRevenueToday);
            Assert.Equal(2, result.FoodAnalytics.OrdersToday);
            Assert.Equal(2, result.FoodAnalytics.QuantitySoldToday);
            Assert.Equal(50m, result.FoodAnalytics.AvgOrderValueToday);
        }

        [Fact]
        public void GetDashboardViewModel_WithMemberData_CalculatesMemberMetrics()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();
            var testMembers = CreateTestMembers(today);

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(testMembers);

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.RecentMembers);
            Assert.Single(result.RecentMembers);
            Assert.Equal("Test User 1", result.RecentMembers[0].FullName);
            Assert.Equal("test1@example.com", result.RecentMembers[0].Email);
        }

        [Fact]
        public void GetDashboardViewModel_WithTopPerformers_CalculatesTopLists()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesForTopPerformers(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TopMovies);
            Assert.NotNull(result.TopMembers);
            Assert.NotEmpty(result.TopMovies);
            Assert.NotEmpty(result.TopMembers);
            Assert.Equal("Test Movie 1", result.TopMovies[0].MovieName);
            Assert.Equal(3, result.TopMovies[0].TicketsSold); // 2 seats + 1 seat = 3 total
            Assert.Equal("Test User 1", result.TopMembers[0].MemberName);
            Assert.Equal(2, result.TopMembers[0].Bookings);
        }

        [Fact]
        public void GetDashboardViewModel_WithRecentActivities_CalculatesActivities()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesForRecentActivities(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.MovieAnalytics);
            Assert.NotNull(result.MovieAnalytics.RecentBookings);
            Assert.NotNull(result.MovieAnalytics.RecentCancellations);
            Assert.NotEmpty(result.MovieAnalytics.RecentBookings);
            Assert.NotEmpty(result.MovieAnalytics.RecentCancellations);
            Assert.Equal("INV001", result.MovieAnalytics.RecentBookings[0].InvoiceId);
            Assert.Equal("Test User 1", result.MovieAnalytics.RecentBookings[0].MemberName);
            Assert.Equal("Test Movie 1", result.MovieAnalytics.RecentBookings[0].MovieName);
        }

        [Fact]
        public void GetDashboardViewModel_WithTrendData_CalculatesTrends()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesForTrends(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.RevenueTrendDates);
            Assert.NotNull(result.RevenueTrendValues);
            Assert.NotNull(result.BookingTrendValues);
            Assert.NotNull(result.VoucherTrendValues);
            Assert.Equal(7, result.RevenueTrendDates.Count);
            Assert.Equal(7, result.RevenueTrendValues.Count);
            Assert.Equal(7, result.BookingTrendValues.Count);
            Assert.Equal(7, result.VoucherTrendValues.Count);
        }

        [Fact]
        public void GetDashboardViewModel_WithNullValues_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithNullValues(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.RevenueToday);
            Assert.Equal(0, result.TotalBookings);
            Assert.Equal(0, result.BookingsToday);
            Assert.Equal(0, result.TicketsSoldToday);
        }

        [Fact]
        public void GetDashboardViewModel_WithEmptySeats_CalculatesZeroOccupancy()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = new List<Seat>(); // Empty seats

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.OccupancyRateToday); // Should be 0 when no seats exist
        }

        [Fact]
        public void GetDashboardViewModel_WithFoodAnalytics_CalculatesFoodAnalytics()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FoodAnalytics);
            Assert.NotNull(result.FoodAnalytics.TopFoodItems);
            Assert.NotNull(result.FoodAnalytics.SalesByCategory);
            Assert.NotNull(result.FoodAnalytics.SalesByHour);
            Assert.NotNull(result.FoodAnalytics.RecentOrders);
            Assert.NotNull(result.FoodAnalytics.RecentCancels);
            Assert.NotEmpty(result.FoodAnalytics.TopFoodItems);
            Assert.Equal(16, result.FoodAnalytics.SalesByHour.Count); // 8 AM to 11 PM (16 hours)
        }

        [Fact]
        public void GetDashboardViewModel_WithNullTotalMoney_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithNullTotalMoney(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.RevenueToday); // 0 - 0 food cost = 0 (no food invoices)
            Assert.Equal(0m, result.GrossRevenue);
            Assert.Equal(0m, result.NetRevenue);
        }

        [Fact]
        public void GetDashboardViewModel_WithNullBookingDate_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithNullBookingDate(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.BookingsToday); // Should be 0 when BookingDate is null
            Assert.Equal(0, result.TicketsSoldToday);
        }

        [Fact]
        public void GetDashboardViewModel_WithNullSeat_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithNullSeat(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TicketsSoldToday); // Should be 0 when Seat is null
        }

        [Fact]
        public void GetDashboardViewModel_WithNullAccount_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithNullAccount(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TopMembers);
            Assert.Empty(result.TopMembers); // Should be empty when Account is null
        }

        [Fact]
        public void GetDashboardViewModel_WithNullMovieShow_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesWithNullMovieShow(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TopMovies);
            Assert.Empty(result.TopMovies); // Should be empty when MovieShow is null
            Assert.NotNull(result.TopMembers);
            Assert.Empty(result.TopMembers); // Should be empty when Account is null
        }

        [Fact]
        public void GetDashboardViewModel_WithZeroOrders_CalculatesZeroAverageOrderValue()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testSeats = CreateTestSeats();
            // No food invoices - zero orders

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FoodAnalytics);
            Assert.Equal(0m, result.FoodAnalytics.AvgOrderValueToday); // Should be 0 when no orders
        }

        [Fact]
        public void GetDashboardViewModel_WithDifferentDaysParameter_ReturnsCorrectData()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoicesForTrends(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel(14); // 14 days instead of default 7

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.RevenueTrendDates);
            Assert.NotNull(result.RevenueTrendValues);
            Assert.Equal(14, result.RevenueTrendDates.Count);
            Assert.Equal(14, result.RevenueTrendValues.Count);
        }

        [Fact]
        public void GetDashboardViewModel_WithNullMemberAccount_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoices(today);
            var testSeats = CreateTestSeats();
            var testMembers = CreateTestMembersWithNullAccount(today);

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(testMembers);

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.RecentMembers);
            Assert.Empty(result.RecentMembers); // Should be empty when Account is null
        }

        [Fact]
        public void GetDashboardViewModel_WithFoodInvoicesWithoutInvoice_HandlesGracefully()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestFoodInvoicesWithoutInvoice(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FoodAnalytics);
            Assert.Equal(0m, result.FoodAnalytics.GrossRevenue); // Should be 0 when Invoice is null
        }

        [Fact]
        public void GetDashboardViewModel_WithCancelledFoodInvoices_CalculatesVoucherMetrics()
        {
            // Arrange
            var today = DateTime.Today;
            var testInvoices = CreateTestInvoices(today);
            var testFoodInvoices = CreateTestCancelledFoodInvoices(today);
            var testSeats = CreateTestSeats();

            _mockInvoiceService.Setup(x => x.GetAll()).Returns(testInvoices);
            _mockSeatService.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(testSeats);
            _mockMemberRepository.Setup(x => x.GetAll()).Returns(new List<Member>());

            _context.FoodInvoices.AddRange(testFoodInvoices);
            _context.SaveChanges();

            // Act
            var result = _dashboardService.GetDashboardViewModel();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FoodAnalytics);
            Assert.Equal(100m, result.FoodAnalytics.VouchersIssued); // 2 cancelled food items * 50 each
            Assert.Equal(100m, result.FoodAnalytics.VouchersToday);
        }

        // Helper methods for creating test data
        private List<Invoice> CreateTestInvoices(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A2",
                    Account = new Account { FullName = "Test User 2", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 2" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithCancellations(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = true,
                    CancelDate = today,
                    Seat = "A2",
                    Account = new Account { FullName = "Test User 2", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 2" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesForTrends(DateTime today)
        {
            var invoices = new List<Invoice>();
            for (int i = 0; i < 7; i++)
            {
                invoices.Add(new Invoice
                {
                    InvoiceId = $"INV{i:D3}",
                    TotalMoney = 750m,
                    BookingDate = today.AddDays(-i),
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                });
            }
            return invoices;
        }

        private List<Invoice> CreateTestInvoicesForTopPerformers(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1,A2",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A3",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesForRecentActivities(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = true,
                    CancelDate = today,
                    Seat = "A2",
                    Account = new Account { FullName = "Test User 2", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 2" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithNullValues(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = null,
                    BookingDate = null,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = null,
                    Account = null,
                    MovieShow = null
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithNullTotalMoney(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = null,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithNullBookingDate(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = null,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithNullSeat(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = null,
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithNullAccount(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today,
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = null,
                    MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 1" } }
                }
            };
        }

        private List<Invoice> CreateTestInvoicesWithNullMovieShow(DateTime today)
        {
            return new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    TotalMoney = 750m,
                    BookingDate = today.AddDays(-10), // Old invoice to avoid GetTopPerformers
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    Seat = "A1",
                    Account = new Account { FullName = "Test User 1", RoleId = 3 },
                    MovieShow = null
                }
            };
        }

        private List<FoodInvoice> CreateTestFoodInvoices(DateTime today)
        {
            return new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = "INV001",
                    FoodId = 1,
                    Price = 50m,
                    Quantity = 1,
                    Food = new Food { Name = "Popcorn", Category = "Snacks" },
                    Invoice = new Invoice 
                    { 
                        InvoiceId = "INV001",
                        BookingDate = today, 
                        Status = InvoiceStatus.Completed, 
                        Cancel = false 
                    }
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 2,
                    InvoiceId = "INV002",
                    FoodId = 2,
                    Price = 50m,
                    Quantity = 1,
                    Food = new Food { Name = "Soda", Category = "Drinks" },
                    Invoice = new Invoice 
                    { 
                        InvoiceId = "INV002",
                        BookingDate = today, 
                        Status = InvoiceStatus.Completed, 
                        Cancel = false 
                    }
                }
            };
        }

        private List<FoodInvoice> CreateTestFoodInvoicesWithoutInvoice(DateTime today)
        {
            return new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = "INV001",
                    FoodId = 1,
                    Price = 50m,
                    Quantity = 1,
                    Food = new Food { Name = "Popcorn", Category = "Snacks" },
                    Invoice = null
                }
            };
        }

        private List<FoodInvoice> CreateTestCancelledFoodInvoices(DateTime today)
        {
            return new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = "INV001",
                    FoodId = 1,
                    Price = 50m,
                    Quantity = 1,
                    Food = new Food { Name = "Popcorn", Category = "Snacks" },
                    Invoice = new Invoice 
                    { 
                        InvoiceId = "INV001",
                        BookingDate = today, 
                        Status = InvoiceStatus.Completed, 
                        Cancel = true,
                        CancelDate = today
                    }
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 2,
                    InvoiceId = "INV002",
                    FoodId = 2,
                    Price = 50m,
                    Quantity = 1,
                    Food = new Food { Name = "Soda", Category = "Drinks" },
                    Invoice = new Invoice 
                    { 
                        InvoiceId = "INV002",
                        BookingDate = today, 
                        Status = InvoiceStatus.Completed, 
                        Cancel = true,
                        CancelDate = today
                    }
                }
            };
        }

        private List<Seat> CreateTestSeats()
        {
            return new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1" },
                new Seat { SeatId = 2, SeatName = "A2" },
                new Seat { SeatId = 3, SeatName = "A3" },
                new Seat { SeatId = 4, SeatName = "A4" },
                new Seat { SeatId = 5, SeatName = "A5" },
                new Seat { SeatId = 6, SeatName = "B1" },
                new Seat { SeatId = 7, SeatName = "B2" },
                new Seat { SeatId = 8, SeatName = "B3" },
                new Seat { SeatId = 9, SeatName = "B4" },
                new Seat { SeatId = 10, SeatName = "B5" }
            };
        }

        private List<Member> CreateTestMembers(DateTime today)
        {
            return new List<Member>
            {
                new Member
                {
                    MemberId = "M001",
                    AccountId = "A001",
                    Account = new Account
                    {
                        AccountId = "A001",
                        FullName = "Test User 1",
                        Email = "test1@example.com",
                        RegisterDate = DateOnly.FromDateTime(today)
                    }
                }
            };
        }

        private List<Member> CreateTestMembersWithNullAccount(DateTime today)
        {
            return new List<Member>
            {
                new Member
                {
                    MemberId = "M001",
                    AccountId = "A001",
                    Account = null
                }
            };
        }
    }
} 