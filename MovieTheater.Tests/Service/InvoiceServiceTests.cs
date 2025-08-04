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
    }
} 