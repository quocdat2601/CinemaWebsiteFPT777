//using Microsoft.EntityFrameworkCore;
//using Moq;
//using MovieTheater.Models;
//using MovieTheater.Repository;
//using MovieTheater.Service;
//using MovieTheater.ViewModels;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Xunit;
//using ModelVersion = MovieTheater.Models.Version;

//using Moq;
//using MovieTheater.Models;
//using MovieTheater.Service;
//using MovieTheater.Tests.Controller;

//namespace MovieTheater.Tests.Service
//{
//    public class BookingDomainServiceTests
//    {
//        // SUT + its dependencies as mocks
//        private readonly Mock<IBookingService> _bookingService = new();
//        private readonly Mock<IMovieService> _movieService = new();
//        private readonly Mock<ISeatService> _seatService = new();
//        private readonly Mock<IAccountService> _accountService = new();
//        private readonly Mock<IPromotionService> _promoService = new();
//        private readonly Mock<IFoodService> _foodService = new();
//        private readonly Mock<IVoucherService> _voucherService = new();
//        private readonly MovieTheaterContext _context = InMemoryDb.Create();
//        private readonly Mock<IBookingPriceCalculationService> _priceCalc = new();
//        private readonly Mock<ISeatTypeService> _seatTypeSvc = new();
//        private BookingDomainService _svc;

//        //        private readonly BookingDomainService _svc;

//        public BookingDomainServiceTests()
//        {
//            // Use an in-memory EF Core context for anything that uses _context
//            _svc = new BookingDomainService(
//                _bookingService.Object,
//                _movieService.Object,
//                _seatService.Object,
//                _accountService.Object,
//                _seatTypeSvc.Object,
//                _promoService.Object,
//                _foodService.Object,
//                _context,
//                _priceCalc.Object,
//                _voucherService.Object
//            );
//        }

//        private static MovieShow MakeShow(int id, DateOnly date, TimeOnly time)
//            => new MovieShow
//            {
//                MovieShowId = id,
//                ShowDate = date,
//                Schedule = new Schedule { ScheduleTime = time },
//                Version = new ModelVersion { VersionName = "STD", Multi = 1 },
//                CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" },
//                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" }
//            };

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_ReturnsNull_WhenMovieNotFound()
//        {
//            // Arrange
//            _bookingService.Setup(x => x.GetById("nope")).Returns((Movie)null);

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync(
//                movieId: "nope",
//                showDate: default,
//                showTime: "00:00",
//                selectedSeatIds: new List<int>(),
//                movieShowId: 1,
//                foodIds: null,
//                foodQtys: null,
//                userId: "u1"
//            );

//            // Assert
//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_HappyPath_PopulatesBasicFields()
//        {
//            // Arrange
//            var user = new Account { AccountId = "u1", FullName = "Jane", Email = "j@x", IdentityCard = "ID1", PhoneNumber = "555", Rank = null };
//            var show = MakeShow(id: 42, date: DateOnly.FromDateTime(DateTime.Today), time: new TimeOnly(14, 0));
//            var seatEntity = new Seat { SeatId = 5, SeatName = "A1", SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } };

//            _bookingService.Setup(x => x.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test" });
//            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow> { show });
//            _seatService.Setup(x => x.GetSeatById(5)).Returns(seatEntity);
//            _seatService.Setup(x => x.GetSeatTypesAsync())
//                          .ReturnsAsync(new List<SeatType> { seatEntity.SeatType });
//            _accountService.Setup(x => x.GetById("u1")).Returns(user);
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>()))
//                         .Returns((Promotion)null);

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync(
//                movieId: "M1",
//                showDate: DateOnly.FromDateTime(DateTime.Today),
//                showTime: "14:00",
//                selectedSeatIds: new List<int> { 5 },
//                movieShowId: 42,
//                foodIds: null,
//                foodQtys: null,
//                userId: "u1"
//            );

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Equal("Test", vm.MovieName);
//            Assert.Equal("R1", vm.CinemaRoomName);
//            Assert.Single(vm.SelectedSeats);
//            Assert.Equal(200m, vm.SelectedSeats[0].Price); // Fix: expect 200m
//            Assert.Equal("Jane", vm.FullName);
//            Assert.Equal(0, vm.TotalFoodPrice);
//            Assert.Equal(vm.Subtotal, vm.TotalPrice);
//        }

//        [Fact]
//        public async Task ConfirmBookingAsync_ReturnsFalse_OnInvalidModel()
//        {
//            // Act
//            var result = await _svc.ConfirmBookingAsync(model: null, userId: "", isTestSuccess: "true");

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Invalid booking data.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmBookingAsync_HappyPath_CreatesInvoiceAndReturnsSuccess()
//        {
//            // Arrange
//            var vm = new ConfirmBookingViewModel
//            {
//                MovieShowId = 99,
//                SelectedSeats = new List<SeatDetailViewModel> {
//                    new() { SeatId=7, Price=120m }
//                }
//            };
//            var user = new Account { AccountId = "u1", Rank = null };
//            var show = new MovieShow { MovieShowId = 99, MovieId = "M1", Version = new ModelVersion(), Schedule = new Schedule() }; // Fix: set MovieId

//            _accountService.Setup(x => x.GetById("u1")).Returns(user);
//            _context.MovieShows.Add(show);
//            await _context.SaveChangesAsync();

//            // prices
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult { SeatTotalAfterDiscounts = 120m, Subtotal = 120m, UseScore = 10, AddScore = 5 });

