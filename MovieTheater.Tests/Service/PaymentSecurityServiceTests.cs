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
        private readonly Mock<MovieTheaterContext> _mockContext;
        private readonly Mock<ILogger<PaymentSecurityService>> _mockLogger;
        private readonly Mock<IVNPayService> _mockVnPayService;
        private readonly PaymentSecurityService _service;

        public PaymentSecurityServiceTests()
        {
            _mockContext = new Mock<MovieTheaterContext>();
            _mockLogger = new Mock<ILogger<PaymentSecurityService>>();
            _mockVnPayService = new Mock<IVNPayService>();
            _service = new PaymentSecurityService(_mockContext.Object, _mockLogger.Object, _mockVnPayService.Object);
        }

        [Fact]
        public void ValidatePaymentData_WithValidData_ReturnsValidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "INV001",
                TotalAmount = 100000
            };

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = "USER001",
                    Status = InvoiceStatus.Incomplete,
                    PromotionDiscount = "0",
                    UseScore = 0,
                    VoucherId = null,
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ErrorMessage);
            Assert.Empty(result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithNullInvoiceId_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = null,
                TotalAmount = 100000
            };

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invoice ID không được để trống", result.ErrorMessage);
            Assert.Equal("INVALID_INVOICE_ID", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithEmptyInvoiceId_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "",
                TotalAmount = 100000
            };

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invoice ID không được để trống", result.ErrorMessage);
            Assert.Equal("INVALID_INVOICE_ID", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithInvalidAmount_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "INV001",
                TotalAmount = 0
            };

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Số tiền thanh toán không hợp lệ", result.ErrorMessage);
            Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithNegativeAmount_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "INV001",
                TotalAmount = -1000
            };

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Số tiền thanh toán không hợp lệ", result.ErrorMessage);
            Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithNonExistentInvoice_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "INV999",
                TotalAmount = 100000
            };

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>().AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Hóa đơn không tồn tại", result.ErrorMessage);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithUnauthorizedUser_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "INV001",
                TotalAmount = 100000
            };

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = "USER002", // Different user
                    Status = InvoiceStatus.Incomplete
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Bạn không có quyền truy cập hóa đơn này", result.ErrorMessage);
            Assert.Equal("UNAUTHORIZED_ACCESS", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentData_WithProcessedInvoice_ReturnsInvalidResult()
        {
            // Arrange
            var model = new PaymentViewModel
            {
                InvoiceId = "INV001",
                TotalAmount = 100000
            };

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = "USER001",
                    Status = InvoiceStatus.Completed // Already processed
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidatePaymentData(model, "USER001");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Hóa đơn đã được xử lý", result.ErrorMessage);
            Assert.Equal("INVOICE_ALREADY_PROCESSED", result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WithValidAmount_ReturnsValidResult()
        {
            // Arrange
            var invoiceId = "INV001";
            var amount = 100000m;

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    PromotionDiscount = "0",
                    UseScore = 0,
                    VoucherId = null,
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ErrorMessage);
            Assert.Empty(result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WithNonExistentInvoice_ReturnsInvalidResult()
        {
            // Arrange
            var invoiceId = "INV999";
            var amount = 100000m;

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>().AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Hóa đơn không tồn tại", result.ErrorMessage);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WithAmountMismatch_ReturnsInvalidResult()
        {
            // Arrange
            var invoiceId = "INV001";
            var amount = 150000m; // Different from calculated amount

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    PromotionDiscount = "0",
                    UseScore = 0,
                    VoucherId = null,
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Số tiền thanh toán không khớp với giá trị thực tế", result.ErrorMessage);
            Assert.Equal("AMOUNT_MISMATCH", result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WithPromotionDiscount_CalculatesCorrectAmount()
        {
            // Arrange
            var invoiceId = "INV001";
            var amount = 90000m; // 100000 - 10% discount

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    PromotionDiscount = "{\"seat\": 10}", // 10% discount
                    UseScore = 0,
                    VoucherId = null,
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WithUsedScore_CalculatesCorrectAmount()
        {
            // Arrange
            var invoiceId = "INV001";
            var amount = 90000m; // 100000 - 10000 (10 points * 1000)

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    PromotionDiscount = "0",
                    UseScore = 10, // 10 points = 10000 VND
                    VoucherId = null,
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidatePaymentResponse_WithValidData_ReturnsValidResult()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "valid_signature"
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "valid_signature")).Returns(true);

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV001" }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ErrorMessage);
            Assert.Empty(result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WithMissingRequiredFields_ReturnsInvalidResult()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001"
                // Missing vnp_ResponseCode
            };

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Dữ liệu phản hồi từ VNPay không hợp lệ", result.ErrorMessage);
            Assert.Equal("INVALID_VNPAY_RESPONSE", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WithMissingSignature_ReturnsInvalidResult()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "00"
                // Missing vnp_SecureHash
            };

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Chữ ký bảo mật không hợp lệ", result.ErrorMessage);
            Assert.Equal("INVALID_SIGNATURE", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WithInvalidSignature_ReturnsInvalidResult()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "invalid_signature"
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "invalid_signature")).Returns(false);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Chữ ký bảo mật không hợp lệ", result.ErrorMessage);
            Assert.Equal("INVALID_SIGNATURE", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WithFailedResponseCode_ReturnsInvalidResult()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV001",
                ["vnp_ResponseCode"] = "07", // Failed response code
                ["vnp_SecureHash"] = "valid_signature"
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "valid_signature")).Returns(true);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Giao dịch thất bại với mã lỗi: 07", result.ErrorMessage);
            Assert.Equal("VNPAY_ERROR_07", result.ErrorCode);
        }

        [Fact]
        public void ValidatePaymentResponse_WithNonExistentInvoice_ReturnsInvalidResult()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                ["vnp_TxnRef"] = "INV999",
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "valid_signature"
            };

            _mockVnPayService.Setup(s => s.ValidateSignature(vnpayData, "valid_signature")).Returns(true);

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var invoices = new List<Invoice>().AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.ValidatePaymentResponse(vnpayData);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Không tìm thấy hóa đơn tương ứng", result.ErrorMessage);
            Assert.Equal("INVOICE_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ValidateAmount_WithVoucherDiscount_CalculatesCorrectAmount()
        {
            // Arrange
            var invoiceId = "INV001";
            var amount = 90000m; // 100000 - 10000 voucher

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var mockVouchers = new Mock<DbSet<Voucher>>();
            
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    PromotionDiscount = "0",
                    UseScore = 0,
                    VoucherId = "VOUCHER001",
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            var vouchers = new List<Voucher>
            {
                new Voucher { VoucherId = "VOUCHER001", Value = 10000 }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.Provider).Returns(vouchers.Provider);
            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.Expression).Returns(vouchers.Expression);
            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.ElementType).Returns(vouchers.ElementType);
            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.GetEnumerator()).Returns(vouchers.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _mockContext.Setup(c => c.Vouchers).Returns(mockVouchers.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateAmount_WithComplexCalculation_HandlesAllDiscounts()
        {
            // Arrange
            var invoiceId = "INV001";
            var amount = 80000m; // 100000 - 10% promo (10000) - 10000 voucher = 80000

            var mockInvoices = new Mock<DbSet<Invoice>>();
            var mockVouchers = new Mock<DbSet<Voucher>>();
            
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    PromotionDiscount = "{\"seat\": 10}", // 10% discount
                    UseScore = 0,
                    VoucherId = "VOUCHER001",
                    ScheduleSeats = new List<ScheduleSeat>
                    {
                        new ScheduleSeat
                        {
                            Seat = new Seat
                            {
                                SeatType = new SeatType { PricePercent = 100000 }
                            }
                        }
                    }
                }
            }.AsQueryable();

            var vouchers = new List<Voucher>
            {
                new Voucher { VoucherId = "VOUCHER001", Value = 10000 }
            }.AsQueryable();

            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.Provider).Returns(vouchers.Provider);
            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.Expression).Returns(vouchers.Expression);
            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.ElementType).Returns(vouchers.ElementType);
            mockVouchers.As<IQueryable<Voucher>>().Setup(m => m.GetEnumerator()).Returns(vouchers.GetEnumerator());

            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _mockContext.Setup(c => c.Vouchers).Returns(mockVouchers.Object);

            // Act
            var result = _service.ValidateAmount(invoiceId, amount);

            // Assert
            Assert.True(result.IsValid);
        }
    }
} 