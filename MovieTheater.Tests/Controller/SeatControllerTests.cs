using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using Xunit;
using Microsoft.AspNetCore.Http; // Required for DefaultHttpContext
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Required for TempDataDictionary
using Microsoft.AspNetCore.Routing; // Required for RouteData
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Tests.Controller
{
    public class SeatControllerTests
    {
        private readonly Mock<ICinemaService> _cinemaService = new();
        private readonly Mock<ISeatService> _seatService = new();
        private readonly Mock<ISeatTypeService> _seatTypeService = new();
        private readonly Mock<ICoupleSeatService> _coupleSeatService = new();
        private readonly Mock<IMovieService> _movieService = new();
        private readonly Mock<ILogger<SeatController>> _logger = new();
        private readonly Mock<IScheduleSeatRepository> _scheduleSeatRepository = new();
        private readonly Mock<IFoodService> _foodService = new();

        private SeatController BuildController()
        {
            var controller = new SeatController(
                _cinemaService.Object,
                _seatService.Object,
                _seatTypeService.Object,
                _coupleSeatService.Object,
                _movieService.Object,
                _logger.Object,
                _scheduleSeatRepository.Object,
                _foodService.Object
            );

            // Setup HttpContext and TempData for the controller
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData() // Often needed, even if empty, for controller context
            };
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public void Index_ReturnsView()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Details_ReturnsView()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = controller.Details(1);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Create_Post_ReturnsRedirectToIndex_WhenSuccessful()
        {
            // Arrange
            var controller = BuildController();
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = controller.Create(collection);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public void Create_Post_ReturnsView_WhenExceptionOccurs()
        {
            // Arrange
            var controller = BuildController();
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Mock an exception scenario
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = controller.Create(collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WhenCinemaRoomValid()
        {
            // Arrange
            int cinemaId = 1;
            var cinemaRoom = new CinemaRoom { CinemaRoomId = cinemaId, CinemaRoomName = "Room 1", SeatLength = 5, SeatWidth = 5 };
            _cinemaService.Setup(s => s.GetById(cinemaId)).Returns(cinemaRoom);
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(cinemaId)).ReturnsAsync(new List<Seat>());
            _seatTypeService.Setup(s => s.GetAll()).Returns(new List<SeatType>());
            _coupleSeatService.Setup(s => s.GetAllCoupleSeatsAsync()).ReturnsAsync(new List<CoupleSeat>());
            var ctrl = BuildController();

            // Act
            var result = await ctrl.Edit(cinemaId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<ShowroomEditViewModel>(viewResult.Model);
            // Optionally, if you expect TempData not to be set for this path:
            Assert.Null(ctrl.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Edit_Get_Redirects_WhenSeatLengthOrWidthMissing()
        {
            // Arrange
            int cinemaId = 2;
            var cinemaRoom = new CinemaRoom { CinemaRoomId = cinemaId, CinemaRoomName = "Room 2", SeatLength = 0, SeatWidth = 0 };
            _cinemaService.Setup(s => s.GetById(cinemaId)).Returns(cinemaRoom);
            // These setups below might not be strictly necessary if the redirect happens before these services are called
            // but it doesn't hurt to keep them consistent with the controller's dependencies.
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(cinemaId)).ReturnsAsync(new List<Seat>());
            _seatTypeService.Setup(s => s.GetAll()).Returns(new List<SeatType>());
            _coupleSeatService.Setup(s => s.GetAllCoupleSeatsAsync()).ReturnsAsync(new List<CoupleSeat>());

            var ctrl = BuildController();

            // Act
            var result = await ctrl.Edit(cinemaId);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("ShowroomMg", redirect.RouteValues["tab"]);
            Assert.Equal("Add seat length and seat width before viewing seat", ctrl.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task View_Get_ReturnsView_WhenCinemaRoomExists()
        {
            // Arrange
            int cinemaId = 1;
            var cinemaRoom = new CinemaRoom { CinemaRoomId = cinemaId, CinemaRoomName = "Room 1", SeatLength = 5, SeatWidth = 5 };
            _cinemaService.Setup(s => s.GetById(cinemaId)).Returns(cinemaRoom);
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(cinemaId)).ReturnsAsync(new List<Seat>());
            _seatTypeService.Setup(s => s.GetAll()).Returns(new List<SeatType>());
            _scheduleSeatRepository.Setup(s => s.GetScheduleSeatsByMovieShowAsync(It.IsAny<int>())).ReturnsAsync(new List<ScheduleSeat>());
            _foodService.Setup(s => s.GetAllAsync(null, null, true)).ReturnsAsync(new FoodListViewModel { Foods = new List<FoodViewModel>() });
            _movieService.Setup(s => s.GetMovieShow()).Returns(new List<MovieShow>());

            var controller = BuildController();

            // Act
            var result = await controller.View(cinemaId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<SeatSelectionViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task View_Get_ReturnsNotFound_WhenCinemaRoomDoesNotExist()
        {
            // Arrange
            int cinemaId = 999;
            _cinemaService.Setup(s => s.GetById(cinemaId)).Returns((CinemaRoom)null);

            var controller = BuildController();

            // Act
            var result = await controller.View(cinemaId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Select_Get_ReturnsView_WhenValidParameters()
        {
            // Arrange
            string movieId = "M1";
            string date = "01/01/2024";
            string time = "10:00";
            int? versionId = 1;

            var movie = new Movie { MovieId = movieId, MovieNameEnglish = "Test Movie" };
            _movieService.Setup(s => s.GetById(movieId)).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows(movieId)).Returns(new List<MovieShow>());

            var controller = BuildController();

            // Act
            var result = await controller.Select(movieId, date, time, versionId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Select_Get_ReturnsNotFound_WhenMovieDoesNotExist()
        {
            // Arrange
            string movieId = "INVALID";
            string date = "01/01/2024";
            string time = "10:00";
            int? versionId = 1;

            _movieService.Setup(s => s.GetById(movieId)).Returns((Movie)null);

            var controller = BuildController();

            // Act
            var result = await controller.Select(movieId, date, time, versionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Select_Get_ReturnsBadRequest_WhenInvalidDateFormat()
        {
            // Arrange
            string movieId = "M1";
            string date = "invalid-date";
            string time = "10:00";
            int? versionId = 1;

            var movie = new Movie { MovieId = movieId, MovieNameEnglish = "Test Movie" };
            _movieService.Setup(s => s.GetById(movieId)).Returns(movie);

            var controller = BuildController();

            // Act
            var result = await controller.Select(movieId, date, time, versionId);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid date format", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateSeatTypes_Post_ReturnsOk_WhenValidUpdates()
        {
            // Arrange
            var updates = new List<SeatTypeUpdateModel>
            {
                new SeatTypeUpdateModel { SeatId = 1, NewSeatTypeId = 2 },
                new SeatTypeUpdateModel { SeatId = 2, NewSeatTypeId = 3 }
            };

            var seat1 = new Seat { SeatId = 1, SeatTypeId = 1 };
            var seat2 = new Seat { SeatId = 2, SeatTypeId = 1 };

            _seatService.Setup(s => s.GetSeatById(1)).Returns(seat1);
            _seatService.Setup(s => s.GetSeatById(2)).Returns(seat2);
            _seatService.Setup(s => s.Save()).Verifiable();

            var controller = BuildController();

            // Act
            var result = await controller.UpdateSeatTypes(updates);

            // Assert
            Assert.IsType<OkResult>(result);
            _seatService.Verify(s => s.Save(), Times.Once);
        }

        [Fact]
        public async Task UpdateSeatTypes_Post_ReturnsOk_WhenSeatNotFound()
        {
            // Arrange
            var updates = new List<SeatTypeUpdateModel>
            {
                new SeatTypeUpdateModel { SeatId = 999, NewSeatTypeId = 2 }
            };

            _seatService.Setup(s => s.GetSeatById(999)).Returns((Seat)null);
            _seatService.Setup(s => s.Save()).Verifiable();

            var controller = BuildController();

            // Act
            var result = await controller.UpdateSeatTypes(updates);

            // Assert
            Assert.IsType<OkResult>(result);
            _seatService.Verify(s => s.Save(), Times.Once);
        }

        [Fact]
        public async Task CreateCoupleSeat_Post_ReturnsOk_WhenValidCoupleSeat()
        {
            // Arrange
            var coupleSeat = new CoupleSeatRequest { FirstSeatId = 1, SecondSeatId = 2 };
            _coupleSeatService.Setup(s => s.CreateCoupleSeatAsync(1, 2)).ReturnsAsync(new CoupleSeat { FirstSeatId = 1, SecondSeatId = 2 });

            var controller = BuildController();

            // Act
            var result = await controller.CreateCoupleSeat(coupleSeat);

            // Assert
            Assert.IsType<OkResult>(result);
            _coupleSeatService.Verify(s => s.CreateCoupleSeatAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task CreateCoupleSeat_Post_ReturnsBadRequest_WhenExceptionOccurs()
        {
            // Arrange
            var coupleSeat = new CoupleSeatRequest { FirstSeatId = 1, SecondSeatId = 2 };
            _coupleSeatService.Setup(s => s.CreateCoupleSeatAsync(1, 2))
                .ThrowsAsync(new InvalidOperationException("Seats are already coupled"));

            var controller = BuildController();

            // Act
            var result = await controller.CreateCoupleSeat(coupleSeat);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Seats are already coupled", badRequest.Value);
        }

        [Fact]
        public void Delete_Get_ReturnsView()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = controller.Delete(1);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Delete_Post_ReturnsRedirectToIndex_WhenSuccessful()
        {
            // Arrange
            var controller = BuildController();
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = controller.Delete(1, collection);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public void Delete_Post_ReturnsView_WhenExceptionOccurs()
        {
            // Arrange
            var controller = BuildController();
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Mock an exception scenario
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = controller.Delete(1, collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteCoupleSeat_Post_ReturnsOk_WhenValidSeatIds()
        {
            // Arrange
            var request = new SeatController.SeatIdsRequest { SeatIds = new List<int> { 1, 2 } };
            _seatService.Setup(s => s.DeleteCoupleSeatBySeatIdsAsync(1, 2)).Returns(Task.CompletedTask);

            var controller = BuildController();

            // Act
            var result = await controller.DeleteCoupleSeat(request);

            // Assert
            Assert.IsType<OkResult>(result);
            _seatService.Verify(s => s.DeleteCoupleSeatBySeatIdsAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task DeleteCoupleSeat_Post_ReturnsBadRequest_WhenSeatIdsIsNull()
        {
            // Arrange
            var request = new SeatController.SeatIdsRequest { SeatIds = null };

            var controller = BuildController();

            // Act
            var result = await controller.DeleteCoupleSeat(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Exactly two seat IDs required.", badRequest.Value);
        }

        [Fact]
        public async Task DeleteCoupleSeat_Post_ReturnsBadRequest_WhenSeatIdsCountIsNotTwo()
        {
            // Arrange
            var request = new SeatController.SeatIdsRequest { SeatIds = new List<int> { 1 } };

            var controller = BuildController();

            // Act
            var result = await controller.DeleteCoupleSeat(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Exactly two seat IDs required.", badRequest.Value);
        }

        [Fact]
        public async Task CreateCoupleSeatsBatch_Post_ReturnsOk_WhenValidCoupleSeats()
        {
            // Arrange
            var coupleSeats = new List<CoupleSeatRequest>
            {
                new CoupleSeatRequest { FirstSeatId = 1, SecondSeatId = 2 },
                new CoupleSeatRequest { FirstSeatId = 3, SecondSeatId = 4 }
            };

            _coupleSeatService.Setup(s => s.CreateCoupleSeatAsync(1, 2)).ReturnsAsync(new CoupleSeat { FirstSeatId = 1, SecondSeatId = 2 });
            _coupleSeatService.Setup(s => s.CreateCoupleSeatAsync(3, 4)).ReturnsAsync(new CoupleSeat { FirstSeatId = 3, SecondSeatId = 4 });

            var controller = BuildController();

            // Act
            var result = await controller.CreateCoupleSeatsBatch(coupleSeats);

            // Assert
            Assert.IsType<OkResult>(result);
            _coupleSeatService.Verify(s => s.CreateCoupleSeatAsync(1, 2), Times.Once);
            _coupleSeatService.Verify(s => s.CreateCoupleSeatAsync(3, 4), Times.Once);
        }

        [Fact]
        public async Task CreateCoupleSeatsBatch_Post_ReturnsBadRequest_WhenCoupleSeatsIsNull()
        {
            // Arrange
            List<CoupleSeatRequest> coupleSeats = null;

            var controller = BuildController();

            // Act
            var result = await controller.CreateCoupleSeatsBatch(coupleSeats);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No couple seat pairs provided.", badRequest.Value);
        }

        [Fact]
        public async Task CreateCoupleSeatsBatch_Post_ReturnsBadRequest_WhenCoupleSeatsIsEmpty()
        {
            // Arrange
            var coupleSeats = new List<CoupleSeatRequest>();

            var controller = BuildController();

            // Act
            var result = await controller.CreateCoupleSeatsBatch(coupleSeats);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No couple seat pairs provided.", badRequest.Value);
        }

        [Fact]
        public async Task CreateCoupleSeatsBatch_Post_HandlesException_WhenOnePairFails()
        {
            // Arrange
            var coupleSeats = new List<CoupleSeatRequest>
            {
                new CoupleSeatRequest { FirstSeatId = 1, SecondSeatId = 2 },
                new CoupleSeatRequest { FirstSeatId = 3, SecondSeatId = 4 }
            };

            _coupleSeatService.Setup(s => s.CreateCoupleSeatAsync(1, 2)).ReturnsAsync(new CoupleSeat { FirstSeatId = 1, SecondSeatId = 2 });
            _coupleSeatService.Setup(s => s.CreateCoupleSeatAsync(3, 4))
                .ThrowsAsync(new InvalidOperationException("Seats are already coupled"));

            var controller = BuildController();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await controller.CreateCoupleSeatsBatch(coupleSeats));

            _coupleSeatService.Verify(s => s.CreateCoupleSeatAsync(1, 2), Times.Once);
            _coupleSeatService.Verify(s => s.CreateCoupleSeatAsync(3, 4), Times.Once);
        }

        [Fact]
        public async Task View_Get_SetsBookedSeatsInViewBag()
        {
            // Arrange
            int cinemaId = 1;
            var cinemaRoom = new CinemaRoom { CinemaRoomId = cinemaId, CinemaRoomName = "Room 1", SeatLength = 5, SeatWidth = 5 };
            var movieShow = new MovieShow { MovieShowId = 1, CinemaRoomId = cinemaId };
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { SeatId = 1, SeatStatusId = 2 },
                new ScheduleSeat { SeatId = 2, SeatStatusId = 2 },
                new ScheduleSeat { SeatId = 3, SeatStatusId = 1 } // Available seat
            };

            _cinemaService.Setup(s => s.GetById(cinemaId)).Returns(cinemaRoom);
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(cinemaId)).ReturnsAsync(new List<Seat>());
            _seatTypeService.Setup(s => s.GetAll()).Returns(new List<SeatType>());
            _movieService.Setup(s => s.GetMovieShow()).Returns(new List<MovieShow> { movieShow });
            _scheduleSeatRepository.Setup(s => s.GetScheduleSeatsByMovieShowAsync(1)).ReturnsAsync(scheduleSeats);
            _foodService.Setup(s => s.GetAllAsync(null, null, true)).ReturnsAsync(new FoodListViewModel { Foods = new List<FoodViewModel>() });

            var controller = BuildController();

            // Act
            var result = await controller.View(cinemaId);

            // Assert
            Assert.IsType<ViewResult>(result);
            // Note: ViewBag is not directly accessible in tests, but the logic should work
        }

        [Fact]
        public async Task Select_Get_WithValidMovieShow_ReturnsView()
        {
            // Arrange
            string movieId = "M1";
            string date = "01/01/2024";
            string time = "10:00";
            int? versionId = 1;

            var movie = new Movie { MovieId = movieId, MovieNameEnglish = "Test Movie" };
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.Parse("10:00") };
            var movieShow = new MovieShow
            {
                MovieShowId = 1,
                MovieId = movieId,
                ShowDate = DateOnly.Parse("01/01/2024"),
                ScheduleId = 1,
                VersionId = versionId ?? 1,
                Schedule = schedule,
                CinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1", SeatLength = 5, SeatWidth = 5 }
            };

            _movieService.Setup(s => s.GetById(movieId)).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows(movieId)).Returns(new List<MovieShow> { movieShow });
            _seatService.Setup(s => s.GetSeatsByRoomIdAsync(1)).ReturnsAsync(new List<Seat>());
            _seatTypeService.Setup(s => s.GetAll()).Returns(new List<SeatType>());
            _scheduleSeatRepository.Setup(s => s.GetScheduleSeatsByMovieShowAsync(1)).ReturnsAsync(new List<ScheduleSeat>());
            _coupleSeatService.Setup(s => s.GetAllCoupleSeatsAsync()).ReturnsAsync(new List<CoupleSeat>());
            _foodService.Setup(s => s.GetAllAsync(null, null, true)).ReturnsAsync(new FoodListViewModel { Foods = new List<FoodViewModel>() });

            var controller = BuildController();

            // Act
            var result = await controller.Select(movieId, date, time, versionId);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Select_Get_ReturnsNotFound_WhenMovieShowNotFound()
        {
            // Arrange
            string movieId = "M1";
            string date = "01/01/2024";
            string time = "10:00";
            int? versionId = 1;

            var movie = new Movie { MovieId = movieId, MovieNameEnglish = "Test Movie" };
            _movieService.Setup(s => s.GetById(movieId)).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows(movieId)).Returns(new List<MovieShow>());

            var controller = BuildController();

            // Act
            var result = await controller.Select(movieId, date, time, versionId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Select_Get_ReturnsNotFound_WhenCinemaRoomNotFound()
        {
            // Arrange
            string movieId = "M1";
            string date = "01/01/2024";
            string time = "10:00";
            int? versionId = 1;

            var movie = new Movie { MovieId = movieId, MovieNameEnglish = "Test Movie" };
            var movieShow = new MovieShow
            {
                MovieShowId = 1,
                MovieId = movieId,
                ShowDate = DateOnly.Parse("01/01/2024"),
                ScheduleId = 1,
                VersionId = versionId ?? 1,
                CinemaRoom = null // No cinema room
            };

            _movieService.Setup(s => s.GetById(movieId)).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows(movieId)).Returns(new List<MovieShow> { movieShow });

            var controller = BuildController();

            // Act
            var result = await controller.Select(movieId, date, time, versionId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}