//            _bookingService.Setup(x => x.GenerateInvoiceIdAsync()).ReturnsAsync("INV1");

//            // Act
//            var result = await _svc.ConfirmBookingAsync(vm, userId: "u1", isTestSuccess: "true");

//            // Assert
//            Assert.True(result.Success);
//            Assert.Equal("INV1", result.InvoiceId);

//            // Verify invoice persisted
//            var saved = _context.Invoices.FirstOrDefault(i => i.InvoiceId == "INV1");
//            Assert.NotNull(saved);
//            Assert.Equal(120m, saved.TotalMoney);
//        }

//        [Fact]
//        public async Task BuildSuccessViewModelAsync_ReturnsNull_WhenInvoiceNotFound()
//        {
//            // Act
//            var vm = await _svc.BuildSuccessViewModelAsync("bad", "u1");

//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildSuccessViewModelAsync_HappyPath_PopulatesViewModel()
//        {
//            // Arrange
//            var account = new Account { AccountId = "u1", Email = "e@x" };
//            var expectedDate = new DateOnly(2025, 7, 15);
//            var show = new MovieShow
//            {
//                MovieShowId = 5,
//                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "X" },
//                ShowDate = expectedDate, // Set expected date
//                CinemaRoom = new CinemaRoom { CinemaRoomName = "C" },
//                Version = new ModelVersion { VersionName = "V1" },
//                Schedule = new Schedule { ScheduleTime = new TimeOnly(9, 0) }
//            };
//            var inv = new Invoice
//            {
//                InvoiceId = "I1",
//                AccountId = "u1",
//                MovieShowId = 5,
//                TotalMoney = 200m,
//                UseScore = 0,
//                AddScore = 10,
//                PromotionDiscount = 0,
//                Status = InvoiceStatus.Completed,
//                BookingDate = DateTime.Today
//            };
//            _accountService.Setup(x => x.GetById("u1")).Returns(account);
//            _context.MovieShows.Add(show);
//            _context.Invoices.Add(inv);
//            _context.ScheduleSeats.Add(new ScheduleSeat { InvoiceId = "I1", SeatId = 7, MovieShowId = 5 });
//            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 200, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(seatType);
//            _context.Seats.Add(new Seat { SeatId = 7, SeatName = "A7", SeatTypeId = 1, SeatType = seatType }); // Add seat entity with type
//            await _context.SaveChangesAsync();

//            // Act
//            var vm = await _svc.BuildSuccessViewModelAsync("I1", "u1");

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Equal("X", vm.BookingDetails.MovieName);
//            Assert.Equal(expectedDate, vm.BookingDetails.ShowDate); // Assert against expected date
//            Assert.Contains("A7", vm.BookingDetails.SelectedSeats.Select(s => s.SeatName)); // Assert seat name
//            Assert.Equal(200m, vm.Subtotal + vm.TotalFoodPrice);
//        }

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_ReturnsNull_WhenMovieShowNotFound()
//        {
//            // Arrange
//            var movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            _bookingService.Setup(x => x.GetById("M1")).Returns(movie);
//            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow>()); // No shows

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync("M1", DateOnly.FromDateTime(DateTime.Today), "14:00", new List<int>(), 1, null, null, "u1");

//            // Assert
//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_ReturnsNull_WhenCinemaRoomIsNull()
//        {
//            // Arrange
//            var show = new MovieShow { MovieShowId = 1, ShowDate = DateOnly.FromDateTime(DateTime.Today), Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }, Version = new ModelVersion(), Movie = new Movie { MovieId = "M1" } };
//            var movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            _bookingService.Setup(x => x.GetById("M1")).Returns(movie);
//            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow> { show });

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync("M1", DateOnly.FromDateTime(DateTime.Today), "14:00", new List<int>(), 1, null, null, "u1");

