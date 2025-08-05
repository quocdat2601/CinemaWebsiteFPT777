using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Service;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text;

namespace MovieTheater.Tests.Controller
{
    public class CinemaControllerTests
    {
        private readonly Mock<ICinemaService> _cinemaServiceMock = new();
        private readonly Mock<IMovieService> _movieServiceMock = new();
        private readonly Mock<ITicketService> _ticketServiceMock = new();

        private readonly CinemaController _controller;

        public CinemaControllerTests()
        {
            _controller = new CinemaController(
                _cinemaServiceMock.Object,
                _movieServiceMock.Object,
                _ticketServiceMock.Object
            );

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Setup ControllerContext with HttpContext
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
            };
        }

        #region Index Tests
        [Fact]
        public void Index_ReturnsView()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }
        #endregion

        #region Details Tests
        [Fact]
        public void Details_ReturnsView()
        {
            // Act
            var result = _controller.Details(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
        }
        #endregion

        #region Create Tests
        [Fact]
        public void Create_InvalidModel_RedirectsToAdminMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom();
            _controller.ModelState.AddModelError("CinemaRoomName", "Name is required");

            // Act
            var result = _controller.Create(cinemaRoom, 1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
        }

        [Fact]
        public void Create_ValidModel_AdminRole_RedirectsToAdminMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "Test Room" };
            SetupUserRole("Admin");

            // Act
            var result = _controller.Create(cinemaRoom, 1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom created successfully!", _controller.TempData["ToastMessage"]);
            _cinemaServiceMock.Verify(s => s.Add(It.Is<CinemaRoom>(r => r.VersionId == 1)), Times.Once);
        }

        [Fact]
        public void Create_ValidModel_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "Test Room" };
            SetupUserRole("Employee");

            // Act
            var result = _controller.Create(cinemaRoom, 1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom created successfully!", _controller.TempData["ToastMessage"]);
        }
        #endregion

        #region Edit GET Tests
        [Fact]
        public void Edit_Get_ShowroomFound_ReturnsView()
        {
            // Arrange
            var showroom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            var versions = new List<MovieTheater.Models.Version> { new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" } };
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns(showroom);
            _movieServiceMock.Setup(s => s.GetAllVersions()).Returns(versions);

            // Act
            var result = _controller.Edit(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(showroom, result.Model);
            Assert.Equal(versions, result.ViewData["Versions"]);
            Assert.Equal(0, result.ViewData["CurrentVersionId"]);
        }

        [Fact]
        public void Edit_Get_ShowroomNotFound_ReturnsNotFound()
        {
            // Arrange
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns((CinemaRoom)null);

            // Act
            var result = _controller.Edit(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        #endregion

        #region Edit POST Tests
        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsViewWithErrorsAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "" };
            var versions = new List<MovieTheater.Models.Version> { new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" } };
            _controller.ModelState.AddModelError("CinemaRoomName", "Name is required");
            _movieServiceMock.Setup(s => s.GetAllVersions()).Returns(versions);

            // Act
            var result = await _controller.Edit(cinemaRoom, 1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cinemaRoom, result.Model);
            Assert.Equal(versions, result.ViewData["Versions"]);
            Assert.Equal(1, result.ViewData["CurrentVersionId"]);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdateFails_ReturnsViewWithErrorAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "Test Room" };
            var versions = new List<MovieTheater.Models.Version> { new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" } };
            _cinemaServiceMock.Setup(s => s.Update(cinemaRoom)).Returns(Task.FromResult(false));
            _movieServiceMock.Setup(s => s.GetAllVersions()).Returns(versions);

            // Act
            var result = await _controller.Edit(cinemaRoom, 1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cinemaRoom, result.Model);
            Assert.Equal("Failed to update showroom. Please check your input and try again.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_AdminRole_RedirectsToAdminMainPageAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "Test Room" };
            SetupUserRole("Admin");
            _cinemaServiceMock.Setup(s => s.Update(cinemaRoom)).Returns(Task.FromResult(true));

            // Act
            var result = await _controller.Edit(cinemaRoom, 1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom updated successfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_EmployeeRole_RedirectsToEmployeeMainPageAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "Test Room" };
            SetupUserRole("Employee");
            _cinemaServiceMock.Setup(s => s.Update(cinemaRoom)).Returns(Task.FromResult(true));

            // Act
            var result = await _controller.Edit(cinemaRoom, 1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
        }

        [Fact]
        public async Task Edit_Post_ThrowsException_ReturnsViewWithErrorAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "Test Room" };
            var versions = new List<MovieTheater.Models.Version> { new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" } };
            _cinemaServiceMock.Setup(s => s.Update(cinemaRoom)).Throws(new Exception("Test exception"));
            _movieServiceMock.Setup(s => s.GetAllVersions()).Returns(versions);

            // Act
            var result = await _controller.Edit(cinemaRoom, 1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cinemaRoom, result.Model);
            Assert.Contains("An error occurred while updating the showroom: Test exception", _controller.TempData["ErrorMessage"].ToString());
        }
        #endregion

        #region Delete Tests
        [Fact]
        public async Task Delete_ShowroomNotFound_RedirectsWithError()
        {
            // Arrange
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns((CinemaRoom)null);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Delete(1, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom not found.", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task Delete_DeleteFails_RedirectsWithError()
        {
            // Arrange
            var showroom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns(showroom);
            _cinemaServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(false);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Delete(1, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Failed to delete showroom.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Delete_DeleteSuccess_AdminRole_RedirectsToAdminMainPage()
        {
            // Arrange
            var showroom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns(showroom);
            _cinemaServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Delete(1, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom deleted successfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task Delete_DeleteSuccess_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            // Arrange
            var showroom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns(showroom);
            _cinemaServiceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
            SetupUserRole("Employee");

            // Act
            var result = await _controller.Delete(1, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_ThrowsException_RedirectsWithError()
        {
            // Arrange
            var showroom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns(showroom);
            _cinemaServiceMock.Setup(s => s.DeleteAsync(1)).Throws(new Exception("Test exception"));
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Delete(1, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Contains("An error occurred during deletion: Test exception", _controller.TempData["ToastMessage"].ToString());
        }
        #endregion

        #region Disable GET Tests
        [Fact]
        public void Disable_Get_ShowroomFound_ReturnsView()
        {
            // Arrange
            var showroom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns(showroom);

            // Act
            var result = _controller.Disable(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(showroom, result.Model);
        }

        [Fact]
        public void Disable_Get_ShowroomNotFound_ReturnsNotFound()
        {
            // Arrange
            _cinemaServiceMock.Setup(s => s.GetById(1)).Returns((CinemaRoom)null);

            // Act
            var result = _controller.Disable(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        #endregion

        #region Disable POST Tests
        [Fact]
        public async Task Disable_Post_DisableFails_RedirectsWithError()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Disable(cinemaRoom)).ReturnsAsync(false);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Disable(cinemaRoom, "") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Failed to update showroom status.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Disable_Post_DisableSuccess_AdminRole_RedirectsToAdminMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Disable(cinemaRoom)).ReturnsAsync(true);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Disable(cinemaRoom, "") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom disabled successfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task Disable_Post_DisableSuccess_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Disable(cinemaRoom)).ReturnsAsync(true);
            SetupUserRole("Employee");

            // Act
            var result = await _controller.Disable(cinemaRoom, "") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
        }

        [Fact]
        public async Task Disable_Post_ThrowsException_RedirectsWithError()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Disable(cinemaRoom)).Throws(new Exception("Test exception"));
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Disable(cinemaRoom, "") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Contains("An error occurred while disabling the showroom: Test exception", _controller.TempData["ErrorMessage"].ToString());
        }
        #endregion

        #region Enable Tests
        [Fact]
        public async Task Enable_EnableFails_RedirectsWithError()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Enable(cinemaRoom)).ReturnsAsync(false);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Enable(cinemaRoom) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Failed to update showroom status.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Enable_EnableSuccess_AdminRole_RedirectsToAdminMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Enable(cinemaRoom)).ReturnsAsync(true);
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Enable(cinemaRoom) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Equal("Showroom enabled successfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task Enable_EnableSuccess_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Enable(cinemaRoom)).ReturnsAsync(true);
            SetupUserRole("Employee");

            // Act
            var result = await _controller.Enable(cinemaRoom) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
        }

        [Fact]
        public async Task Enable_ThrowsException_RedirectsWithError()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Test Room" };
            _cinemaServiceMock.Setup(s => s.Enable(cinemaRoom)).Throws(new Exception("Test exception"));
            SetupUserRole("Admin");

            // Act
            var result = await _controller.Enable(cinemaRoom) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("ShowroomMg", result.RouteValues["tab"]);
            Assert.Contains("An error occurred while activating the showroom: Test exception", _controller.TempData["ErrorMessage"].ToString());
        }
        #endregion

        #region GetRoomsByVersion Tests
        [Fact]
        public void GetRoomsByVersion_ReturnsJson()
        {
            // Arrange
            var rooms = new List<CinemaRoom>
            {
                new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" },
                new CinemaRoom { CinemaRoomId = 2, CinemaRoomName = "Room 2" }
            };
            _cinemaServiceMock.Setup(s => s.GetRoomsByVersion(1)).Returns(rooms);

            // Act
            var result = _controller.GetRoomsByVersion(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            // Note: JsonResult.Value might be null if the list is empty, but the result itself should not be null
        }
        #endregion

        #region GetMovieShowsByCinemaRoomGrouped Tests
        [Fact]
        public void GetMovieShowsByCinemaRoomGrouped_ReturnsJson()
        {
            // Arrange
            var shows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, CinemaRoomId = 1, ShowDate = DateOnly.FromDateTime(DateTime.Today), Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) } },
                new MovieShow { MovieShowId = 2, CinemaRoomId = 1, ShowDate = DateOnly.FromDateTime(DateTime.Today), Schedule = new Schedule { ScheduleTime = new TimeOnly(16, 0) } }
            };
            _movieServiceMock.Setup(s => s.GetMovieShow()).Returns(shows);

            // Act
            var result = _controller.GetMovieShowsByCinemaRoomGrouped(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            // Note: JsonResult.Value might be null if the list is empty, but the result itself should not be null
        }
        #endregion

        #region GetDetailedMovieShowsByCinemaRoom Tests
        [Fact]
        public void GetDetailedMovieShowsByCinemaRoom_ReturnsJson()
        {
            // Arrange
            var shows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 1,
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) },
                    Movie = new Movie { MovieNameEnglish = "Test Movie", Duration = 120 },
                    Version = new MovieTheater.Models.Version { VersionName = "2D" },
                    Invoices = new List<Invoice>()
                }
            };
            _movieServiceMock.Setup(s => s.GetMovieShow()).Returns(shows);

            // Act
            var result = _controller.GetDetailedMovieShowsByCinemaRoom(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            // Note: JsonResult.Value might be null if the list is empty, but the result itself should not be null
        }
        #endregion

        #region GetInvoicesByMovieShow Tests
        [Fact]
        public void GetInvoicesByMovieShow_ReturnsJson()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV1",
                    AccountId = "ACC1",
                    Account = new Account { FullName = "Test User" },
                    Seat = "A1",
                    Cancel = false,
                    TotalMoney = 100
                }
            };
            _movieServiceMock.Setup(s => s.GetInvoicesByMovieShow(1)).Returns(invoices);

            // Act
            var result = _controller.GetInvoicesByMovieShow(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            // Note: JsonResult.Value might be null if the list is empty, but the result itself should not be null
        }
        #endregion

        #region RefundByMovieShow Tests
        [Fact]
        public async Task RefundByMovieShow_ReturnsJson()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1", Cancel = false, Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "INV2", Cancel = false, Status = InvoiceStatus.Completed }
            };
            _movieServiceMock.Setup(s => s.GetInvoicesByMovieShow(1)).Returns(invoices);
            _ticketServiceMock.Setup(s => s.CancelTicketByAdminAsync("INV1", It.IsAny<string>()))
                .ReturnsAsync((true, new List<string> { "Success" }));
            _ticketServiceMock.Setup(s => s.CancelTicketByAdminAsync("INV2", It.IsAny<string>()))
                .ReturnsAsync((true, new List<string> { "Success" }));
            SetupUserRole("Admin");

            // Act
            var result = await _controller.RefundByMovieShow(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            // Note: JsonResult.Value might be null if the list is empty, but the result itself should not be null
        }
        #endregion

        #region Helper Methods
        private void SetupUserRole(string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;
        }
        #endregion
    }
}