//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using MovieTheater.Controllers;
//using MovieTheater.Models;
//using MovieTheater.Repository;
//using MovieTheater.Service;
//using MovieTheater.ViewModels;
//using Xunit;
//using System.IO;

//namespace MovieTheater.Tests.Controller
//{
//    public class EmployeeControllerTests
//    {
//        private readonly Mock<IEmployeeService> _employeeServiceMock;
//        private readonly Mock<IMovieService> _movieServiceMock;
//        private readonly Mock<IMemberRepository> _memberRepoMock;
//        private readonly Mock<IAccountService> _accountServiceMock;
//        private readonly Mock<IInvoiceService> _invoiceServiceMock;
//        private readonly EmployeeController _controller;

//        public EmployeeControllerTests()
//        {
//            _employeeServiceMock = new Mock<IEmployeeService>();
//            _movieServiceMock = new Mock<IMovieService>();
//            _memberRepoMock = new Mock<IMemberRepository>();
//            _accountServiceMock = new Mock<IAccountService>();
//            _invoiceServiceMock = new Mock<IInvoiceService>();
//            _controller = new EmployeeController(
//                _employeeServiceMock.Object,
//                _movieServiceMock.Object,
//                _memberRepoMock.Object,
//                _accountServiceMock.Object,
//                _invoiceServiceMock.Object
//            );
//            // Khởi tạo TempData để tránh NullReferenceException
//            var tempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>();
//            var dict = new Dictionary<string, object>();
//            tempData.SetupAllProperties();
//            tempData.Setup(t => t[It.IsAny<string>()]).Returns((string key) => dict.ContainsKey(key) ? dict[key] : null);
//            tempData.SetupSet(t => t[It.IsAny<string>()] = It.IsAny<object>()).Callback<string, object>((key, value) => dict[key] = value);
//            _controller.TempData = tempData.Object;
//        }

//        [Fact]
//        public void MainPage_ReturnsViewWithTab()
//        {
//            var result = _controller.MainPage("MemberMg") as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal("MemberMg", _controller.ViewData["ActiveTab"]);
//        }

//        [Fact]
//        public void MemberList_ReturnsPartialViewWithMembers()
//        {
//            _memberRepoMock.Setup(r => r.GetAll()).Returns(new List<Member> { new Member() });
//            var result = _controller.MemberList() as PartialViewResult;
//            Assert.NotNull(result);
//            Assert.Equal("MemberMg", result.ViewName);
//            Assert.IsType<List<Member>>(result.Model);
//        }

//        [Fact]
//        public void LoadTab_MovieMg_ReturnsPartialViewWithMovies()
//        {
//            _movieServiceMock.Setup(s => s.GetAll()).Returns(new List<Movie> { new Movie() });
//            var result = _controller.LoadTab("MovieMg") as PartialViewResult;
//            Assert.NotNull(result);
//            Assert.Equal("MovieMg", result.ViewName);
//            Assert.IsType<List<Movie>>(result.Model);
//        }

//        [Fact]
//        public void LoadTab_MemberMg_ReturnsPartialViewWithMembers()
//        {
//            _memberRepoMock.Setup(r => r.GetAll()).Returns(new List<Member> { new Member() });
//            var result = _controller.LoadTab("MemberMg") as PartialViewResult;
//            Assert.NotNull(result);
//            // Chấp nhận cả hai trường hợp đường dẫn ViewName
//            Assert.Contains("MemberMg.cshtml", result.ViewName);
//            Assert.IsType<List<Member>>(result.Model);
//        }

//        [Fact]
//        public void LoadTab_UnknownTab_ReturnsContentResult()
//        {
//            var result = _controller.LoadTab("Unknown") as ContentResult;
//            Assert.NotNull(result);
//            Assert.Equal("Tab not found.", result.Content);
//        }

//        [Fact]
//        public void Create_Get_ReturnsViewWithModel()
//        {
//            var result = _controller.Create() as ViewResult;
//            Assert.NotNull(result);
//            Assert.IsType<RegisterViewModel>(result.Model);
//        }

//        [Fact]
//        public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
//        {
//            _controller.ModelState.AddModelError("Test", "Error");
//            var model = new RegisterViewModel();
//            var result = await _controller.CreateAsync(model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//        }

//        [Fact]
//        public async Task Create_Post_UsernameExists_ReturnsViewWithError()
//        {
//            var model = new RegisterViewModel();
//            _employeeServiceMock.Setup(s => s.Register(model)).Returns(false);
//            var result = await _controller.CreateAsync(model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//            Assert.Equal("Registration failed - Username already exists", _controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public async Task Create_Post_UploadFileAndSuccess_RedirectsToMainPage()
//        {
//            var model = new RegisterViewModel
//            {
//                ImageFile = new Mock<IFormFile>().Object
//            };
//            var fileMock = new Mock<IFormFile>();
//            fileMock.Setup(f => f.Length).Returns(1);
//            fileMock.Setup(f => f.FileName).Returns("test.png");
//            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);
//            model.ImageFile = fileMock.Object;
//            _employeeServiceMock.Setup(s => s.Register(It.IsAny<RegisterViewModel>())).Returns(true);