//            // Assert
//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_ReturnsNull_WhenUserAccountIsNull()
//        {
//            // Arrange
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            var movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            _bookingService.Setup(x => x.GetById("M1")).Returns(movie);
//            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow> { show });
//            _accountService.Setup(x => x.GetById("u1")).Returns((Account)null);

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync("M1", DateOnly.FromDateTime(DateTime.Today), "14:00", new List<int>(), 1, null, null, "u1");

//            // Assert
//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_FoodLogic_WorksWithValidFoodIdsAndQtys()
//        {
//            // Arrange
//            var user = new Account { AccountId = "u1", FullName = "Jane" };
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            var seatEntity = new Seat { SeatId = 5, SeatName = "A1", SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } };
//            var food = new FoodViewModel { FoodId = 10, Name = "Popcorn", Price = 50 };
//            _bookingService.Setup(x => x.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test" });
//            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow> { show });
//            _seatService.Setup(x => x.GetSeatById(5)).Returns(seatEntity);
//            _seatService.Setup(x => x.GetSeatTypesAsync()).ReturnsAsync(new List<SeatType> { seatEntity.SeatType });
//            _accountService.Setup(x => x.GetById("u1")).Returns(user);
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>())).Returns((Promotion)null);
//            _foodService.Setup(x => x.GetByIdAsync(It.Is<int>(id => id == 10))).ReturnsAsync(food);

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync(
//                movieId: "M1",
//                showDate: DateOnly.FromDateTime(DateTime.Today),
//                showTime: "14:00",
//                selectedSeatIds: new List<int> { 5 },
//                movieShowId: 1,
//                foodIds: new List<int> { 10 },
//                foodQtys: new List<int> { 2 },
//                userId: "u1"
//            );

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Single(vm.SelectedFoods);
//            Assert.Equal(100, vm.TotalFoodPrice);
//        }

//        [Fact]
//        public async Task BuildConfirmBookingViewModelAsync_FoodLogic_IgnoresMismatchedFoodIdsAndQtys()
//        {
//            // Arrange
//            var user = new Account { AccountId = "u1", FullName = "Jane" };
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            var seatEntity = new Seat { SeatId = 5, SeatName = "A1", SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } };
//            _bookingService.Setup(x => x.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test" });
//            _movieService.Setup(x => x.GetMovieShows("M1")).Returns(new List<MovieShow> { show });
//            _seatService.Setup(x => x.GetSeatById(5)).Returns(seatEntity);
//            _seatService.Setup(x => x.GetSeatTypesAsync()).ReturnsAsync(new List<SeatType> { seatEntity.SeatType });
//            _accountService.Setup(x => x.GetById("u1")).Returns(user);
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>())).Returns((Promotion)null);

//            // Act
//            var vm = await _svc.BuildConfirmBookingViewModelAsync(
//                movieId: "M1",
//                showDate: DateOnly.FromDateTime(DateTime.Today),
//                showTime: "14:00",
//                selectedSeatIds: new List<int> { 5 },
//                movieShowId: 1,
//                foodIds: new List<int> { 10 },
//                foodQtys: new List<int> { 2, 3 }, // Mismatched
//                userId: "u1"
//            );

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Empty(vm.SelectedFoods);
//            Assert.Equal(0, vm.TotalFoodPrice);
//        }

//        [Fact]
//        public async Task ConfirmBookingAsync_ReturnsFalse_WhenUserNotFound()
//        {
//            // Arrange
//            var vm = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } } };
//            _accountService.Setup(x => x.GetById("u1")).Returns((Account)null);

