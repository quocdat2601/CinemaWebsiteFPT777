//using Xunit;
//using Moq;
//using MovieTheater.Controllers;
//using MovieTheater.Repository;
//using MovieTheater.Service;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using MovieTheater.Models;
//using System.Security.Claims;
//using Microsoft.AspNetCore.Http;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using MovieTheater.Hubs;
//using System.Linq;

//namespace MovieTheater.Tests.Controller
//{
//    public class TicketControllerTests
//    {
//        private TicketController CreateController(
//            Mock<IInvoiceRepository> mockInvoiceRepo = null,
//            Mock<IAccountService> mockAccountService = null,
//            Mock<IVoucherService> mockVoucherService = null,
//            Mock<MovieTheaterContext> mockContext = null,
//            Mock<IHubContext<DashboardHub>> mockHub = null,
//            ClaimsPrincipal user = null)
//        {
//            mockInvoiceRepo ??= new Mock<IInvoiceRepository>();
//            mockAccountService ??= new Mock<IAccountService>();
//            mockVoucherService ??= new Mock<IVoucherService>();
//            mockContext ??= new Mock<MovieTheaterContext>();
//            mockHub ??= new Mock<IHubContext<DashboardHub>>();

//            var controller = new TicketController(
//                mockContext.Object,
//                mockInvoiceRepo.Object,
//                mockAccountService.Object,
//                mockVoucherService.Object,
//                null, // IFoodInvoiceService chưa mock, thêm nếu cần
//                mockHub.Object
//            );

//            if (user != null)
//            {
//                controller.ControllerContext = new ControllerContext()
//                {
//                    HttpContext = new DefaultHttpContext() { User = user }
//                };
//            }
//            return controller;
//        }

//        [Fact]
//        public void Test_ReturnsContentResult()
//        {
//            // Arrange
//            var controller = CreateController();

//            // Act
//            var result = controller.Test();

//            // Assert
//            var contentResult = Assert.IsType<ContentResult>(result);
//            Assert.Equal("Test OK", contentResult.Content);
//        }

//        [Fact]
//        public async Task Index_ReturnsViewResult_WhenUserLoggedIn()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<string>(), It.IsAny<InvoiceStatus?>()))
//                .ReturnsAsync(new List<Invoice>());
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, "test-account-id")
//            }, "mock"));
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

//            // Act
//            var result = await controller.Index();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            Assert.IsAssignableFrom<IEnumerable<Invoice>>(viewResult.Model);
//        }

//        [Fact]
//        public async Task Index_RedirectsToLogin_WhenUserNotLoggedIn()
//        {
//            // Arrange
//            var controller = CreateController();
//            controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal() }
//            };

//            // Act
//            var result = await controller.Index();

//            // Assert
//            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
//            Assert.Equal("Login", redirectResult.ActionName);
//            Assert.Equal("Account", redirectResult.ControllerName);
//        }

//        [Fact]
//        public async Task Index_ReturnsViewWithTickets_WhenUserHasTickets()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var fakeAccountId = "test-account";
//            var fakeTickets = new List<Invoice>
//            {
//                new Invoice { InvoiceId = "1", AccountId = fakeAccountId, Status = InvoiceStatus.Completed, MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Movie A" } } }
//            };
//            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(fakeAccountId, It.IsAny<InvoiceStatus?>())).ReturnsAsync(fakeTickets.AsQueryable());
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
//            }, "mock"));
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

//            // Act
//            var result = await controller.Index();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsAssignableFrom<IEnumerable<Invoice>>(viewResult.Model);
//            Assert.Single(model);
//            Assert.Equal("Movie A", model.First().MovieShow.Movie.MovieNameEnglish);
//        }

//        [Fact]
//        public async Task Index_ReturnsViewWithNoTickets_WhenUserHasNoTickets()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var fakeAccountId = "test-account";
//            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(fakeAccountId, It.IsAny<InvoiceStatus?>())).ReturnsAsync(new List<Invoice>().AsQueryable());
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
//            }, "mock"));
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

//            // Act
//            var result = await controller.Index();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsAssignableFrom<IEnumerable<Invoice>>(viewResult.Model);
//            Assert.Empty(model);
//        }

