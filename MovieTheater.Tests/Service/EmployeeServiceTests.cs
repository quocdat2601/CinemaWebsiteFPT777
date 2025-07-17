using System.Collections.Generic;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<IAccountService> _accountServiceMock;
        private readonly EmployeeService _service;

        public EmployeeServiceTests()
        {
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _accountServiceMock = new Mock<IAccountService>();
            _service = new EmployeeService(_employeeRepoMock.Object, _accountServiceMock.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllEmployees()
        {
            // Arrange
            var employees = new List<Employee> { new Employee(), new Employee() };
            _employeeRepoMock.Setup(r => r.GetAll()).Returns(employees);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.Equal(2, ((List<Employee>)result).Count);
        }

        [Fact]
        public void GetById_ReturnsEmployee_WhenExists()
        {
            var employee = new Employee { AccountId = "1" };
            _employeeRepoMock.Setup(r => r.GetById("1")).Returns(employee);

            var result = _service.GetById("1");

            Assert.NotNull(result);
            Assert.Equal("1", result.AccountId);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenNotExists()
        {
            _employeeRepoMock.Setup(r => r.GetById("2")).Returns((Employee)null);

            var result = _service.GetById("2");

            Assert.Null(result);
        }

        [Fact]
        public void Register_CallsAccountServiceAndReturnsResult()
        {
            var model = new RegisterViewModel();
            _accountServiceMock.Setup(s => s.Register(model)).Returns(true);

            var result = _service.Register(model);

            Assert.True(result);
            Assert.Equal(2, model.RoleId); // RoleId phải được set là 2
        }

        [Fact]
        public void Update_ReturnsFalse_WhenEmployeeNotFound()
        {
            _employeeRepoMock.Setup(r => r.GetById("notfound")).Returns((Employee)null);
            var model = new RegisterViewModel();

            var result = _service.Update("notfound", model);

            Assert.False(result);
        }

        [Fact]
        public void Update_CallsAccountServiceUpdate_WhenEmployeeExists()
        {
            var employee = new Employee { AccountId = "acc1" };
            var model = new RegisterViewModel();
            _employeeRepoMock.Setup(r => r.GetById("emp1")).Returns(employee);
            _accountServiceMock.Setup(s => s.Update("acc1", model)).Returns(true);

            var result = _service.Update("emp1", model);

            Assert.True(result);
        }

        [Fact]
        public void Delete_ReturnsFalse_WhenEmployeeNotFound()
        {
            _employeeRepoMock.Setup(r => r.GetById("notfound")).Returns((Employee)null);

            var result = _service.Delete("notfound");

            Assert.False(result);
        }

        [Fact]
        public void Delete_CallsRepositoryDeleteAndSave_WhenEmployeeExists()
        {
            var employee = new Employee { AccountId = "acc1" };
            _employeeRepoMock.Setup(r => r.GetById("emp1")).Returns(employee);

            var result = _service.Delete("emp1");

            _employeeRepoMock.Verify(r => r.Delete("emp1"), Times.Once);
            _employeeRepoMock.Verify(r => r.Save(), Times.Once);
            Assert.True(result);
        }
    }
} 