//            // Act
//            var result = await _svc.ConfirmBookingAsync(vm, "u1", "true");

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("User not found.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmBookingAsync_ReturnsFalse_WhenVoucherIsInvalid()
//        {
//            // Arrange
//            var vm = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1, Price = 100m } }, SelectedVoucherId = "V1" };
//            var user = new Account { AccountId = "u1" };
//            _accountService.Setup(x => x.GetById("u1")).Returns(user);
//            // Stub CalculatePrice so priceResult isn't null
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult
//            {
//                Subtotal = 100m,
//                SeatTotalAfterDiscounts = 100m,
//                UseScore = 0,
//                AddScore = 0,
//                VoucherAmount = 0,
//                TotalFoodPrice = 0,
//                TotalPrice = 100m
//            });
//            _voucherService.Setup(x => x.GetById("V1")).Returns((Voucher)null);
//            // Act
//            var result = await _svc.ConfirmBookingAsync(vm, "u1", "true");
//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Selected voucher does not exist.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmBookingAsync_AddsScoreAndDeductsScore_WhenApplicable()
//        {
//            // Arrange
//            var vm = new ConfirmBookingViewModel { MovieShowId = 99, SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 7, Price = 120m } } };
//            var user = new Account { AccountId = "u1", Rank = null };
//            var show = new MovieShow { MovieShowId = 99, MovieId = "M1", Version = new ModelVersion(), Schedule = new Schedule() };
//            _accountService.Setup(x => x.GetById("u1")).Returns(user);
//            _context.MovieShows.Add(show);
//            await _context.SaveChangesAsync();
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult { SeatTotalAfterDiscounts = 120m, Subtotal = 120m, UseScore = 10, AddScore = 5 });
//            _bookingService.Setup(x => x.GenerateInvoiceIdAsync()).ReturnsAsync("INV2");
//            _accountService.Setup(x => x.AddScoreAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>())).Returns(Task.CompletedTask).Verifiable();
//            _accountService.Setup(x => x.DeductScoreAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>())).Returns(Task.CompletedTask).Verifiable();

//            // Act
//            var result = await _svc.ConfirmBookingAsync(vm, "u1", "true");

//            // Assert
//            Assert.True(result.Success);
//            _accountService.Verify(x => x.AddScoreAsync("u1", 5, true), Times.Once); // isTestSuccess true
//            _accountService.Verify(x => x.DeductScoreAsync("u1", 10, true), Times.Once); // isTestSuccess true
//        }

//        [Fact]
//        public async Task BuildConfirmTicketAdminViewModelAsync_ReturnsNull_WhenMovieShowNotFound()
//        {
//            // Arrange
//            _movieService.Setup(x => x.GetMovieShowById(123)).Returns((MovieShow)null);

//            // Act
//            var vm = await _svc.BuildConfirmTicketAdminViewModelAsync(123, new List<int>(), new List<int>(), new List<int>());

//            // Assert
//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildConfirmTicketAdminViewModelAsync_FoodLogic_WorksWithValidFoodIdsAndQtys()
//        {
//            // Arrange
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            var seatEntity = new Seat { SeatId = 5, SeatName = "A1", SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } };
//            var food = new FoodViewModel { FoodId = 10, Name = "Popcorn", Price = 50 };
//            show.CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" };
//            show.Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            show.Version = new ModelVersion { VersionName = "STD", Multi = 1 };
//            show.Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) };
//            _movieService.Setup(x => x.GetMovieShowById(1)).Returns(show);
//            _seatService.Setup(x => x.GetSeatById(5)).Returns(seatEntity);
//            _seatService.Setup(x => x.GetSeatTypesAsync()).ReturnsAsync(new List<SeatType> { seatEntity.SeatType });
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>())).Returns((Promotion)null);
//            _foodService.Setup(x => x.GetByIdAsync(It.Is<int>(id => id == 10))).ReturnsAsync(food);

//            // Act
//            var vm = await _svc.BuildConfirmTicketAdminViewModelAsync(1, new List<int> { 5 }, new List<int> { 10 }, new List<int> { 2 });

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Single(vm.SelectedFoods);
//            Assert.Equal(100, vm.TotalFoodPrice);
//        }

//        [Fact]
//        public async Task BuildConfirmTicketAdminViewModelAsync_SkipsSeatsThatAreNull()
//        {
//            // Arrange
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            show.CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" };
//            show.Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            show.Version = new ModelVersion { VersionName = "STD", Multi = 1 };
//            show.Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) };
//            _movieService.Setup(x => x.GetMovieShowById(1)).Returns(show);
//            _seatService.Setup(x => x.GetSeatById(5)).Returns((Seat)null); // Seat not found
//            _seatService.Setup(x => x.GetSeatTypesAsync()).ReturnsAsync(new List<SeatType>());
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>())).Returns((Promotion)null);

//            // Act
//            var vm = await _svc.BuildConfirmTicketAdminViewModelAsync(1, new List<int> { 5 }, null, null);

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Empty(vm.BookingDetails.SelectedSeats);
//        }

