using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class InvoiceServiceTests
    {
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
        private readonly Mock<IVoucherRepository> _mockVoucherRepository;
        private readonly InvoiceService _service;

        public InvoiceServiceTests()
        {
            _mockInvoiceRepository = new Mock<IInvoiceRepository>();
            _mockVoucherRepository = new Mock<IVoucherRepository>();
            _service = new InvoiceService(_mockInvoiceRepository.Object, _mockVoucherRepository.Object);
        }

        [Fact]
        public void GetAll_WithValidData_ReturnsAllInvoices()
        {
            // Arrange
            var expectedInvoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV001", TotalMoney = 750m, Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "INV002", TotalMoney = 500m, Status = InvoiceStatus.Incomplete }
            };

            _mockInvoiceRepository.Setup(x => x.GetAll()).Returns(expectedInvoices);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            var invoices = result.ToList();
            Assert.Equal(2, invoices.Count);
            Assert.Equal("INV001", invoices[0].InvoiceId);
            Assert.Equal("INV002", invoices[1].InvoiceId);
            _mockInvoiceRepository.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAll_WithEmptyData_ReturnsEmptyList()
        {
            // Arrange
            var expectedInvoices = new List<Invoice>();
            _mockInvoiceRepository.Setup(x => x.GetAll()).Returns(expectedInvoices);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            var invoices = result.ToList();
            Assert.Empty(invoices);
            _mockInvoiceRepository.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WithValidId_ReturnsInvoice()
        {
            // Arrange
            var invoiceId = "INV001";
            var expectedInvoice = new Invoice 
            { 
                InvoiceId = invoiceId, 
                TotalMoney = 750m, 
                Status = InvoiceStatus.Completed 
            };

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(expectedInvoice);

            // Act
            var result = _service.GetById(invoiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(invoiceId, result.InvoiceId);
            Assert.Equal(750m, result.TotalMoney);
            Assert.Equal(InvoiceStatus.Completed, result.Status);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
        }

        [Fact]
        public void GetById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invoiceId = "INVALID_ID";
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            var result = _service.GetById(invoiceId);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
        }

        [Fact]
        public void GetById_WithNullId_ReturnsNull()
        {
            // Arrange
            string? invoiceId = null;
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            var result = _service.GetById(invoiceId);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
        }

        [Fact]
        public void GetById_WithEmptyId_ReturnsNull()
        {
            // Arrange
            var invoiceId = "";
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            var result = _service.GetById(invoiceId);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
        }

        [Fact]
        public void Update_WithValidInvoice_CallsRepositoryUpdate()
        {
            // Arrange
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = 750m, 
                Status = InvoiceStatus.Completed 
            };

            // Act
            _service.Update(invoice);

            // Assert
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
        }

        [Fact]
        public void Update_WithNullInvoice_CallsRepositoryUpdate()
        {
            // Arrange
            Invoice? invoice = null;

            // Act
            _service.Update(invoice);

            // Assert
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
        }

        [Fact]
        public void Update_WithInvoiceHavingNullValues_CallsRepositoryUpdate()
        {
            // Arrange
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = null, 
                Status = InvoiceStatus.Incomplete 
            };

            // Act
            _service.Update(invoice);

            // Assert
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
        }

        [Fact]
        public void Save_CallsRepositorySave()
        {
            // Arrange
            // No setup needed

            // Act
            _service.Save();

            // Assert
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void Save_WhenCalledMultipleTimes_CallsRepositorySaveEachTime()
        {
            // Arrange
            // No setup needed

            // Act
            _service.Save();
            _service.Save();
            _service.Save();

            // Assert
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Exactly(3));
        }

        [Fact]
        public void GetAll_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database connection failed");
            _mockInvoiceRepository.Setup(x => x.GetAll()).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _service.GetAll());
            Assert.Equal("Database connection failed", exception.Message);
            _mockInvoiceRepository.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var invoiceId = "INV001";
            var expectedException = new InvalidOperationException("Database connection failed");
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _service.GetById(invoiceId));
            Assert.Equal("Database connection failed", exception.Message);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
        }

        [Fact]
        public void Update_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var invoice = new Invoice { InvoiceId = "INV001", TotalMoney = 750m };
            var expectedException = new InvalidOperationException("Database connection failed");
            _mockInvoiceRepository.Setup(x => x.Update(invoice)).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _service.Update(invoice));
            Assert.Equal("Database connection failed", exception.Message);
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
        }

        [Fact]
        public void Save_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database connection failed");
            _mockInvoiceRepository.Setup(x => x.Save()).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _service.Save());
            Assert.Equal("Database connection failed", exception.Message);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void GetAll_WithLargeDataSet_ReturnsAllInvoices()
        {
            // Arrange
            var expectedInvoices = new List<Invoice>();
            for (int i = 1; i <= 100; i++)
            {
                expectedInvoices.Add(new Invoice 
                { 
                    InvoiceId = $"INV{i:D3}", 
                    TotalMoney = 100m * i, 
                    Status = InvoiceStatus.Completed 
                });
            }

            _mockInvoiceRepository.Setup(x => x.GetAll()).Returns(expectedInvoices);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            var invoices = result.ToList();
            Assert.Equal(100, invoices.Count);
            Assert.Equal("INV001", invoices[0].InvoiceId);
            Assert.Equal("INV100", invoices[99].InvoiceId);
            _mockInvoiceRepository.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WithSpecialCharacters_ReturnsCorrectInvoice()
        {
            // Arrange
            var invoiceId = "INV-001_SPECIAL@#$%";
            var expectedInvoice = new Invoice 
            { 
                InvoiceId = invoiceId, 
                TotalMoney = 750m, 
                Status = InvoiceStatus.Completed 
            };

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(expectedInvoice);

            // Act
            var result = _service.GetById(invoiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(invoiceId, result.InvoiceId);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
        }

        [Fact]
        public void Update_WithInvoiceHavingAllProperties_CallsRepositoryUpdate()
        {
            // Arrange
            var invoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = 750m, 
                Status = InvoiceStatus.Completed,
                BookingDate = DateTime.Today,
                Cancel = false,
                Seat = "A1,A2",
                AccountId = "ACC001",
                MovieShowId = 1
            };

            // Act
            _service.Update(invoice);

            // Assert
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
        }

        [Fact]
        public async Task MarkInvoiceAsCompletedAsync_WithValidInvoice_UpdatesStatus()
        {
            // Arrange
            var invoiceId = "INV001";
            var invoice = new Invoice 
            { 
                InvoiceId = invoiceId, 
                Status = InvoiceStatus.Incomplete 
            };

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(invoice);

            // Act
            await _service.MarkInvoiceAsCompletedAsync(invoiceId);

            // Assert
            Assert.Equal(InvoiceStatus.Completed, invoice.Status);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public async Task MarkInvoiceAsCompletedAsync_WithAlreadyCompletedInvoice_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INV001";
            var invoice = new Invoice 
            { 
                InvoiceId = invoiceId, 
                Status = InvoiceStatus.Completed 
            };

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(invoice);

            // Act
            await _service.MarkInvoiceAsCompletedAsync(invoiceId);

            // Assert
            Assert.Equal(InvoiceStatus.Completed, invoice.Status);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(It.IsAny<Invoice>()), Times.Never);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task MarkInvoiceAsCompletedAsync_WithNullInvoice_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INV001";
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            await _service.MarkInvoiceAsCompletedAsync(invoiceId);

            // Assert
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(It.IsAny<Invoice>()), Times.Never);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task MarkInvoiceAsCompletedAsync_WithInvalidId_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INVALID_ID";
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            await _service.MarkInvoiceAsCompletedAsync(invoiceId);

            // Assert
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(It.IsAny<Invoice>()), Times.Never);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task MarkVoucherAsUsedAsync_WithValidVoucher_UpdatesIsUsed()
        {
            // Arrange
            var voucherId = "VOUCHER001";
            var voucher = new Voucher 
            { 
                VoucherId = voucherId, 
                IsUsed = false 
            };

            _mockVoucherRepository.Setup(x => x.GetById(voucherId)).Returns(voucher);

            // Act
            await _service.MarkVoucherAsUsedAsync(voucherId);

            // Assert
            Assert.True(voucher.IsUsed);
            _mockVoucherRepository.Verify(x => x.GetById(voucherId), Times.Once);
            _mockVoucherRepository.Verify(x => x.Update(voucher), Times.Once);
            _mockVoucherRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public async Task MarkVoucherAsUsedAsync_WithAlreadyUsedVoucher_DoesNotUpdate()
        {
            // Arrange
            var voucherId = "VOUCHER001";
            var voucher = new Voucher 
            { 
                VoucherId = voucherId, 
                IsUsed = true 
            };

            _mockVoucherRepository.Setup(x => x.GetById(voucherId)).Returns(voucher);

            // Act
            await _service.MarkVoucherAsUsedAsync(voucherId);

            // Assert
            Assert.True(voucher.IsUsed);
            _mockVoucherRepository.Verify(x => x.GetById(voucherId), Times.Once);
            _mockVoucherRepository.Verify(x => x.Update(It.IsAny<Voucher>()), Times.Never);
            _mockVoucherRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task MarkVoucherAsUsedAsync_WithNullVoucher_DoesNotUpdate()
        {
            // Arrange
            var voucherId = "VOUCHER001";
            _mockVoucherRepository.Setup(x => x.GetById(voucherId)).Returns((Voucher?)null);

            // Act
            await _service.MarkVoucherAsUsedAsync(voucherId);

            // Assert
            _mockVoucherRepository.Verify(x => x.GetById(voucherId), Times.Once);
            _mockVoucherRepository.Verify(x => x.Update(It.IsAny<Voucher>()), Times.Never);
            _mockVoucherRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task MarkVoucherAsUsedAsync_WithInvalidId_DoesNotUpdate()
        {
            // Arrange
            var voucherId = "INVALID_ID";
            _mockVoucherRepository.Setup(x => x.GetById(voucherId)).Returns((Voucher?)null);

            // Act
            await _service.MarkVoucherAsUsedAsync(voucherId);

            // Assert
            _mockVoucherRepository.Verify(x => x.GetById(voucherId), Times.Once);
            _mockVoucherRepository.Verify(x => x.Update(It.IsAny<Voucher>()), Times.Never);
            _mockVoucherRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task UpdateInvoiceStatusAsync_WithValidInvoice_UpdatesStatus()
        {
            // Arrange
            var invoiceId = "INV001";
            var newStatus = InvoiceStatus.Completed; // Changed from Cancelled to Completed
            var invoice = new Invoice 
            { 
                InvoiceId = invoiceId, 
                Status = InvoiceStatus.Incomplete 
            };

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(invoice);

            // Act
            await _service.UpdateInvoiceStatusAsync(invoiceId, newStatus);

            // Assert
            Assert.Equal(newStatus, invoice.Status);
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(invoice), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public async Task UpdateInvoiceStatusAsync_WithNullInvoice_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INV001";
            var newStatus = InvoiceStatus.Completed; // Changed from Cancelled to Completed
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            await _service.UpdateInvoiceStatusAsync(invoiceId, newStatus);

            // Assert
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(It.IsAny<Invoice>()), Times.Never);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public async Task UpdateInvoiceStatusAsync_WithInvalidId_DoesNotUpdate()
        {
            // Arrange
            var invoiceId = "INVALID_ID";
            var newStatus = InvoiceStatus.Completed; // Changed from Cancelled to Completed
            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns((Invoice?)null);

            // Act
            await _service.UpdateInvoiceStatusAsync(invoiceId, newStatus);

            // Assert
            _mockInvoiceRepository.Verify(x => x.GetById(invoiceId), Times.Once);
            _mockInvoiceRepository.Verify(x => x.Update(It.IsAny<Invoice>()), Times.Never);
            _mockInvoiceRepository.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public void FindInvoiceByOrderId_WithValidOrderId_ReturnsInvoice()
        {
            // Arrange
            var orderId = "ORDER001";
            var expectedInvoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = 750m 
            };

            _mockInvoiceRepository.Setup(x => x.FindInvoiceByOrderId(orderId)).Returns(expectedInvoice);

            // Act
            var result = _service.FindInvoiceByOrderId(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("INV001", result.InvoiceId);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByOrderId(orderId), Times.Once);
        }

        [Fact]
        public void FindInvoiceByOrderId_WithInvalidOrderId_ReturnsNull()
        {
            // Arrange
            var orderId = "INVALID_ORDER";
            _mockInvoiceRepository.Setup(x => x.FindInvoiceByOrderId(orderId)).Returns((Invoice?)null);

            // Act
            var result = _service.FindInvoiceByOrderId(orderId);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByOrderId(orderId), Times.Once);
        }

        [Fact]
        public void FindInvoiceByOrderId_WithNullOrderId_ReturnsNull()
        {
            // Arrange
            string? orderId = null;
            _mockInvoiceRepository.Setup(x => x.FindInvoiceByOrderId(orderId)).Returns((Invoice?)null);

            // Act
            var result = _service.FindInvoiceByOrderId(orderId);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByOrderId(orderId), Times.Once);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithValidAmount_ReturnsInvoice()
        {
            // Arrange
            var amount = 750m;
            var expectedInvoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = amount,
                BookingDate = DateTime.Today 
            };

            _mockInvoiceRepository.Setup(x => x.FindInvoiceByAmountAndTime(amount, null)).Returns(expectedInvoice);

            // Act
            var result = _service.FindInvoiceByAmountAndTime(amount);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(amount, result.TotalMoney);
            Assert.Equal("INV001", result.InvoiceId);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByAmountAndTime(amount, null), Times.Once);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithAmountAndTime_ReturnsInvoice()
        {
            // Arrange
            var amount = 750m;
            var recentTime = DateTime.Today.AddDays(-1);
            var expectedInvoice = new Invoice 
            { 
                InvoiceId = "INV001", 
                TotalMoney = amount,
                BookingDate = recentTime 
            };

            _mockInvoiceRepository.Setup(x => x.FindInvoiceByAmountAndTime(amount, recentTime)).Returns(expectedInvoice);

            // Act
            var result = _service.FindInvoiceByAmountAndTime(amount, recentTime);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(amount, result.TotalMoney);
            Assert.Equal("INV001", result.InvoiceId);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByAmountAndTime(amount, recentTime), Times.Once);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithInvalidAmount_ReturnsNull()
        {
            // Arrange
            var amount = 999999m;
            _mockInvoiceRepository.Setup(x => x.FindInvoiceByAmountAndTime(amount, null)).Returns((Invoice?)null);

            // Act
            var result = _service.FindInvoiceByAmountAndTime(amount);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByAmountAndTime(amount, null), Times.Once);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WithZeroAmount_ReturnsNull()
        {
            // Arrange
            var amount = 0m;
            _mockInvoiceRepository.Setup(x => x.FindInvoiceByAmountAndTime(amount, null)).Returns((Invoice?)null);

            // Act
            var result = _service.FindInvoiceByAmountAndTime(amount);

            // Assert
            Assert.Null(result);
            _mockInvoiceRepository.Verify(x => x.FindInvoiceByAmountAndTime(amount, null), Times.Once);
        }

        [Fact]
        public async Task MarkInvoiceAsCompletedAsync_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var invoiceId = "INV001";
            var invoice = new Invoice { InvoiceId = invoiceId, Status = InvoiceStatus.Incomplete };
            var expectedException = new InvalidOperationException("Database error");

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(invoice);
            _mockInvoiceRepository.Setup(x => x.Update(invoice)).Throws(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.MarkInvoiceAsCompletedAsync(invoiceId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task MarkVoucherAsUsedAsync_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var voucherId = "VOUCHER001";
            var voucher = new Voucher { VoucherId = voucherId, IsUsed = false };
            var expectedException = new InvalidOperationException("Database error");

            _mockVoucherRepository.Setup(x => x.GetById(voucherId)).Returns(voucher);
            _mockVoucherRepository.Setup(x => x.Update(voucher)).Throws(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.MarkVoucherAsUsedAsync(voucherId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public async Task UpdateInvoiceStatusAsync_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var invoiceId = "INV001";
            var newStatus = InvoiceStatus.Completed; // Changed from Cancelled to Completed
            var invoice = new Invoice { InvoiceId = invoiceId, Status = InvoiceStatus.Incomplete };
            var expectedException = new InvalidOperationException("Database error");

            _mockInvoiceRepository.Setup(x => x.GetById(invoiceId)).Returns(invoice);
            _mockInvoiceRepository.Setup(x => x.Update(invoice)).Throws(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateInvoiceStatusAsync(invoiceId, newStatus));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public void FindInvoiceByOrderId_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var orderId = "ORDER001";
            var expectedException = new InvalidOperationException("Database error");
            _mockInvoiceRepository.Setup(x => x.FindInvoiceByOrderId(orderId)).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _service.FindInvoiceByOrderId(orderId));
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public void FindInvoiceByAmountAndTime_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            var amount = 750m;
            var expectedException = new InvalidOperationException("Database error");
            _mockInvoiceRepository.Setup(x => x.FindInvoiceByAmountAndTime(amount, null)).Throws(expectedException);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _service.FindInvoiceByAmountAndTime(amount));
            Assert.Equal("Database error", exception.Message);
        }
    }
} 