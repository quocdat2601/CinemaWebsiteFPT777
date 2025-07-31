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
        public void Edit_Get_ReturnsView_WhenCinemaRoomValid()
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
            var result = ctrl.Edit(cinemaId).GetAwaiter().GetResult();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<ShowroomEditViewModel>(viewResult.Model);
            // Optionally, if you expect TempData not to be set for this path:
            Assert.Null(ctrl.TempData["ErrorMessage"]);
        }

        [Fact]
        public void Edit_Get_Redirects_WhenSeatLengthOrWidthMissing()
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
            var result = ctrl.Edit(cinemaId).GetAwaiter().GetResult();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }
    }
}