//        [Fact]
//        public async Task BuildConfirmTicketAdminViewModelAsync_SkipsFoodsThatAreNull()
//        {
//            // Arrange
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            show.CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" };
//            show.Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            show.Version = new ModelVersion { VersionName = "STD", Multi = 1 };
//            show.Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) };
//            _movieService.Setup(x => x.GetMovieShowById(1)).Returns(show);
//            _seatService.Setup(x => x.GetSeatById(5)).Returns(new Seat { SeatId = 5, SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } });
//            _seatService.Setup(x => x.GetSeatTypesAsync()).ReturnsAsync(new List<SeatType> { new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } });
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>())).Returns((Promotion)null);
//            _foodService.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FoodViewModel)null); // Food not found

//            // Act
//            var vm = await _svc.BuildConfirmTicketAdminViewModelAsync(1, new List<int> { 5 }, new List<int> { 10 }, new List<int> { 2 });

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Empty(vm.SelectedFoods);
//            Assert.Equal(0, vm.TotalFoodPrice);
//        }

//        [Fact]
//        public async Task BuildConfirmTicketAdminViewModelAsync_IgnoresMismatchedFoodIdsAndQtys()
//        {
//            // Arrange
//            var show = MakeShow(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0));
//            show.CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" };
//            show.Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" };
//            show.Version = new ModelVersion { VersionName = "STD", Multi = 1 };
//            show.Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) };
//            _movieService.Setup(x => x.GetMovieShowById(1)).Returns(show);
//            _seatService.Setup(x => x.GetSeatById(5)).Returns(new Seat { SeatId = 5, SeatTypeId = 2, SeatType = new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } });
//            _seatService.Setup(x => x.GetSeatTypesAsync()).ReturnsAsync(new List<SeatType> { new SeatType { SeatTypeId = 2, TypeName = "VIP", PricePercent = 200 } });
//            _promoService.Setup(x => x.GetBestPromotionForShowDate(It.IsAny<DateOnly>())).Returns((Promotion)null);

