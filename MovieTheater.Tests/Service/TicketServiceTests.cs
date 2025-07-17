using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace MovieTheater.Tests.Service
{
    public class TicketServiceTests
    {
        // Mock dependencies
        private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
        private readonly Mock<IAccountService> _accountServiceMock = new();
        private readonly Mock<IVoucherService> _voucherServiceMock = new();
        private readonly Mock<IScheduleSeatRepository> _scheduleSeatRepoMock = new();
        private readonly Mock<ISeatRepository> _seatRepoMock = new();
        private readonly Mock<IHubContext<MovieTheater.Hubs.DashboardHub>> _dashboardHubMock = new();
        private readonly Mock<IHubContext<MovieTheater.Hubs.SeatHub>> _seatHubMock = new();
        private readonly Mock<IFoodInvoiceService> _foodInvoiceServiceMock = new();

        // Utility: Create TicketService with all mocks
        private TicketService CreateService()
        {
            return new TicketService(
                _invoiceRepoMock.Object,
                _accountServiceMock.Object,
                _voucherServiceMock.Object,
                _dashboardHubMock.Object,
                _scheduleSeatRepoMock.Object,
                _seatRepoMock.Object,
                _seatHubMock.Object,
                _foodInvoiceServiceMock.Object
            );
        }

        // Utility: Mock SignalR DashboardHub context to avoid NullReferenceException
        private void SetupDashboardHubMock()
        {
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);
            _dashboardHubMock.Setup(x => x.Clients).Returns(mockClients.Object);
            // Không cần mock SendAsync, chỉ cần property Clients.All
        }

        /// <summary>
        /// Test: Lấy danh sách vé theo accountId (happy path)
        /// </summary>
        [Fact]
        public async Task GetUserTicketsAsync_ReturnsTickets()
        {
            // Arrange
            var accountId = "acc1";
            var invoices = new List<Invoice> { new Invoice { InvoiceId = "inv1", AccountId = accountId } };
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(invoices);
            var service = CreateService();
            // Act
            var result = await service.GetUserTicketsAsync(accountId);
            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetUserTicketsAsync_ReturnsEmptyList_WhenNoTickets()
        {
            // Arrange
            var accountId = "acc2";
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(new List<Invoice>());
            var service = CreateService();
            // Act
            var result = await service.GetUserTicketsAsync(accountId);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// Test: Trả về null khi không tìm thấy vé (GetTicketDetailsAsync)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync((Invoice)null);
            var service = CreateService();
            // Act
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test: Hủy vé thất bại khi không tìm thấy vé
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync((Invoice)null);
            var service = CreateService();
            // Act
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            // Assert
            Assert.False(success);
            Assert.Contains("Booking not found.", messages);
        }

        /// <summary>
        /// Test: Hủy vé thất bại khi vé chưa thanh toán (NotPaid)
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_NotPaid()
        {
            // Arrange
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Status = 0 // Not Completed
            };
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            // Act
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            // Assert
            Assert.False(success);
            Assert.Contains("Only paid bookings can be cancelled", string.Join(" ", messages));
        }

        /// <summary>
        /// Test: Hủy vé thành công (có điểm, có voucher, happy path)
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_Success_HasScoreAndVoucher()
        {
            // Arrange
            SetupDashboardHubMock();
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Status = InvoiceStatus.Completed,
                AddScore = 10,
                UseScore = 5,
                VoucherId = "v1",
                TotalMoney = 100000
            };
            var voucher = new Voucher { VoucherId = "v1", Code = "VCODE", IsUsed = true };
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync(booking);
            _scheduleSeatRepoMock.Setup(r => r.GetByInvoiceId("inv1")).Returns(new List<ScheduleSeat>());
            _voucherServiceMock.Setup(v => v.GetById("v1")).Returns(voucher);
            var service = CreateService();
            // Act
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            // Assert
            Assert.True(success);
            Assert.Contains("Ticket cancelled successfully.", messages);
            Assert.Contains("Refund voucher value", string.Join(" ", messages));
            Assert.Contains("restored", string.Join(" ", messages));
            _voucherServiceMock.Verify(v => v.Update(It.Is<Voucher>(x => x.VoucherId == "v1" && x.IsUsed == false)), Times.Once);
        }

        /// <summary>
        /// Test: Lấy chi tiết vé với Seat_IDs (có 2 ghế)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_WithSeatIds()
        {
            // Arrange
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                SeatIds = "1,2",
                PromotionDiscount = 10,
                MovieShow = new MovieShow { Version = new MovieTheater.Models.Version { Multi = 1 } }
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            _seatRepoMock.Setup(r => r.GetById(1)).Returns(new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } });
            _seatRepoMock.Setup(r => r.GetById(2)).Returns(new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } });
            var service = CreateService();
            // Act
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        /// <summary>
        /// Test: Lấy chi tiết vé với ScheduleSeats (có 2 ghế)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_WithScheduleSeats()
        {
            // Arrange
            var scheduleSeats = new List<ScheduleSeat>
            {
                new ScheduleSeat { Seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } } },
                new ScheduleSeat { Seat = new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } } }
            };
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                ScheduleSeats = scheduleSeats,
                PromotionDiscount = 10,
                MovieShow = new MovieShow { Version = new MovieTheater.Models.Version { Multi = 1 } }
            };
            // Đảm bảo không có phần tử null trong ScheduleSeats
            Assert.All(scheduleSeats, ss => Assert.NotNull(ss.Seat));
            Assert.All(scheduleSeats, ss => Assert.NotNull(ss.Seat.SeatType));
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            // Act
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        /// <summary>
        /// Test: Lấy chi tiết vé với Seat (dùng GetByName)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_WithSeatNames()
        {
            // Arrange
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Seat = "A1, A2",
                PromotionDiscount = 10,
                MovieShow = new MovieShow { Version = new MovieTheater.Models.Version { Multi = 1 } }
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            _seatRepoMock.Setup(r => r.GetByName("A1")).Returns(new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } });
            _seatRepoMock.Setup(r => r.GetByName("A2")).Returns(new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } });
            var service = CreateService();
            // Act
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        /// <summary>
        /// Test: Lấy chi tiết vé khi không có Seat_IDs, ScheduleSeats, Seat (trả về seatDetails rỗng)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_EmptySeats()
        {
            // Arrange
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                MovieShow = new MovieShow { Version = new MovieTheater.Models.Version { Multi = 1 } },
                ScheduleSeats = new List<ScheduleSeat>() // Đảm bảo không null
                // Không có Seat_IDs, Seat
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            // Act
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        /// <summary>
        /// Test: Hủy vé thành công và có thông báo nâng hạng
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_Success_WithRankUpMessage()
        {
            // Arrange
            SetupDashboardHubMock();
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Status = InvoiceStatus.Completed,
                TotalMoney = 100000
            };
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync(booking);
            _scheduleSeatRepoMock.Setup(r => r.GetByInvoiceId("inv1")).Returns(new List<ScheduleSeat>());
            _accountServiceMock.Setup(a => a.GetAndClearRankUpgradeNotification("acc1")).Returns("Chúc mừng bạn đã lên hạng!");
            var service = CreateService();
            // Act
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            // Assert
            Assert.True(success);
            Assert.Contains("Chúc mừng bạn đã lên hạng!", string.Join(" ", messages));
        }

        /// <summary>
        /// Test: Hủy vé thành công không có voucher, không có điểm, không có refund voucher
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_Success_NoVoucherNoScoreNoRefund()
        {
            // Arrange
            SetupDashboardHubMock();
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Status = InvoiceStatus.Completed,
                TotalMoney = 0 // Không tạo refund voucher
            };
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync(booking);
            _scheduleSeatRepoMock.Setup(r => r.GetByInvoiceId("inv1")).Returns(new List<ScheduleSeat>());
            var service = CreateService();
            // Act
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            // Assert
            Assert.True(success);
            Assert.Contains("Ticket cancelled successfully.", messages);
            Assert.DoesNotContain("Refund voucher value", string.Join(" ", messages));
        }

        /// <summary>
        /// Test: Hủy vé bởi admin thành công (happy path)
        /// </summary>
        [Fact]
        public async Task CancelTicketByAdminAsync_Success()
        {
            // Arrange
            SetupDashboardHubMock();
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Status = InvoiceStatus.Completed,
                AddScore = 10,
                UseScore = 5,
                VoucherId = "v1",
                TotalMoney = 100000
            };
            var voucher = new Voucher { VoucherId = "v1", Code = "VCODE", IsUsed = true };
            _invoiceRepoMock.Setup(r => r.GetById("inv1")).Returns(booking);
            _scheduleSeatRepoMock.Setup(r => r.GetByInvoiceId("inv1")).Returns(new List<ScheduleSeat>());
            _voucherServiceMock.Setup(v => v.GetById("v1")).Returns(voucher);
            var service = CreateService();
            // Act
            var (success, messages) = await service.CancelTicketByAdminAsync("inv1");
            // Assert
            Assert.True(success);
            Assert.Contains("Ticket cancelled successfully.", messages);
            Assert.Contains("Refund voucher value", string.Join(" ", messages));
            Assert.Contains("restored", string.Join(" ", messages));
        }

        /// <summary>
        /// Test: Lọc lịch sử vé theo trạng thái (booked)
        /// </summary>
        [Fact]
        public async Task GetHistoryPartialAsync_FilterByStatus()
        {
            // Arrange
            var accountId = "acc1";
            var invoices = new List<Invoice> {
                new Invoice { InvoiceId = "inv1", AccountId = accountId, BookingDate = new System.DateTime(2024,1,1), Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "inv2", AccountId = accountId, BookingDate = new System.DateTime(2024,1,2), Status = InvoiceStatus.Incomplete }
            };
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(invoices);
            var service = CreateService();
            // Act
            var result = await service.GetHistoryPartialAsync(accountId, null, null, "booked");
            // Assert
            Assert.Single(result);
        }

        /// <summary>
        /// Test: Lọc lịch sử vé theo fromDate, toDate
        /// </summary>
        [Fact]
        public async Task GetHistoryPartialAsync_FilterByDate()
        {
            // Arrange
            var accountId = "acc1";
            var invoices = new List<Invoice> {
                new Invoice { InvoiceId = "inv1", AccountId = accountId, BookingDate = new System.DateTime(2024,1,1), Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "inv2", AccountId = accountId, BookingDate = new System.DateTime(2024,2,1), Status = InvoiceStatus.Completed }
            };
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(invoices);
            var service = CreateService();
            var fromDate = new System.DateTime(2024, 2, 1);
            var toDate = new System.DateTime(2024, 2, 1);
            // Act
            var result = await service.GetHistoryPartialAsync(accountId, fromDate, toDate, "all");
            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTicketDetailsAsync_ReturnsInvoice_WhenFound()
        {
            // Arrange
            var booking = new Invoice { InvoiceId = "inv1", AccountId = "acc1" };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            // Act
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        [Fact]
        public void BuildSeatDetails_ReturnsSeatDetails_FromScheduleSeats()
        {
            // Arrange
            var booking = new Invoice
            {
                ScheduleSeats = new List<ScheduleSeat>
                {
                    new ScheduleSeat { Seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } } },
                    new ScheduleSeat { Seat = new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } } }
                },
                PromotionDiscount = 10,
                MovieShow = new MovieShow { Version = new MovieTheater.Models.Version { Multi = 1 } }
            };
            var service = CreateService();
            // Act
            var seatDetails = service.BuildSeatDetails(booking);
            // Assert
            Assert.Equal(2, seatDetails.Count);
            Assert.Contains(seatDetails, s => s.SeatName == "A1");
            Assert.Contains(seatDetails, s => s.SeatName == "A2");
        }
    }
}
