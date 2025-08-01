using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.Tests.Controller
{
    public class EmployeeControllerTests
    {
        private readonly Mock<IEmployeeService> _employeeServiceMock = new();
        private readonly Mock<IMovieService> _movieServiceMock = new();
        private readonly Mock<IMemberRepository> _memberRepoMock = new();
        private readonly Mock<IAccountService> _accountServiceMock = new();
        private readonly Mock<IInvoiceService> _invoiceServiceMock = new();
        private readonly Mock<ICinemaService> _cinemaServiceMock = new();
        private readonly Mock<IPromotionService> _promotionServiceMock = new();
        private readonly Mock<IFoodService> _foodServiceMock = new();
        private readonly Mock<IVoucherService> _voucherServiceMock = new();
        private readonly Mock<IPersonRepository> _personRepoMock = new();

        private readonly EmployeeController _controller;

        public EmployeeControllerTests()
        {
            _controller = new EmployeeController(
                _employeeServiceMock.Object,
                _movieServiceMock.Object,
                _memberRepoMock.Object,
                _accountServiceMock.Object,
                _invoiceServiceMock.Object,
                _cinemaServiceMock.Object,
                _promotionServiceMock.Object,
                _foodServiceMock.Object,
                _voucherServiceMock.Object,
                _personRepoMock.Object
            );

            // Optional: Fake TempData to avoid null reference
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public void Delete_Get_ReturnsView_WhenEmployeeFound()
        {
            // Arrange
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);

            // Act
            var result = _controller.Delete(employeeId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mockEmployee, result.Model);
        }
        [Fact]
        public void Delete_Get_Redirects_WhenEmployeeNotFound()
        {
            // Arrange
            var employeeId = "EMP999";
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns((Employee)null);

            // Act
            var result = _controller.Delete(employeeId) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
        }
        [Fact]
        public void Delete_Post_DeletesAndRedirects_WhenSuccessful()
        {
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);
            _employeeServiceMock.Setup(s => s.Delete(employeeId)).Returns(true);

            var result = _controller.Delete(employeeId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
        }
        [Fact]
        public void Delete_Post_ReturnsError_WhenDeleteFails()
        {
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);
            _employeeServiceMock.Setup(s => s.Delete(employeeId)).Returns(false);

            var result = _controller.Delete(employeeId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
        }
        [Fact]
        public void ToggleStatus_ValidId_UpdatesStatus()
        {
            var id = "EMP001";
            var employee = new Employee { EmployeeId = id };
            _employeeServiceMock.Setup(s => s.GetById(id)).Returns(employee);

            var result = _controller.ToggleStatus(id) as RedirectToActionResult;

            _employeeServiceMock.Verify(s => s.ToggleStatus(id), Times.Once);
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }
        [Fact]
        public void ToggleStatus_InvalidId_RedirectsWithError()
        {
            var result = _controller.ToggleStatus(null) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            _employeeServiceMock.Verify(s => s.ToggleStatus(It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public void ToggleStatus_EmployeeNotFound_RedirectsWithError()
        {
            var id = "EMP999";
            _employeeServiceMock.Setup(s => s.GetById(id)).Returns((Employee)null);

            var result = _controller.ToggleStatus(id) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }
    }
}