//            // Act
//            var vm = await _svc.BuildConfirmTicketAdminViewModelAsync(1, new List<int> { 5 }, new List<int> { 10 }, new List<int> { 2, 3 });

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Empty(vm.SelectedFoods);
//            Assert.Equal(0, vm.TotalFoodPrice);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_ReturnsFalse_WhenBookingDetailsOrSeatsMissing()
//        {
//            // Arrange
//            var model = new ConfirmTicketAdminViewModel { BookingDetails = null };

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Booking details or selected seats are missing.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_ReturnsFalse_WhenMemberIdMissing()
//        {
//            // Arrange
//            var model = new ConfirmTicketAdminViewModel { BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } } }, MemberId = null };

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Member check is required before confirming.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_ReturnsFalse_WhenMemberNotFound()
//        {
//            // Arrange
//            var model = new ConfirmTicketAdminViewModel { BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } } }, MemberId = "M123" };
//            // No member in context

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Member not found. Please check member details again.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_ReturnsFalse_WhenMemberAccountNotFound()
//        {
//            // Arrange
//            var member = new Member { MemberId = "M123", Account = null };
//            _context.Members.Add(member);
//            await _context.SaveChangesAsync();
//            var model = new ConfirmTicketAdminViewModel { BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } } }, MemberId = "M123" };

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Member account not found. Please check member details again.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_ReturnsFalse_WhenVoucherIsInvalid()
//        {
//            // Arrange
//            var member = new Member { MemberId = "M123", Account = new Account { AccountId = "A1" } };
//            _context.Members.Add(member);
//            await _context.SaveChangesAsync();
//            var model = new ConfirmTicketAdminViewModel
//            {
//                BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } }, PromotionDiscountPercent = 0 },
//                MemberId = "M123",
//                SelectedVoucherId = "VOUCHER1"
//            };
//            _voucherService.Setup(x => x.GetById("VOUCHER1")).Returns((Voucher)null);
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult());
//            _movieService.Setup(x => x.GetMovieShowById(It.IsAny<int>())).Returns(new MovieShow());
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            await _context.SaveChangesAsync();

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.False(result.Success);
//            Assert.Equal("Selected voucher does not exist.", result.ErrorMessage);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_DoesNotCallAddOrDeductScore_WhenZero()
//        {
//            // Arrange
//            var member = new Member { MemberId = "M123", Account = new Account { AccountId = "A1" } };
//            _context.Members.Add(member);
//            await _context.SaveChangesAsync();
//            var model = new ConfirmTicketAdminViewModel
//            {
//                BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } }, PromotionDiscountPercent = 0 },
//                MemberId = "M123"
//            };
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult { AddScore = 0, UseScore = 0 });
//            _movieService.Setup(x => x.GetMovieShowById(It.IsAny<int>())).Returns(new MovieShow());
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            await _context.SaveChangesAsync();
//            _accountService.Setup(x => x.AddScoreAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>())).Verifiable();
//            _accountService.Setup(x => x.DeductScoreAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>())).Verifiable();

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.True(result.Success);
//            _accountService.Verify(x => x.AddScoreAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
//            _accountService.Verify(x => x.DeductScoreAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_UpdatesVoucher_WhenNotUsed()
//        {
//            // Arrange
//            var member = new Member { MemberId = "M123", Account = new Account { AccountId = "A1" } };
//            _context.Members.Add(member);
//            await _context.SaveChangesAsync();
//            var model = new ConfirmTicketAdminViewModel
//            {
//                BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } }, PromotionDiscountPercent = 0 },
//                MemberId = "M123",
//                SelectedVoucherId = "VOUCHER1"
//            };
//            var voucher = new Voucher { VoucherId = "VOUCHER1", IsUsed = false };
//            _voucherService.Setup(x => x.GetById("VOUCHER1")).Returns(voucher);
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult { VoucherAmount = 10 });
//            _movieService.Setup(x => x.GetMovieShowById(It.IsAny<int>())).Returns(new MovieShow());
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            await _context.SaveChangesAsync();
//            _voucherService.Setup(x => x.Update(voucher)).Verifiable();

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.True(result.Success);
//            Assert.True(voucher.IsUsed);
//            _voucherService.Verify(x => x.Update(voucher), Times.Once);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_DoesNotUpdateVoucher_WhenAlreadyUsed()
//        {
//            // Arrange
//            var member = new Member { MemberId = "M123", Account = new Account { AccountId = "A1" } };
//            _context.Members.Add(member);
//            await _context.SaveChangesAsync();
//            var model = new ConfirmTicketAdminViewModel
//            {
//                BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } }, PromotionDiscountPercent = 0 },
//                MemberId = "M123",
//                SelectedVoucherId = "VOUCHER1"
//            };
//            var voucher = new Voucher { VoucherId = "VOUCHER1", IsUsed = true };
//            _voucherService.Setup(x => x.GetById("VOUCHER1")).Returns(voucher);
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult { VoucherAmount = 10 });
//            _movieService.Setup(x => x.GetMovieShowById(It.IsAny<int>())).Returns(new MovieShow());
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            await _context.SaveChangesAsync();
//            _voucherService.Setup(x => x.Update(voucher)).Verifiable();

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.True(result.Success);
//            Assert.True(voucher.IsUsed);
//            _voucherService.Verify(x => x.Update(voucher), Times.Never);
//        }

//        [Fact]
//        public async Task ConfirmTicketForAdminAsync_SavesFoodInvoices_WhenFoodsPresent()
//        {
//            // Arrange
//            _bookingService.Setup(x => x.GenerateInvoiceIdAsync()).ReturnsAsync("INV_ADMIN");
//            _bookingService.Setup(x => x.SaveInvoiceAsync(It.IsAny<Invoice>())).Returns<Invoice>(inv => { _context.Invoices.Add(inv); return _context.SaveChangesAsync(); });
//            var member = new Member { MemberId = "M123", Account = new Account { AccountId = "A1" } };
//            _context.Members.Add(member);
//            await _context.SaveChangesAsync();
//            var model = new ConfirmTicketAdminViewModel
//            {
//                BookingDetails = new ConfirmBookingViewModel { SelectedSeats = new List<SeatDetailViewModel> { new() { SeatId = 1 } }, PromotionDiscountPercent = 0 },
//                MemberId = "M123",
//                SelectedFoods = new List<FoodViewModel> { new FoodViewModel { FoodId = 10, Price = 50, Quantity = 2 } }
//            };
//            _priceCalc.Setup(x => x.CalculatePrice(
//                It.IsAny<List<SeatDetailViewModel>>(),
//                It.IsAny<MovieShow>(),
//                It.IsAny<Account>(),
//                It.IsAny<decimal?>(),
//                It.IsAny<int?>(),
//                It.IsAny<List<Food>>()
//            )).Returns(new BookingPriceResult());
//            _movieService.Setup(x => x.GetMovieShowById(It.IsAny<int>())).Returns(new MovieShow());
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            await _context.SaveChangesAsync();

//            // Act
//            var result = await _svc.ConfirmTicketForAdminAsync(model);