//            var result = await _controller.CreateAsync(model);
//            var redirect = result as RedirectToActionResult;
//            Assert.NotNull(redirect);
//            Assert.Equal("MainPage", redirect.ActionName);
//            Assert.Equal("Admin", redirect.ControllerName);
//            Assert.Equal("EmployeeMg", redirect.RouteValues["tab"]);
//            Assert.Equal("Employee Created Succesfully!", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public async Task Create_Post_Exception_ReturnsViewWithError()
//        {
//            var model = new RegisterViewModel();
//            _employeeServiceMock.Setup(s => s.Register(It.IsAny<RegisterViewModel>())).Throws(new System.Exception("fail"));
//            var result = await _controller.CreateAsync(model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//            Assert.Contains("Error during registration", (string)_controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public void Edit_Get_EmployeeNotFound_ReturnsNotFound()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns((Employee)null);
//            var result = _controller.Edit("id");
//            Assert.IsType<NotFoundResult>(result);
//        }

//        [Fact]
//        public void Edit_Get_EmployeeFound_ReturnsViewWithModel()
//        {
//            var emp = new Employee { Account = new Account { Username = "u", FullName = "f", DateOfBirth = new System.DateOnly(2000, 1, 1), Gender = "M", IdentityCard = "idc", Email = "e", Address = "a", PhoneNumber = "p", Image = "img" }, AccountId = "aid" };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            var result = _controller.Edit("id") as ViewResult;
//            Assert.NotNull(result);
//            Assert.IsType<EmployeeEditViewModel>(result.Model);
//        }

//        [Fact]
//        public async Task Edit_Post_InvalidModel_ReturnsViewWithModel()
//        {
//            _controller.ModelState.AddModelError("Test", "Error");
//            var model = new EmployeeEditViewModel();
//            var result = await _controller.EditAsync("id", model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//        }

//        [Fact]
//        public async Task Edit_Post_EmployeeNotFound_ReturnsViewWithError()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns((Employee)null);
//            var model = new EmployeeEditViewModel();
//            var result = await _controller.EditAsync("id", model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//            Assert.Equal("Employee not found.", _controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public async Task Edit_Post_PasswordNotMatch_ReturnsViewWithError()
//        {
//            var emp = new Employee { Account = new Account { Password = "old" } };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            var model = new EmployeeEditViewModel { Password = "123", ConfirmPassword = "456" };
//            var result = await _controller.EditAsync("id", model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//            Assert.Equal("Password and Confirm Password do not match", _controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public async Task Edit_Post_UpdatePasswordFails_ReturnsViewWithError()
//        {
//            var emp = new Employee
//            {
//                AccountId = "aid",
//                Account = new Account
//                {
//                    Username = "u",
//                    Password = "old"
//                }
//            };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
//            var model = new EmployeeEditViewModel { Password = "new", ConfirmPassword = "new", Username = "u" };
//            var result = await _controller.EditAsync("id", model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//            Assert.Equal("Failed to update password", _controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public async Task Edit_Post_UploadFileAndSuccess_RedirectsToMainPage()
//        {
//            var emp = new Employee
//            {
//                AccountId = "aid",
//                Account = new Account
//                {
//                    Username = "u",
//                    Password = "old",
//                    Image = "img.png"
//                }
//            };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
//            _employeeServiceMock.Setup(s => s.Update(It.IsAny<string>(), It.IsAny<RegisterViewModel>())).Returns(true);
//            var fileMock = new Mock<IFormFile>();
//            fileMock.Setup(f => f.Length).Returns(1);
//            fileMock.Setup(f => f.FileName).Returns("test.png");
//            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);
//            var model = new EmployeeEditViewModel { Password = "old", ConfirmPassword = "old", Username = "u", ImageFile = fileMock.Object };
//            var result = await _controller.EditAsync("id", model);
//            var redirect = result as RedirectToActionResult;
//            Assert.NotNull(redirect);
//            Assert.Equal("MainPage", redirect.ActionName);
//            Assert.Equal("Admin", redirect.ControllerName);
//            Assert.Equal("EmployeeMg", redirect.RouteValues["tab"]);
//            Assert.Equal("Employee Updated Successfully!", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public async Task Edit_Post_NoUploadFile_UsesOldImage()
//        {
//            var emp = new Employee
//            {
//                AccountId = "aid",
//                Account = new Account
//                {
//                    Username = "u",
//                    Password = "old",
//                    Image = "oldimg.png"
//                }
//            };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
//            _employeeServiceMock.Setup(s => s.Update(It.IsAny<string>(), It.IsAny<RegisterViewModel>())).Returns(true);
//            var model = new EmployeeEditViewModel { Password = "old", ConfirmPassword = "old", Username = "u", ImageFile = null };
//            var result = await _controller.EditAsync("id", model);
//            var redirect = result as RedirectToActionResult;
//            Assert.NotNull(redirect);
//            Assert.Equal("MainPage", redirect.ActionName);
//            Assert.Equal("Admin", redirect.ControllerName);
//            Assert.Equal("EmployeeMg", redirect.RouteValues["tab"]);
//            Assert.Equal("Employee Updated Successfully!", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public async Task Edit_Post_UpdateFails_RedirectsWithError()
//        {
//            var emp = new Employee
//            {
//                AccountId = "aid",
//                Account = new Account
//                {
//                    Username = "u",
//                    Password = "old"
//                }
//            };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
//            _employeeServiceMock.Setup(s => s.Update(It.IsAny<string>(), It.IsAny<RegisterViewModel>())).Returns(false);
//            var model = new EmployeeEditViewModel { Password = "old", ConfirmPassword = "old", Username = "u" };
//            var result = await _controller.EditAsync("id", model);
//            var redirect = result as RedirectToActionResult;
//            Assert.NotNull(redirect);
//            Assert.Equal("MainPage", redirect.ActionName);
//            Assert.Equal("Admin", redirect.ControllerName);
//            Assert.Equal("EmployeeMg", redirect.RouteValues["tab"]);
//            Assert.Equal("Update failed - Username already exists", _controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public async Task Edit_Post_Exception_ReturnsViewWithError()
//        {
//            var emp = new Employee
//            {
//                AccountId = "aid",
//                Account = new Account
//                {
//                    Username = "u",
//                    Password = "old"
//                }
//            };
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
//            _employeeServiceMock.Setup(s => s.Update(It.IsAny<string>(), It.IsAny<RegisterViewModel>())).Throws(new System.Exception("fail"));
//            var model = new EmployeeEditViewModel { Password = "old", ConfirmPassword = "old", Username = "u" };
//            var result = await _controller.EditAsync("id", model) as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(model, result.Model);
//            Assert.Contains("Error during update", (string)_controller.TempData["ErrorMessage"]);
//        }

