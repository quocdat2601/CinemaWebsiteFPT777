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
                _seatHubMock.Object
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
            var accountId = "acc1";
            var invoices = new List<Invoice> { new Invoice { InvoiceId = "inv1", AccountId = accountId } };
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(invoices);
            var service = CreateService();
            var result = await service.GetUserTicketsAsync(accountId);
            Assert.NotNull(result);
            Assert.Single(result);
        }

        /// <summary>
        /// Test: Trả về null khi không tìm thấy vé (GetTicketDetailsAsync)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_ReturnsNull_WhenNotFound()
        {
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync((Invoice)null);
            var service = CreateService();
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            Assert.Null(result);
        }

        /// <summary>
        /// Test: Hủy vé thất bại khi không tìm thấy vé
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_ReturnsFalse_WhenNotFound()
        {
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync((Invoice)null);
            var service = CreateService();
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            Assert.False(success);
            Assert.Contains("Booking not found.", messages);
        }

        /// <summary>
        /// Test: Hủy vé thất bại khi vé chưa thanh toán (NotPaid)
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_NotPaid()
        {
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Status = 0 // Not Completed
            };
            _invoiceRepoMock.Setup(r => r.GetForCancelAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            Assert.False(success);
            Assert.Contains("Only paid bookings can be cancelled", string.Join(" ", messages));
        }

        /// <summary>
        /// Test: Hủy vé thành công (có điểm, có voucher, happy path)
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_Success_HasScoreAndVoucher()
        {
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
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
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
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Seat_IDs = "1,2",
                PromotionDiscount = 10
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            _seatRepoMock.Setup(r => r.GetById(1)).Returns(new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } });
            _seatRepoMock.Setup(r => r.GetById(2)).Returns(new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } });
            var service = CreateService();
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            // Dùng reflection để lấy property SeatDetails từ anonymous object
            var seatDetailsProp = result.GetType().GetProperty("SeatDetails");
            var seatDetails = seatDetailsProp.GetValue(result) as IEnumerable<MovieTheater.ViewModels.SeatDetailViewModel>;
            Assert.NotNull(seatDetails);
            Assert.Equal(2, seatDetails.Count());
        }

        /// <summary>
        /// Test: Lấy chi tiết vé với ScheduleSeats (có 2 ghế)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_WithScheduleSeats()
        {
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                ScheduleSeats = new List<ScheduleSeat>
                {
                    new ScheduleSeat { Seat = new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } } },
                    new ScheduleSeat { Seat = new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } } }
                },
                PromotionDiscount = 10
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            var seatDetailsProp = result.GetType().GetProperty("SeatDetails");
            var seatDetails = seatDetailsProp.GetValue(result) as IEnumerable<MovieTheater.ViewModels.SeatDetailViewModel>;
            Assert.NotNull(seatDetails);
            Assert.Equal(2, seatDetails.Count());
        }

        /// <summary>
        /// Test: Lấy chi tiết vé với Seat (dùng GetByName)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_WithSeatNames()
        {
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1",
                Seat = "A1, A2",
                PromotionDiscount = 10
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            _seatRepoMock.Setup(r => r.GetByName("A1")).Returns(new Seat { SeatId = 1, SeatName = "A1", SeatType = new SeatType { TypeName = "VIP", PricePercent = 100 } });
            _seatRepoMock.Setup(r => r.GetByName("A2")).Returns(new Seat { SeatId = 2, SeatName = "A2", SeatType = new SeatType { TypeName = "Normal", PricePercent = 80 } });
            var service = CreateService();
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            var seatDetailsProp = result.GetType().GetProperty("SeatDetails");
            var seatDetails = seatDetailsProp.GetValue(result) as IEnumerable<MovieTheater.ViewModels.SeatDetailViewModel>;
            Assert.NotNull(seatDetails);
            Assert.Equal(2, seatDetails.Count());
        }

        /// <summary>
        /// Test: Lấy chi tiết vé khi không có Seat_IDs, ScheduleSeats, Seat (trả về seatDetails rỗng)
        /// </summary>
        [Fact]
        public async Task GetTicketDetailsAsync_EmptySeats()
        {
            var booking = new Invoice
            {
                InvoiceId = "inv1",
                AccountId = "acc1"
                // Không có Seat_IDs, ScheduleSeats, Seat
            };
            _invoiceRepoMock.Setup(r => r.GetDetailsAsync("inv1", "acc1")).ReturnsAsync(booking);
            var service = CreateService();
            var result = await service.GetTicketDetailsAsync("inv1", "acc1");
            var seatDetailsProp = result.GetType().GetProperty("SeatDetails");
            var seatDetails = seatDetailsProp.GetValue(result) as IEnumerable<MovieTheater.ViewModels.SeatDetailViewModel>;
            Assert.NotNull(seatDetails);
            Assert.Empty(seatDetails);
        }

        /// <summary>
        /// Test: Hủy vé thành công và có thông báo nâng hạng
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_Success_WithRankUpMessage()
        {
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
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
            Assert.True(success);
            Assert.Contains("Chúc mừng bạn đã lên hạng!", string.Join(" ", messages));
        }

        /// <summary>
        /// Test: Hủy vé thành công không có voucher, không có điểm, không có refund voucher
        /// </summary>
        [Fact]
        public async Task CancelTicketAsync_Success_NoVoucherNoScoreNoRefund()
        {
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
            var (success, messages) = await service.CancelTicketAsync("inv1", "acc1");
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
            var (success, messages) = await service.CancelTicketByAdminAsync("inv1");
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
            var accountId = "acc1";
            var invoices = new List<Invoice> {
                new Invoice { InvoiceId = "inv1", AccountId = accountId, BookingDate = new System.DateTime(2024,1,1), Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "inv2", AccountId = accountId, BookingDate = new System.DateTime(2024,1,2), Status = InvoiceStatus.Incomplete }
            };
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(invoices);
            var service = CreateService();
            var result = await service.GetHistoryPartialAsync(accountId, null, null, "booked");
            Assert.Single(result);
        }

        /// <summary>
        /// Test: Lọc lịch sử vé theo fromDate, toDate
        /// </summary>
        [Fact]
        public async Task GetHistoryPartialAsync_FilterByDate()
        {
            var accountId = "acc1";
            var invoices = new List<Invoice> {
                new Invoice { InvoiceId = "inv1", AccountId = accountId, BookingDate = new System.DateTime(2024,1,1), Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "inv2", AccountId = accountId, BookingDate = new System.DateTime(2024,2,1), Status = InvoiceStatus.Completed }
            };
            _invoiceRepoMock.Setup(r => r.GetByAccountIdAsync(accountId, null)).ReturnsAsync(invoices);
            var service = CreateService();
            var fromDate = new System.DateTime(2024, 2, 1);
            var toDate = new System.DateTime(2024, 2, 1);
            var result = await service.GetHistoryPartialAsync(accountId, fromDate, toDate, "all");
            Assert.Single(result);
        }
    }
}