//            // Assert
//            Assert.True(result.Success);
//            var foodInvoice = _context.FoodInvoices.FirstOrDefault(f => f.FoodId == 10);
//            Assert.NotNull(foodInvoice);
//            Assert.Equal(2, foodInvoice.Quantity);
//            Assert.Equal(50, foodInvoice.Price);
//        }

//        [Fact]
//        public async Task BuildTicketBookingConfirmedViewModelAsync_ReturnsNull_WhenInvoiceNotFound()
//        {
//            // Act
//            var vm = await _svc.BuildTicketBookingConfirmedViewModelAsync("bad");

//            // Assert
//            Assert.Null(vm);
//        }

//        [Fact]
//        public async Task BuildTicketBookingConfirmedViewModelAsync_SkipsSeatsThatAreNull()
//        {
//            // Arrange
//            var show = new MovieShow
//            {
//                MovieShowId = 1,
//                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" },
//                ShowDate = DateOnly.FromDateTime(DateTime.Today),
//                CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" },
//                Version = new ModelVersion { VersionName = "STD", Multi = 1 },
//                Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
//            };
//            _context.MovieShows.Add(show);
//            await _context.SaveChangesAsync();
//            var inv = new Invoice { InvoiceId = "INVX", SeatIds = "1,2", MovieShowId = 1, TotalMoney = 100 };
//            _context.Invoices.Add(inv);
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            _context.ScheduleSeats.Add(new ScheduleSeat
//            {
//                InvoiceId = inv.InvoiceId,
//                SeatId = 999, // does NOT match any seat in context
//                MovieShowId = inv.MovieShowId,
//                SeatStatusId = 2
//            });
//            await _context.SaveChangesAsync();
//            _seatService.Setup(s => s.GetSeatById(999)).Returns((Seat)null);

//            // Act
//            var vm = await _svc.BuildTicketBookingConfirmedViewModelAsync("INVX");

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Empty(vm.BookingDetails.SelectedSeats);
//        }

//        [Fact]
//        public async Task BuildTicketBookingConfirmedViewModelAsync_HandlesSeatTypeIdNull()
//        {
//            // Arrange
//            var show = new MovieShow
//            {
//                MovieShowId = 1,
//                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" },
//                ShowDate = DateOnly.FromDateTime(DateTime.Today),
//                CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" },
//                Version = new ModelVersion { VersionName = "STD", Multi = 1 },
//                Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
//            };
//            _context.MovieShows.Add(show);

//            var inv = new Invoice
//            {
//                InvoiceId = "INVY",
//                MovieShowId = 1,
//                SeatIds = "3",
//                TotalMoney = 100m,
//                BookingDate = DateTime.Today,
//                Status = InvoiceStatus.Completed
//            };
//            _context.Invoices.Add(inv);

//            _context.ScheduleSeats.Add(new ScheduleSeat
//            {
//                InvoiceId = inv.InvoiceId,
//                SeatId = 3,
//                MovieShowId = 1,
//                SeatStatusId = 2
//            });

//            // We do need a dummy SeatType so EF is happy — but our seat 3 has no type
//            _context.SeatTypes.Add(new SeatType
//            {
//                SeatTypeId = 1,
//                TypeName = "Ignored",
//                PricePercent = 100,
//                ColorHex = "#FFF"
//            });
//            _context.Seats.Add(new Seat
//            {
//                SeatId = 3,
//                SeatTypeId = null,
//                SeatType = null,
//                SeatName = "X3"
//            });

//            await _context.SaveChangesAsync();

//            // *** Stub the seatService so BuildTicketBookingConfirmed can actually pull this Seat ***
//            _seatService
//                .Setup(s => s.GetSeatById(3))
//                .Returns(new Seat { SeatId = 3, SeatName = "X3", SeatTypeId = null, SeatType = null });

//            // Act
//            var vm = await _svc.BuildTicketBookingConfirmedViewModelAsync("INVY");

//            // Assert
//            Assert.NotNull(vm);
//            // Now we *do* get exactly one seat back, even though it has no SeatType
//            Assert.Single(vm.BookingDetails.SelectedSeats);
//            Assert.Equal("N/A", vm.BookingDetails.SelectedSeats[0].SeatType);
//        }

//        [Fact]
//        public async Task BuildTicketBookingConfirmedViewModelAsync_HandlesRankDiscountAndVoucher()
//        {
//            // Arrange
//            var show = new MovieShow
//            {
//                MovieShowId = 1,
//                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" },
//                ShowDate = DateOnly.FromDateTime(DateTime.Today),
//                CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" },
//                Version = new ModelVersion { VersionName = "STD", Multi = 1 },
//                Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
//            };
//            _context.MovieShows.Add(show);

//            // rank/account/member (not strictly needed for admin‑only discount read but ok)
//            var rank = new Rank { RankId = 1, DiscountPercentage = 10 };
//            _context.Ranks.Add(rank);
//            var account = new Account { AccountId = "A1", RankId = 1, Rank = rank };
//            _context.Accounts.Add(account);
//            _context.Members.Add(new Member { MemberId = "M1", AccountId = "A1", Account = account });

//            // Invoice + scheduleSeat
//            var inv = new Invoice
//            {
//                InvoiceId = "INVN",
//                MovieShowId = 1,
//                SeatIds = "1",
//                RankDiscountPercentage = 10,
//                VoucherId = "V1",
//                UseScore = 1,
//                BookingDate = DateTime.Today,
//                Status = InvoiceStatus.Completed,
//                AccountId = "A1"
//            };
//            _context.Invoices.Add(inv);

//            _context.ScheduleSeats.Add(new ScheduleSeat
//            {
//                InvoiceId = inv.InvoiceId,
//                SeatId = 1,
//                MovieShowId = 1,
//                SeatStatusId = 2
//            });

//            // Voucher
//            _context.Vouchers.Add(new Voucher
//            {
//                VoucherId = "V1",
//                Value = 20,
//                AccountId = "A1",
//                Code = "CODE1"
//            });

//            // Seed a real seat‐type & seat so subtotal = 100
//            var st = new SeatType
//            {
//                SeatTypeId = 1,
//                TypeName = "Standard",
//                PricePercent = 100,
//                ColorHex = "#FFFFFF"
//            };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat
//            {
//                SeatId = 1,
//                SeatName = "A1",
//                SeatTypeId = 1,
//                SeatType = st
//            });

//            await _context.SaveChangesAsync();

//            // *** stub both seatService AND seatTypeService ***
//            _seatService
//              .Setup(s => s.GetSeatById(1))
//              .Returns(new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = st });

//            // Act
//            var vm = await _svc.BuildTicketBookingConfirmedViewModelAsync("INVN");

//            // Assert
//            Assert.NotNull(vm);
//            // subtotal = 100
//            // rankDiscount = 100 * (10%/100m) == 10
//            Assert.Equal(10m, vm.RankDiscount);
//            // voucher value
//            Assert.Equal(20m, vm.VoucherAmount);
//        }

//        [Fact]
//        public async Task BuildTicketBookingConfirmedViewModelAsync_SetsTotalPriceToZeroIfNegative()
//        {
//            // Arrange
//            var show = new MovieShow
//            {
//                MovieShowId = 1,
//                Movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test" },
//                ShowDate = DateOnly.FromDateTime(DateTime.Today),
//                CinemaRoom = new CinemaRoom { CinemaRoomName = "R1" },
//                Version = new ModelVersion { VersionName = "STD", Multi = 1 },
//                Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
//            };
//            _context.MovieShows.Add(show);
//            await _context.SaveChangesAsync();
//            var inv = new Invoice { InvoiceId = "INVN", SeatIds = "1", MovieShowId = 1, TotalMoney = 10, RankDiscountPercentage = 50, VoucherId = "V1", UseScore = 1 };
//            _context.Invoices.Add(inv);
//            var st = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 10, ColorHex = "#FFFFFF" };
//            _context.SeatTypes.Add(st);
//            _context.Seats.Add(new Seat { SeatId = 1, SeatTypeId = 1, SeatType = st });
//            _context.Vouchers.Add(new Voucher { VoucherId = "V1", Value = 100, AccountId = "A1", Code = "CODE1" });
//            _context.ScheduleSeats.Add(new ScheduleSeat { InvoiceId = inv.InvoiceId, SeatId = 1, MovieShowId = inv.MovieShowId, SeatStatusId = 2 });
//            await _context.SaveChangesAsync();

//            // Act
//            var vm = await _svc.BuildTicketBookingConfirmedViewModelAsync("INVN");

//            // Assert
//            Assert.NotNull(vm);
//            Assert.Equal(0, vm.TotalPrice);
//        }
//    }

//    // Helper to create an in‑memory DbContext
//    static class InMemoryDb
//    {
//        public static MovieTheaterContext Create()
//        {
//            var opts = new DbContextOptionsBuilder<MovieTheaterContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                .Options;
//            return new MovieTheaterContext(opts);
//        }
//    }
//}
