using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ModelVersion = MovieTheater.Models.Version;

namespace MovieTheater.Tests.Service
{
    public class BookingDomainServiceTests
    {
        // SUT + its dependencies as mocks
        private readonly Mock<IBookingService> _bookingService = new();
        private readonly Mock<IMovieService> _movieService = new();
        private readonly Mock<ISeatService> _seatService = new();
        private readonly Mock<IAccountService> _accountService = new();
        private readonly Mock<IPromotionService> _promoService = new();
        private readonly Mock<IFoodService> _foodService = new();
        private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
        private readonly Mock<IVoucherService> _voucherService = new();
        private readonly MovieTheaterContext _context = InMemoryDb.Create();
        private readonly Mock<IBookingPriceCalculationService> _priceCalc = new();
        private readonly Mock<ISeatTypeService> _seatTypeSvc = new();

        private readonly BookingDomainService _svc;

        public BookingDomainServiceTests()
        {
            // Use an in-memory EF Core context for anything that uses _context
            _svc = new BookingDomainService(
                _bookingService.Object,
                _movieService.Object,
                _seatService.Object,
                _accountService.Object,
                _seatTypeSvc.Object,
                _promoService.Object,
                _foodService.Object,
                _invoiceRepo.Object,
                _context,
                _priceCalc.Object,
                _voucherService.Object
            );
        }

        private static MovieShow MakeShow(int id, DateOnly date, TimeOnly time)
            => new MovieShow
            {
                MovieShowId = id,
                ShowDate = date,
                Schedule = new Schedule { ScheduleTime = time },
                Version = new ModelVersion { VersionName = "STD", Multi = 1 },
                CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" },
                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" }
            };

        [Fact]
        public async Task BuildConfirmBookingViewModelAsync_ReturnsNull_WhenMovieNotFound()
        {
            // Arrange
            _bookingService.Setup(x => x.GetById("nope")).Returns((Movie)null);

            // Act
            var vm = await _svc.BuildConfirmBookingViewModelAsync(
                movieId: "nope",
                showDate: default,
                showTime: "00:00",
                selectedSeatIds: new List<int>(),
                movieShowId: 1,
                foodIds: null,
                foodQtys: null,
                userId: "u1"
            );

            // Assert
            Assert.Null(vm);
        }

        [Fact]
        public async Task BuildConfirmBookingViewModelAsync_HappyPath_PopulatesBasicFields()
        {
            // Arrange
            var user = new Account { AccountId = "u1", FullName = "Jane", Email = "j@x", IdentityCard = "ID1", PhoneNumber = "555", Rank = null };
            var show = MakeShow(id: 42, date: DateOnly.FromDateTime(DateTime.Today), time: new TimeOnly(14, 0));
            var seatEntity = new Seat { SeatId = 5, SeatName = "A1", SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } };

