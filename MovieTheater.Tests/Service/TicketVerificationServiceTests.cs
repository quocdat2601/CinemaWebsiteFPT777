using Xunit;
using Moq;
using MovieTheater.Service;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TicketVerificationResultViewModel = MovieTheater.ViewModels.TicketVerificationResultViewModel;

namespace MovieTheater.Tests.Service
{
    public class TicketVerificationServiceTests
    {
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<IScheduleSeatRepository> _mockScheduleSeatRepo;
        private readonly Mock<IMemberRepository> _mockMemberRepo;
        private readonly Mock<ISeatService> _mockSeatService;
        private readonly Mock<ISeatTypeService> _mockSeatTypeService;
        private readonly Mock<ILogger<TicketVerificationService>> _mockLogger;
        private readonly TicketVerificationService _service;

        public TicketVerificationServiceTests()
        {
            _mockInvoiceService = new Mock<IInvoiceService>();
            _mockScheduleSeatRepo = new Mock<IScheduleSeatRepository>();
            _mockMemberRepo = new Mock<IMemberRepository>();
            _mockSeatService = new Mock<ISeatService>();
            _mockSeatTypeService = new Mock<ISeatTypeService>();
            _mockLogger = new Mock<ILogger<TicketVerificationService>>();
            _service = new TicketVerificationService(
                _mockInvoiceService.Object,
                _mockScheduleSeatRepo.Object,
                _mockMemberRepo.Object,
                _mockSeatService.Object,
                _mockSeatTypeService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void VerifyTicket_InvoiceNotFound_ReturnsFail()
        {
            _mockInvoiceService.Setup(s => s.GetById("INV404")).Returns((Invoice)null);
            var result = _service.VerifyTicket("INV404");
            Assert.False(result.IsSuccess);
            Assert.Equal("Không tìm thấy vé", result.Message);
        }

        [Fact]
        public void VerifyTicket_NotPaid_ReturnsFail()
        {
            _mockInvoiceService.Setup(s => s.GetById("INV1")).Returns(new Invoice { InvoiceId = "INV1", Status = InvoiceStatus.Incomplete });
            var result = _service.VerifyTicket("INV1");
            Assert.False(result.IsSuccess);
            Assert.Equal("Vé chưa thanh toán", result.Message);
        }

        [Fact]
        public void VerifyTicket_AlreadyUsed_ReturnsFail()
        {
            var invoice = new Invoice { InvoiceId = "INV2", Status = InvoiceStatus.Completed };
            _mockInvoiceService.Setup(s => s.GetById("INV2")).Returns(invoice);
            _mockScheduleSeatRepo.Setup(r => r.GetByInvoiceId("INV2")).Returns(new List<ScheduleSeat> { new ScheduleSeat { SeatStatusId = 2 } });
            var result = _service.VerifyTicket("INV2");
            Assert.False(result.IsSuccess);
            Assert.Equal("Vé đã được sử dụng", result.Message);
        }

        [Fact]
        public void VerifyTicket_Valid_ReturnsSuccess()
        {
            var invoice = new Invoice { InvoiceId = "INV3", Status = InvoiceStatus.Completed, Seat = "A1", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie" }, ShowDate = DateOnly.FromDateTime(DateTime.Today), Schedule = new Schedule { ScheduleTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)) } }, TotalMoney = 100000, AccountId = "acc1" };
            _mockInvoiceService.Setup(s => s.GetById("INV3")).Returns(invoice);
            _mockScheduleSeatRepo.Setup(r => r.GetByInvoiceId("INV3")).Returns(new List<ScheduleSeat> { new ScheduleSeat { SeatStatusId = 1 } });
            _mockMemberRepo.Setup(r => r.GetByAccountId("acc1")).Returns(new Member { Account = new Account { FullName = "Test User", PhoneNumber = "0123456789" } });
            var result = _service.VerifyTicket("INV3");
            Assert.True(result.IsSuccess);
            Assert.Equal("Xác thực vé thành công!", result.Message);
            Assert.Equal("INV3", result.InvoiceId);
            Assert.Equal("Test Movie", result.MovieName);
        }

        [Fact]
        public void GetTicketInfo_InvoiceNotFound_ReturnsFail()
        {
            _mockInvoiceService.Setup(s => s.GetById("INV404")).Returns((Invoice)null);
            var result = _service.GetTicketInfo("INV404");
            Assert.False(result.IsSuccess);
            Assert.Equal("Không tìm thấy vé", result.Message);
        }

        [Fact]
        public void GetTicketInfo_Valid_ReturnsSuccess()
        {
            var invoice = new Invoice { InvoiceId = "INV5", Status = InvoiceStatus.Completed, Seat = "A2", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 2" }, ShowDate = DateOnly.FromDateTime(DateTime.Today), Schedule = new Schedule { ScheduleTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)) } }, TotalMoney = 200000, AccountId = "acc2" };
            _mockInvoiceService.Setup(s => s.GetById("INV5")).Returns(invoice);
            _mockMemberRepo.Setup(r => r.GetByAccountId("acc2")).Returns(new Member { Account = new Account { FullName = "User 2", PhoneNumber = "0987654321" } });
            var result = _service.GetTicketInfo("INV5");
            Assert.True(result.IsSuccess);
            Assert.Equal("Lấy thông tin vé thành công", result.Message);
            Assert.Equal("INV5", result.InvoiceId);
            Assert.Equal("Test Movie 2", result.MovieName);
        }

        [Fact]
        public void ConfirmCheckIn_AlreadyCheckedIn_ReturnsFail()
        {
            var invoice = new Invoice { InvoiceId = "INV6", Status = InvoiceStatus.Completed };
            _mockInvoiceService.Setup(s => s.GetById("INV6")).Returns(invoice);
            _mockScheduleSeatRepo.Setup(r => r.GetByInvoiceId("INV6")).Returns(new List<ScheduleSeat> { new ScheduleSeat { SeatStatusId = 2 } });
            var result = _service.ConfirmCheckIn("INV6", "staff1");
            Assert.False(result.IsSuccess);
            Assert.Equal("Vé đã được check-in", result.Message);
        }

        [Fact]
        public void ConfirmCheckIn_Valid_ReturnsSuccess()
        {
            var invoice = new Invoice { InvoiceId = "INV7", Status = InvoiceStatus.Completed, Seat = "A3", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Test Movie 3" }, ShowDate = DateOnly.FromDateTime(DateTime.Today), Schedule = new Schedule { ScheduleTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)) } }, TotalMoney = 300000, AccountId = "acc3" };
            _mockInvoiceService.Setup(s => s.GetById("INV7")).Returns(invoice);
            _mockScheduleSeatRepo.Setup(r => r.GetByInvoiceId("INV7")).Returns(new List<ScheduleSeat> { new ScheduleSeat { SeatStatusId = 1 } });
            _mockMemberRepo.Setup(r => r.GetByAccountId("acc3")).Returns(new Member { Account = new Account { FullName = "User 3", PhoneNumber = "0111222333" } });
            var result = _service.ConfirmCheckIn("INV7", "staff2");
            Assert.True(result.IsSuccess);
            Assert.Equal("Check-in thành công!", result.Message);
            Assert.Equal("INV7", result.InvoiceId);
            Assert.Equal("Test Movie 3", result.MovieName);
        }
    }
} 