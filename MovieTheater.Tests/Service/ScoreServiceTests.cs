using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class ScoreServiceTests
    {
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
        private readonly Mock<IMemberRepository> _mockMemberRepository;
        private readonly ScoreService _service;

        public ScoreServiceTests()
        {
            _mockInvoiceRepository = new Mock<IInvoiceRepository>();
            _mockMemberRepository = new Mock<IMemberRepository>();
            _service = new ScoreService(_mockInvoiceRepository.Object, _mockMemberRepository.Object);
        }

        [Fact]
        public void GetCurrentScore_WithValidAccountId_ReturnsMemberScore()
        {
            // Arrange
            var accountId = "ACC001";
            var member = new Member
            {
                AccountId = accountId,
                Score = 1500
            };
            _mockMemberRepository.Setup(r => r.GetByAccountId(accountId))
                .Returns(member);

            // Act
            var result = _service.GetCurrentScore(accountId);

            // Assert
            Assert.Equal(1500, result);
            _mockMemberRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetCurrentScore_WhenMemberNotFound_ReturnsZero()
        {
            // Arrange
            var accountId = "ACC001";
            _mockMemberRepository.Setup(r => r.GetByAccountId(accountId))
                .Returns((Member)null);

            // Act
            var result = _service.GetCurrentScore(accountId);

            // Assert
            Assert.Equal(0, result);
            _mockMemberRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetCurrentScore_WhenMemberScoreIsNull_ReturnsZero()
        {
            // Arrange
            var accountId = "ACC001";
            var member = new Member
            {
                AccountId = accountId,
                Score = null
            };
            _mockMemberRepository.Setup(r => r.GetByAccountId(accountId))
                .Returns(member);

            // Act
            var result = _service.GetCurrentScore(accountId);

            // Assert
            Assert.Equal(0, result);
            _mockMemberRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithValidAccountId_ReturnsScoreHistory()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = accountId,
                    AddScore = 0,
                    UseScore = 50,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie 2" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("add", result[0].Type);
            Assert.Equal(100, result[0].Score);
            Assert.Equal("use", result[1].Type);
            Assert.Equal(50, result[1].Score);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithDateRange_ReturnsFilteredHistory()
        {
            // Arrange
            var accountId = "ACC001";
            var fromDate = DateTime.Now.AddDays(-7);
            var toDate = DateTime.Now;
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByDateRangeAsync(accountId, fromDate, toDate))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId, fromDate, toDate);

            // Assert
            Assert.Single(result);
            Assert.Equal("add", result[0].Type);
            Assert.Equal(100, result[0].Score);
            _mockInvoiceRepository.Verify(r => r.GetByDateRangeAsync(accountId, fromDate, toDate), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithHistoryTypeAdd_ReturnsOnlyAddHistory()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = accountId,
                    AddScore = 0,
                    UseScore = 50,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie 2" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId, historyType: "add");

            // Assert
            Assert.Single(result);
            Assert.Equal("add", result[0].Type);
            Assert.Equal(100, result[0].Score);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithHistoryTypeUse_ReturnsOnlyUseHistory()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = accountId,
                    AddScore = 0,
                    UseScore = 50,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie 2" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId, historyType: "use");

            // Assert
            Assert.Single(result);
            Assert.Equal("use", result[0].Type);
            Assert.Equal(50, result[0].Score);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithHistoryTypeAll_ReturnsAllHistory()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = accountId,
                    AddScore = 0,
                    UseScore = 50,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie 2" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId, historyType: "all");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("add", result[0].Type);
            Assert.Equal("use", result[1].Type);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithCanceledInvoices_ExcludesCanceledInvoices()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = true, // Canceled invoice
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = accountId,
                    AddScore = 200,
                    UseScore = 0,
                    Cancel = false, // Valid invoice
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie 2" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId);

            // Assert
            Assert.Single(result);
            Assert.Equal("add", result[0].Type);
            Assert.Equal(200, result[0].Score);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithZeroScores_ExcludesZeroScores()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 0, // Zero score
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now.AddDays(-1),
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie 2" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId);

            // Assert
            Assert.Single(result);
            Assert.Equal("add", result[0].Type);
            Assert.Equal(100, result[0].Score);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithNullMovieShow_ReturnsNAsMovieName()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = null // Null MovieShow
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId);

            // Assert
            Assert.Single(result);
            Assert.Equal("N/A", result[0].MovieName);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithNullBookingDate_ReturnsMinValue()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 0,
                    Cancel = false,
                    BookingDate = null, // Null BookingDate
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId);

            // Assert
            Assert.Single(result);
            Assert.Equal(DateTime.MinValue, result[0].DateCreated);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithEmptyInvoices_ReturnsEmptyList()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>();
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId);

            // Assert
            Assert.Empty(result);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }

        [Fact]
        public void GetScoreHistory_WithNullHistoryType_ReturnsAllHistory()
        {
            // Arrange
            var accountId = "ACC001";
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = accountId,
                    AddScore = 100,
                    UseScore = 50,
                    Cancel = false,
                    BookingDate = DateTime.Now,
                    MovieShow = new MovieShow
                    {
                        Movie = new Movie { MovieNameEnglish = "Test Movie" }
                    }
                }
            };
            _mockInvoiceRepository.Setup(r => r.GetByAccountIdAsync(accountId, null, null))
                .ReturnsAsync(invoices);

            // Act
            var result = _service.GetScoreHistory(accountId, historyType: null);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("add", result[0].Type);
            Assert.Equal("use", result[1].Type);
            _mockInvoiceRepository.Verify(r => r.GetByAccountIdAsync(accountId, null, null), Times.Once);
        }
    }
} 