//        [Fact]
//        public void Delete_Get_EmployeeNotFound_RedirectsWithMessage()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns((Employee)null);
//            var result = _controller.Delete("id") as RedirectToActionResult;
//            Assert.NotNull(result);
//            Assert.Equal("MainPage", result.ActionName);
//            Assert.Equal("Employee not found.", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public void Delete_Get_EmployeeFound_ReturnsViewWithModel()
//        {
//            var emp = new Employee();
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(emp);
//            var result = _controller.Delete("id") as ViewResult;
//            Assert.NotNull(result);
//            Assert.Equal(emp, result.Model);
//        }

//        [Fact]
//        public void Delete_Post_EmployeeNotFound_RedirectsWithMessage()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns((Employee)null);
//            var form = new Mock<IFormCollection>().Object;
//            var result = _controller.Delete("id", form) as RedirectToActionResult;
//            Assert.NotNull(result);
//            Assert.Equal("MainPage", result.ActionName);
//            Assert.Equal("Employee not found.", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public void Delete_Post_DeleteFails_RedirectsWithMessage()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(new Employee());
//            _employeeServiceMock.Setup(s => s.Delete("id")).Returns(false);
//            var form = new Mock<IFormCollection>().Object;
//            var result = _controller.Delete("id", form) as RedirectToActionResult;
//            Assert.NotNull(result);
//            Assert.Equal("MainPage", result.ActionName);
//            Assert.Equal("Failed to delete employee.", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public void Delete_Post_DeleteSuccess_RedirectsWithMessage()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(new Employee());
//            _employeeServiceMock.Setup(s => s.Delete("id")).Returns(true);
//            var form = new Mock<IFormCollection>().Object;
//            var result = _controller.Delete("id", form) as RedirectToActionResult;
//            Assert.NotNull(result);
//            Assert.Equal("MainPage", result.ActionName);
//            Assert.Equal("Employee deleted successfully!", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public void Delete_Post_IdNull_RedirectsWithError()
//        {
//            var form = new Mock<IFormCollection>().Object;
//            var result = _controller.Delete(null, form) as RedirectToActionResult;
//            Assert.NotNull(result);
//            Assert.Equal("MainPage", result.ActionName);
//            Assert.Equal("Invalid employee ID.", _controller.TempData["ToastMessage"]);
//        }

//        [Fact]
//        public void Delete_Post_Exception_RedirectsWithError()
//        {
//            _employeeServiceMock.Setup(s => s.GetById("id")).Returns(new Employee());
//            _employeeServiceMock.Setup(s => s.Delete("id")).Throws(new System.Exception("fail"));
//            var form = new Mock<IFormCollection>().Object;
//            var result = _controller.Delete("id", form) as RedirectToActionResult;
//            Assert.NotNull(result);
//            Assert.Equal("MainPage", result.ActionName);
//            Assert.Contains("An error occurred during deletion", (string)_controller.TempData["ToastMessage"]);
//        }
//    }
//}