//        [Fact]
//        public async Task HistoryPartial_ReturnsAll_WhenStatusAll()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var fakeAccountId = "test-account";
//            var fakeInvoices = new List<Invoice>
//            {
//                new Invoice { InvoiceId = "1", AccountId = fakeAccountId, Status = InvoiceStatus.Completed },
//                new Invoice { InvoiceId = "2", AccountId = fakeAccountId, Status = InvoiceStatus.Incomplete }
//            }.AsQueryable();
//            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(fakeAccountId, It.IsAny<InvoiceStatus?>())).ReturnsAsync(fakeInvoices);
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
//            }, "mock"));
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

//            // Act
//            var result = await controller.HistoryPartial(null, null, "all") as JsonResult;

//            // Assert
//            Assert.NotNull(result);
//            dynamic data = result.Value;
//            Assert.True(data.success);
//            Assert.Equal(2, ((IEnumerable<object>)data.data).Count());
//        }

//        [Fact]
//        public async Task HistoryPartial_ReturnsBooked_WhenStatusBooked()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var fakeAccountId = "test-account";
//            var fakeInvoices = new List<Invoice>
//            {
//                new Invoice { InvoiceId = "1", AccountId = fakeAccountId, Status = InvoiceStatus.Completed },
//                new Invoice { InvoiceId = "2", AccountId = fakeAccountId, Status = InvoiceStatus.Incomplete }
//            }.AsQueryable();
//            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(fakeAccountId, It.IsAny<InvoiceStatus?>())).ReturnsAsync(fakeInvoices);
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
//            }, "mock"));
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

//            // Act
//            var result = await controller.HistoryPartial(null, null, "booked") as JsonResult;

//            // Assert
//            Assert.NotNull(result);
//            dynamic data = result.Value;
//            Assert.True(data.success);
//            Assert.Single((IEnumerable<object>)data.data);
//        }

//        [Fact]
//        public async Task HistoryPartial_ReturnsCanceled_WhenStatusCanceled()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var fakeAccountId = "test-account";
//            var fakeInvoices = new List<Invoice>
//            {
//                new Invoice { InvoiceId = "1", AccountId = fakeAccountId, Status = InvoiceStatus.Completed },
//                new Invoice { InvoiceId = "2", AccountId = fakeAccountId, Status = InvoiceStatus.Incomplete }
//            }.AsQueryable();
//            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(fakeAccountId, It.IsAny<InvoiceStatus?>())).ReturnsAsync(fakeInvoices);
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
//            }, "mock"));
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

//            // Act
//            var result = await controller.HistoryPartial(null, null, "canceled") as JsonResult;

//            // Assert
//            Assert.NotNull(result);
//            dynamic data = result.Value;
//            Assert.True(data.success);
//            Assert.Single((IEnumerable<object>)data.data);
//        }

//        [Fact]
//        public async Task HistoryPartial_ReturnsError_WhenNotLoggedIn()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo);
//            controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = new DefaultHttpContext() // Không có user
//            };

//            // Act
//            var result = await controller.HistoryPartial(null, null, "all") as JsonResult;

//            // Assert
//            Assert.NotNull(result);
//            dynamic data = result.Value;
//            Assert.False(data.success);
//        }

//        [Fact]
//        public async Task Cancel_UpdatesStatusAndRedirects()
//        {
//            // Arrange
//            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
//            var mockAccountService = new Mock<IAccountService>();
//            var mockVoucherService = new Mock<IVoucherService>();
//            var mockContext = new Mock<MovieTheaterContext>();
//            var mockHub = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MovieTheater.Hubs.DashboardHub>>();
//            var fakeAccountId = "test-account";
//            var fakeInvoice = new Invoice { InvoiceId = "1", AccountId = fakeAccountId, Status = InvoiceStatus.Completed };
//            mockInvoiceRepo.Setup(r => r.GetForCancelAsync("1", fakeAccountId)).ReturnsAsync(fakeInvoice);
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
//            }, "mock"));
//            var controller = new TicketController(
//                mockContext.Object,
//                mockInvoiceRepo.Object,
//                mockAccountService.Object,
//                mockVoucherService.Object,
//                null,
//                mockHub.Object
//            );
//            controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = new DefaultHttpContext() { User = user }
//            };

//            // Act
//            var result = await controller.Cancel("1", "/MyAccount/MainPage?tab=Profile");

//            // Assert
//            var redirectResult = Assert.IsType<RedirectResult>(result);
//            Assert.Equal("/MyAccount/MainPage?tab=Profile", redirectResult.Url);
//            Assert.Equal(InvoiceStatus.Incomplete, fakeInvoice.Status); // Đã bị hủy
//        }
//    }
//} 