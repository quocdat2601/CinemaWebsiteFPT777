using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Middleware;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace MovieTheater.Tests
{
    public class PaymentSecurityMiddlewareTests
    {
        private readonly Mock<ILogger<PaymentSecurityMiddleware>> _mockLogger;
        private readonly Mock<IPaymentSecurityService> _mockPaymentSecurityService;
        private readonly PaymentSecurityMiddleware _middleware;

        public PaymentSecurityMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<PaymentSecurityMiddleware>>();
            _mockPaymentSecurityService = new Mock<IPaymentSecurityService>();
            _middleware = new PaymentSecurityMiddleware(null, _mockLogger.Object);
        }

        [Fact]
        public async Task InvokeAsync_NonPaymentRequest_ShouldNotLog()
        {
            // Arrange
            var context = CreateHttpContext("/home/index", "GET");
            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_PaymentApiRequest_ShouldLogAndValidate()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 100000,
                OrderInfo = "Test payment",
                OrderId = "INV001"
            };
            var jsonBody = JsonSerializer.Serialize(paymentRequest);
            var context = CreateHttpContext("/api/payment/create-payment", "POST", jsonBody);
            
            // Mock authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Mock service
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IPaymentSecurityService)))
                .Returns(_mockPaymentSecurityService.Object);
            context.RequestServices = serviceProvider.Object;

            // Mock validation result
            _mockPaymentSecurityService.Setup(x => x.ValidatePaymentData(It.IsAny<PaymentViewModel>(), It.IsAny<string>()))
                .Returns(new PaymentValidationResult { IsValid = true });

            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
            _mockPaymentSecurityService.Verify(x => x.ValidatePaymentData(It.IsAny<PaymentViewModel>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_UnauthenticatedUser_ShouldReturn401()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 100000,
                OrderInfo = "Test payment",
                OrderId = "INV001"
            };
            var jsonBody = JsonSerializer.Serialize(paymentRequest);
            var context = CreateHttpContext("/api/payment/create-payment", "POST", jsonBody);
            
            // No authenticated user
            context.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Mock service
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IPaymentSecurityService)))
                .Returns(_mockPaymentSecurityService.Object);
            context.RequestServices = serviceProvider.Object;

            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_InvalidPaymentData_ShouldReturn400()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = -100, // Invalid amount
                OrderInfo = "Test payment",
                OrderId = "INV001"
            };
            var jsonBody = JsonSerializer.Serialize(paymentRequest);
            var context = CreateHttpContext("/api/payment/create-payment", "POST", jsonBody);
            
            // Mock authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Mock service
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IPaymentSecurityService)))
                .Returns(_mockPaymentSecurityService.Object);
            context.RequestServices = serviceProvider.Object;

            // Mock validation failure
            _mockPaymentSecurityService.Setup(x => x.ValidatePaymentData(It.IsAny<PaymentViewModel>(), It.IsAny<string>()))
                .Returns(new PaymentValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = "Invalid amount",
                    ErrorCode = "INVALID_AMOUNT"
                });

            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_VnPayReturnRequest_ShouldOnlyLog()
        {
            // Arrange
            var context = CreateHttpContext("/api/payment/vnpay-return", "GET");
            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
            _mockPaymentSecurityService.Verify(x => x.ValidatePaymentData(It.IsAny<PaymentViewModel>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_EmptyJsonBody_ShouldContinue()
        {
            // Arrange
            var context = CreateHttpContext("/api/payment/create-payment", "POST", "");
            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            // Should not throw exception and should continue
            _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_InvalidJsonBody_ShouldContinue()
        {
            // Arrange
            var context = CreateHttpContext("/api/payment/create-payment", "POST", "invalid json");
            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            // Should not throw exception and should continue
            _mockLogger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_ServiceNotResolved_ShouldContinue()
        {
            // Arrange
            var paymentRequest = new PaymentRequest
            {
                Amount = 100000,
                OrderInfo = "Test payment",
                OrderId = "INV001"
            };
            var jsonBody = JsonSerializer.Serialize(paymentRequest);
            var context = CreateHttpContext("/api/payment/create-payment", "POST", jsonBody);
            
            // Mock authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Mock service not found
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IPaymentSecurityService)))
                .Returns((IPaymentSecurityService)null);
            context.RequestServices = serviceProvider.Object;

            var middleware = new PaymentSecurityMiddleware(async (ctx) => await Task.CompletedTask, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            // Should log warning and continue
            _mockLogger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }

        private HttpContext CreateHttpContext(string path, string method, string body = "")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = method;
            
            if (!string.IsNullOrEmpty(body))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                context.Request.Body = new MemoryStream(bodyBytes);
                context.Request.ContentLength = bodyBytes.Length;
            }

            return context;
        }
    }

    // Mock classes for testing
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; }
        public string OrderId { get; set; }
    }
} 