            _bookingService.Setup(x => x.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test" });
            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow> { show });
            _seatService.Setup(x => x.GetSeatById(5)).Returns(seatEntity);
            _seatService.Setup(x => x.GetSeatTypesAsync())
                          .ReturnsAsync(new List<SeatType> { seatEntity.SeatType });
            _accountService.Setup(x => x.GetById("u1")).Returns(user);
            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>()))
                         .Returns((Promotion)null);

            // Act
            var vm = await _svc.BuildConfirmBookingViewModelAsync(
                movieId: "M1",
                showDate: DateOnly.FromDateTime(DateTime.Today),
                showTime: "14:00",
                selectedSeatIds: new List<int> { 5 },
                movieShowId: 42,
                foodIds: null,
                foodQtys: null,
                userId: "u1"
            );

            // Assert
            Assert.NotNull(vm);
            Assert.Equal("Test", vm.MovieName);
            Assert.Equal("R1", vm.CinemaRoomName);
            Assert.Single(vm.SelectedSeats);
            Assert.Equal(200m, vm.SelectedSeats[0].Price); // Fix: expect 200m
            Assert.Equal("Jane", vm.FullName);
            Assert.Equal(0, vm.TotalFoodPrice);
            Assert.Equal(vm.Subtotal, vm.TotalPrice);
        }

        [Fact]
        public async Task ConfirmBookingAsync_ReturnsFalse_OnInvalidModel()
        {
            // Act
            var result = await _svc.ConfirmBookingAsync(model: null, userId: "", isTestSuccess: "true");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid booking data.", result.ErrorMessage);
        }

        [Fact]
        public async Task ConfirmBookingAsync_HappyPath_CreatesInvoiceAndReturnsSuccess()
        {
            // Arrange
            var vm = new ConfirmBookingViewModel
            {
                MovieShowId = 99,
                SelectedSeats = new List<SeatDetailViewModel> {
                    new() { SeatId=7, Price=120m }
                }
            };
            var user = new Account { AccountId = "u1", Rank = null };
            var show = new MovieShow { MovieShowId = 99, MovieId = "M1", Version = new ModelVersion(), Schedule = new Schedule() }; // Fix: set MovieId

            _accountService.Setup(x => x.GetById("u1")).Returns(user);
            _context.MovieShows.Add(show);
            await _context.SaveChangesAsync();

            // prices
            _priceCalc.Setup(x => x.CalculatePrice(
                    It.IsAny<List<SeatDetailViewModel>>(),
                    It.IsAny<MovieShow>(),
                    user,
                    It.IsAny<decimal?>(),
                    It.IsAny<int?>(),
                    It.IsAny<List<Food>>()
                ))
                .Returns(new BookingPriceResult
                {
                    SeatTotalAfterDiscounts = 120m,
                    Subtotal = 120m,
                    UseScore = 0,
                    AddScore = 0
                });

            _bookingService.Setup(x => x.GenerateInvoiceIdAsync()).ReturnsAsync("INV1");

            // Act
            var result = await _svc.ConfirmBookingAsync(vm, userId: "u1", isTestSuccess: "true");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("INV1", result.InvoiceId);

            // Verify invoice persisted
            var saved = _context.Invoices.FirstOrDefault(i => i.InvoiceId == "INV1");
            Assert.NotNull(saved);
            Assert.Equal(120m, saved.TotalMoney);
        }

        [Fact]
        public async Task BuildSuccessViewModelAsync_ReturnsNull_WhenInvoiceNotFound()
        {
            // Act
            var vm = await _svc.BuildSuccessViewModelAsync("bad", "u1");

            Assert.Null(vm);
        }

        [Fact]
        public async Task BuildSuccessViewModelAsync_HappyPath_PopulatesViewModel()
        {
            // Arrange
            var account = new Account { AccountId = "u1", Email = "e@x" };
            var expectedDate = new DateOnly(2025, 7, 15);
            var show = new MovieShow
            {
                MovieShowId = 5,
                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "X" },
                ShowDate = expectedDate, // Set expected date
                CinemaRoom = new CinemaRoom { CinemaRoomName = "C" },
                Version = new ModelVersion { VersionName = "V1" },
                Schedule = new Schedule { ScheduleTime = new TimeOnly(9, 0) }
            };
            var inv = new Invoice
            {
                InvoiceId = "I1",
                AccountId = "u1",
                MovieShowId = 5,
                TotalMoney = 200m,
                UseScore = 0,
                AddScore = 10,
                PromotionDiscount = 0,
                Status = InvoiceStatus.Completed,
                BookingDate = DateTime.Today
            };
            _accountService.Setup(x => x.GetById("u1")).Returns(account);
            _context.MovieShows.Add(show);
            _context.Invoices.Add(inv);
            _context.ScheduleSeats.Add(new ScheduleSeat { InvoiceId = "I1", SeatId = 7, MovieShowId = 5 });
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 200, ColorHex = "#FFFFFF" };
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(new Seat { SeatId = 7, SeatName = "A7", SeatTypeId = 1, SeatType = seatType }); // Add seat entity with type
            await _context.SaveChangesAsync();

            // Act
            var vm = await _svc.BuildSuccessViewModelAsync("I1", "u1");

            // Assert
            Assert.NotNull(vm);
            Assert.Equal("X", vm.BookingDetails.MovieName);
            Assert.Equal(expectedDate, vm.BookingDetails.ShowDate); // Assert against expected date
            Assert.Contains("A7", vm.BookingDetails.SelectedSeats.Select(s => s.SeatName)); // Assert seat name
            Assert.Equal(200m, vm.Subtotal + vm.TotalFoodPrice);
        }
    }

    // Helper to create an in‑memory DbContext
    static class InMemoryDb
    {
        public static MovieTheaterContext Create()
        {
            var opts = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new MovieTheaterContext(opts);
        }
    }
}
