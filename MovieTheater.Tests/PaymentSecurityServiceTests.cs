using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Tests
{
    public class PaymentSecurityServiceTests
    {
        private readonly Mock<ILogger<PaymentSecurityService>> _mockLogger;
        private readonly Mock<VNPayService> _mockVnPayService;
        private readonly DbContextOptions<MovieTheaterContext> _options;
        private readonly PaymentSecurityService _service;

        public PaymentSecurityServiceTests()
        {
            _mockLogger = new Mock<ILogger<PaymentSecurityService>>();
            _mockVnPayService = new Mock<VNPayService>(null);
            
            // Use in-memory database for testing
            _options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _service = new PaymentSecurityService(
                new MovieTheaterContext(_options),
                _mockLogger.Object,
                _mockVnPayService.Object
            );
        }

        [Fact]
        public void ValidatePaymentData_WithNullInvoiceId_ShouldReturnError()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = null, TotalAmount = 100000 };

            // Act
            var result = _service.ValidatePaymentData(model, "user123");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_INVOICE_ID", result.ErrorCode);
            Assert.Contains("Invoice ID không được để trống", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WithEmptyInvoiceId_ShouldReturnError()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "", TotalAmount = 100000 };

            // Act
            var result = _service.ValidatePaymentData(model, "user123");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_INVOICE_ID", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithInvalidAmount_ShouldReturnError()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = -100 };

            // Act
            var result = _service.ValidatePaymentData(model, "user123");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
            Assert.Contains("Số tiền thanh toán không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WithZeroAmount_ShouldReturnError()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 0 };

            // Act
            var result = _service.ValidatePaymentData(model, "user123");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithNonExistentInvoice_ShouldReturnError()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "NONEXISTENT", TotalAmount = 100000 };

            // Act
            var result = _service.ValidatePaymentData(model, "user123");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
            Assert.Contains("Hóa đơn không tồn tại", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WithInvoiceBelongingToDifferentUser_ShouldReturnError()
        {
            // Arrange
            using var context = new MovieTheaterContext(_options);
            
            // Create test data
            var account1 = new Account { AccountId = "user1", Username = "user1" };
            var account2 = new Account { AccountId = "user2", Username = "user2" };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                AccountId = "user1", 
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000
            };
            
            context.Accounts.AddRange(account1, account2);
            context.Invoices.Add(invoice);
            context.SaveChanges();

            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 100000 };

            // Act
            var result = _service.ValidatePaymentData(model, "user2"); // Different user

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("UNAUTHORIZED_ACCESS", result.ErrorCode);
            Assert.Contains("Bạn không có quyền truy cập hóa đơn này", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WithCompletedInvoice_ShouldReturnError()
        {
            // Arrange
            using var context = new MovieTheaterContext(_options);
            
            // Create test data
            var account = new Account { AccountId = "user1", Username = "user1" };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                AccountId = "user1", 
                Status = InvoiceStatus.Completed, // Already completed
                TotalMoney = 100000
            };
            
            context.Accounts.Add(account);
            context.Invoices.Add(invoice);
            context.SaveChanges();

            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 100000 };

            // Act
            var result = _service.ValidatePaymentData(model, "user1");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_ALREADY_PROCESSED", result.ErrorCode);
            Assert.Contains("Hóa đơn đã được xử lý", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            using var context = new MovieTheaterContext(_options);
            
            // Create test data
            var account = new Account { AccountId = "user1", Username = "user1" };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                AccountId = "user1", 
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000
            };
            
            context.Accounts.Add(account);
            context.Invoices.Add(invoice);
            context.SaveChanges();

            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 100000 };

            // Act
            var result = _service.ValidatePaymentData(model, "user1");

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ErrorMessage);
            Assert.Empty(result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WithMatchingAmount_ShouldReturnSuccess()
        {
            // Arrange
            using var context = new MovieTheaterContext(_options);
            
            // Create test data with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000 };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1 };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = 100000,
                Status = InvoiceStatus.Incomplete
            };
            
            context.SeatTypes.Add(seatType);
            context.Seats.Add(seat);
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Act
            var result = _service.ValidateAmount("INV001", 100000);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WithNonMatchingAmount_ShouldReturnError()
        {
            // Arrange
            using var context = new MovieTheaterContext(_options);
            
            // Create test data
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000 };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1 };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = 100000,
                Status = InvoiceStatus.Incomplete
            };
            
            context.SeatTypes.Add(seatType);
            context.Seats.Add(seat);
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Act
            var result = _service.ValidateAmount("INV001", 150000); // Different amount

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("AMOUNT_MISMATCH", result.ErrorCode);
            Assert.Contains("Số tiền thanh toán không khớp với giá trị thực tế", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentResponse_WithValidSignature_ShouldReturnSuccess()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "valid_hash",
                ["vnp_Amount"] = "10000000"
            };

            _mockVnPayService.Setup(x => x.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
                .Returns(true);

            using var context = new MovieTheaterContext(_options);
            var invoice = new Invoice { InvoiceId = "INV001" };
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePaymentResponse_WithInvalidSignature_ShouldReturnError()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "invalid_hash",
                ["vnp_Amount"] = "10000000"
            };

            _mockVnPayService.Setup(x => x.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
                .Returns(false);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_SIGNATURE", result.ErrorCode);
            Assert.Contains("Chữ ký bảo mật không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentResponse_WithFailedResponseCode_ShouldReturnError()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "99", // Failed response code
                ["vnp_SecureHash"] = "valid_hash",
                ["vnp_Amount"] = "10000000"
            };

            _mockVnPayService.Setup(x => x.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("VNPAY_ERROR_99", result.ErrorCode);
            Assert.Contains("Giao dịch thất bại với mã lỗi: 99", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentResponse_WithMissingRequiredFields_ShouldReturnError()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                // Missing vnp_ResponseCode
                ["vnp_SecureHash"] = "valid_hash"
            };

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_VNPAY_RESPONSE", result.ErrorCode);
            Assert.Contains("Dữ liệu phản hồi từ VNPay không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentResponse_WithNonExistentInvoice_ShouldReturnError()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "NONEXISTENT",
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "valid_hash",
                ["vnp_Amount"] = "10000000"
            };

            _mockVnPayService.Setup(x => x.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
            Assert.Contains("Không tìm thấy hóa đơn tương ứng", result.ErrorMessage);
        }
    }
} 