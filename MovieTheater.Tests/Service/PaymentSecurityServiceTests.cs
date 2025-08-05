using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Tests.Service
{
    public class PaymentSecurityServiceTests
    {
        private readonly MovieTheaterContext _context;
        private readonly Mock<ILogger<PaymentSecurityService>> _mockLogger;
        private readonly Mock<IVNPayService> _mockVnPayService;
        private readonly PaymentSecurityService _service;

        public PaymentSecurityServiceTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _mockLogger = new Mock<ILogger<PaymentSecurityService>>();
            _mockVnPayService = new Mock<IVNPayService>();
            _service = new PaymentSecurityService(_context, _mockLogger.Object, _mockVnPayService.Object);
        }

        [Fact]
        public void ValidatePaymentData_WhenInvoiceIdIsEmpty_ReturnsInvalidInvoiceId()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "", TotalAmount = 100 };
            var userId = "user1";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_INVOICE_ID", result.ErrorCode);
            Assert.Contains("Invoice ID không được để trống", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WhenInvoiceIdIsNull_ReturnsInvalidInvoiceId()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = null!, TotalAmount = 100 };
            var userId = "user1";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_INVOICE_ID", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WhenTotalAmountIsZero_ReturnsInvalidAmount()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 0 };
            var userId = "user1";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
            Assert.Contains("Số tiền thanh toán không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WhenTotalAmountIsNegative_ReturnsInvalidAmount()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = -100 };
            var userId = "user1";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WhenInvoiceDoesNotExist_ReturnsInvoiceNotFound()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "INV999", TotalAmount = 100 };
            var userId = "user1";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
            Assert.Contains("Hóa đơn không tồn tại", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WhenInvoiceBelongsToDifferentUser_ReturnsUnauthorizedAccess()
        {
            // Arrange
            var account = new Account { AccountId = "ACC001", Username = "user1" };
            var invoice = new Invoice { InvoiceId = "INV001", AccountId = "ACC001", Status = InvoiceStatus.Incomplete };
            
            _context.Accounts.Add(account);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 100 };
            var userId = "ACC002"; // Different user

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("UNAUTHORIZED_ACCESS", result.ErrorCode);
            Assert.Contains("Bạn không có quyền truy cập hóa đơn này", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WhenInvoiceIsNotIncomplete_ReturnsInvoiceAlreadyProcessed()
        {
            // Arrange
            var account = new Account { AccountId = "ACC001", Username = "user1" };
            var invoice = new Invoice { InvoiceId = "INV001", AccountId = "ACC001", Status = InvoiceStatus.Completed };
            
            _context.Accounts.Add(account);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 100 };
            var userId = "ACC001";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_ALREADY_PROCESSED", result.ErrorCode);
            Assert.Contains("Hóa đơn đã được xử lý", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentData_WhenValidData_ReturnsValid()
        {
            // Arrange
            var account = new Account { AccountId = "ACC001", Username = "user1" };
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000, ColorHex = "#FF0000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, Seat = seat };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                AccountId = "ACC001", 
                Status = InvoiceStatus.Incomplete,
                ScheduleSeats = new List<ScheduleSeat> { scheduleSeat }
            };
            
            _context.Accounts.Add(account);
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var model = new PaymentViewModel { InvoiceId = "INV001", TotalAmount = 100000 };
            var userId = "ACC001";

            // Act
            var result = _service.ValidatePaymentData(model, userId);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WhenInvoiceDoesNotExist_ReturnsInvoiceNotFound()
        {
            // Arrange
            var invoiceId = "INV999";
            var amount = 100m;

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WhenAmountMatches_ReturnsValid()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000, ColorHex = "#FF0000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, Seat = seat };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                Status = InvoiceStatus.Incomplete,
                ScheduleSeats = new List<ScheduleSeat> { scheduleSeat }
            };
            
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var amount = 100000m;

            // Act
            var result = _service.ValidateAmount("INV001", amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WhenAmountDoesNotMatch_ReturnsAmountMismatch()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000, ColorHex = "#FF0000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, Seat = seat };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                Status = InvoiceStatus.Incomplete,
                ScheduleSeats = new List<ScheduleSeat> { scheduleSeat }
            };
            
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var amount = 150000m; // Different amount

            // Act
            var result = _service.ValidateAmount("INV001", amount);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("AMOUNT_MISMATCH", result.ErrorCode);
            Assert.Contains("Số tiền thanh toán không khớp với giá trị thực tế", result.ErrorMessage);
        }

        [Fact]
        public void ValidateAmount_WithPromotionDiscount_CalculatesCorrectAmount()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000, ColorHex = "#FF0000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, Seat = seat };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                Status = InvoiceStatus.Incomplete,
                PromotionDiscount = "{\"seat\": 20}", // 20% discount
                ScheduleSeats = new List<ScheduleSeat> { scheduleSeat }
            };
            
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var amount = 80000m; // 100000 - 20% = 80000

            // Act
            var result = _service.ValidateAmount("INV001", amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WithUsedScore_CalculatesCorrectAmount()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000, ColorHex = "#FF0000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, Seat = seat };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                Status = InvoiceStatus.Incomplete,
                UseScore = 10, // 10 points = 10000 VND
                ScheduleSeats = new List<ScheduleSeat> { scheduleSeat }
            };
            
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var amount = 90000m; // 100000 - 10000 = 90000

            // Act
            var result = _service.ValidateAmount("INV001", amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WithVoucher_CalculatesCorrectAmount()
        {
            // Arrange
            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100000, ColorHex = "#FF0000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, Seat = seat };
            var voucher = new Voucher 
            { 
                VoucherId = "VOUCHER001", 
                AccountId = "ACC001",
                Code = "VOUCHER001",
                Value = 15000,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30)
            };
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                Status = InvoiceStatus.Incomplete,
                VoucherId = "VOUCHER001",
                ScheduleSeats = new List<ScheduleSeat> { scheduleSeat }
            };
            
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Vouchers.Add(voucher);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var amount = 85000m; // 100000 - 15000 = 85000

            // Act
            var result = _service.ValidateAmount("INV001", amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePaymentResponse_WhenMissingRequiredFields_ReturnsInvalidResponse()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_ResponseCode", "00" }
                // Missing vnp_TxnRef
            };

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_VNPAY_RESPONSE", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WhenMissingSignature_ReturnsInvalidSignature()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_TxnRef", "INV001" },
                { "vnp_ResponseCode", "00" }
                // Missing vnp_SecureHash
            };

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_SIGNATURE", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WhenInvalidSignature_ReturnsInvalidSignature()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_TxnRef", "INV001" },
                { "vnp_ResponseCode", "00" },
                { "vnp_SecureHash", "invalid_signature" }
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "invalid_signature"))
                .Returns(false);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_SIGNATURE", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WhenResponseCodeNotSuccess_ReturnsError()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_TxnRef", "INV001" },
                { "vnp_ResponseCode", "99" }, // Error code
                { "vnp_SecureHash", "valid_signature" }
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "valid_signature"))
                .Returns(true);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("VNPAY_ERROR_99", result.ErrorCode);
            Assert.Contains("Giao dịch thất bại với mã lỗi: 99", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePaymentResponse_WhenInvoiceDoesNotExist_ReturnsInvoiceNotFound()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_TxnRef", "INV999" }, // Non-existent invoice
                { "vnp_ResponseCode", "00" },
                { "vnp_SecureHash", "valid_signature" }
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "valid_signature"))
                .Returns(true);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WhenValidResponse_ReturnsValid()
        {
            // Arrange
            var invoice = new Invoice { InvoiceId = "INV001", Status = InvoiceStatus.Incomplete };
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_TxnRef", "INV001" },
                { "vnp_ResponseCode", "00" },
                { "vnp_SecureHash", "valid_signature" }
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "valid_signature"))
                .Returns(true);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.True(result.IsValid);
        }
    }
} 