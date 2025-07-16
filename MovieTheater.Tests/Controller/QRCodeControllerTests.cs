using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System;

namespace MovieTheater.Tests.Controller
{
    public class QRCodeControllerTests
    {
        private QRCodeController CreateController(
            IInvoiceService invoiceService = null,
            IScheduleSeatRepository scheduleSeatRepository = null,
            IMemberRepository memberRepository = null,
            ISeatService seatService = null,
            ISeatTypeService seatTypeService = null,
            ILogger<QRCodeController> logger = null,
            ITicketVerificationService ticketVerificationService = null)
        {
            return new QRCodeController(
                invoiceService ?? Mock.Of<IInvoiceService>(),
                scheduleSeatRepository ?? Mock.Of<IScheduleSeatRepository>(),
                memberRepository ?? Mock.Of<IMemberRepository>(),
                seatService ?? Mock.Of<ISeatService>(),
                seatTypeService ?? Mock.Of<ISeatTypeService>(),
                logger ?? Mock.Of<ILogger<QRCodeController>>(),
                ticketVerificationService ?? Mock.Of<ITicketVerificationService>()
            );
        }

        [Fact]
        public void Scanner_ReturnsView()
        {
            var ctrl = CreateController();
            var result = ctrl.Scanner();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void VerifyTicket_Success_ReturnsJsonSuccess()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.VerifyTicket("INV1")).Returns(new TicketVerificationResultViewModel { IsSuccess = true, Message = "OK" });
            var ctrl = CreateController(ticketVerificationService: service.Object);

            var result = ctrl.VerifyTicket(new VerifyTicketRequest { InvoiceId = "INV1" }) as JsonResult;
            Assert.True((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void VerifyTicket_Fail_ReturnsJsonFail()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.VerifyTicket("INV1")).Returns(new TicketVerificationResultViewModel { IsSuccess = false, Message = "ERR" });
            var ctrl = CreateController(ticketVerificationService: service.Object);

            var result = ctrl.VerifyTicket(new VerifyTicketRequest { InvoiceId = "INV1" }) as JsonResult;
            Assert.False((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void VerificationResult_InvoiceNotFound_ReturnsNotFound()
        {
            var invoiceService = new Mock<IInvoiceService>();
            invoiceService.Setup(s => s.GetById("INV1")).Returns((Invoice)null);
            var ctrl = CreateController(invoiceService: invoiceService.Object);

            var result = ctrl.VerificationResult("INV1", "success");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void VerificationResult_InvoiceFound_ReturnsView()
        {
            var invoice = new Invoice
            {
                InvoiceId = "INV1",
                AccountId = "A1",
                Seat = "A1,A2",
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test" },
                    ShowDate = DateOnly.FromDateTime(System.DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) }
                },
                TotalMoney = 100000
            };
            var invoiceService = new Mock<IInvoiceService>();
            invoiceService.Setup(s => s.GetById("INV1")).Returns(invoice);

            var memberRepo = new Mock<IMemberRepository>();
            memberRepo.Setup(m => m.GetByAccountId("A1")).Returns(new Member
            {
                Account = new Account { FullName = "Test User", PhoneNumber = "123" }
            });

            var ctrl = CreateController(invoiceService: invoiceService.Object, memberRepository: memberRepo.Object);

            var result = ctrl.VerificationResult("INV1", "success") as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<TicketVerificationResultViewModel>(result.Model);
        }

        [Fact]
        public void GetTicketInfo_Success_ReturnsJsonSuccess()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.GetTicketInfo("INV1")).Returns(new TicketVerificationResultViewModel { IsSuccess = true });
            var ctrl = CreateController(ticketVerificationService: service.Object);

            var result = ctrl.GetTicketInfo("INV1") as JsonResult;
            Assert.True((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void GetTicketInfo_Fail_ReturnsJsonFail()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.GetTicketInfo("INV1")).Returns(new TicketVerificationResultViewModel { IsSuccess = false, Message = "ERR" });
            var ctrl = CreateController(ticketVerificationService: service.Object);

            var result = ctrl.GetTicketInfo("INV1") as JsonResult;
            Assert.False((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void ConfirmCheckIn_Success_ReturnsJsonSuccess()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.ConfirmCheckIn("INV1", It.IsAny<string>())).Returns(new TicketVerificationResultViewModel { IsSuccess = true, Message = "OK" });
            var ctrl = CreateController(ticketVerificationService: service.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "staff1") }));
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var result = ctrl.ConfirmCheckIn(new QRCodeController.ConfirmCheckInRequest { InvoiceId = "INV1" }) as JsonResult;
            Assert.True((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void ConfirmCheckIn_Fail_ReturnsJsonFail()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.ConfirmCheckIn("INV1", It.IsAny<string>())).Returns(new TicketVerificationResultViewModel { IsSuccess = false, Message = "ERR" });
            var ctrl = CreateController(ticketVerificationService: service.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "staff1") }));
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var result = ctrl.ConfirmCheckIn(new QRCodeController.ConfirmCheckInRequest { InvoiceId = "INV1" }) as JsonResult;
            Assert.False((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void VerificationResult_MemberNull_AccountNull_SeatNull_TotalMoneyNull()
        {
            // Arrange
            var invoice = new Invoice
            {
                InvoiceId = "INV2",
                AccountId = "A2",
                Seat = null,
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test" },
                    ShowDate = DateOnly.FromDateTime(System.DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) }
                },
                TotalMoney = null
            };
            var invoiceService = new Mock<IInvoiceService>();
            invoiceService.Setup(s => s.GetById("INV2")).Returns(invoice);

            var memberRepo = new Mock<IMemberRepository>();
            // member null
            memberRepo.Setup(m => m.GetByAccountId("A2")).Returns((Member)null);

            var ctrl = CreateController(invoiceService: invoiceService.Object, memberRepository: memberRepo.Object);

            var result = ctrl.VerificationResult("INV2", "fail") as ViewResult;
            Assert.NotNull(result);
            var model = Assert.IsType<TicketVerificationResultViewModel>(result.Model);
            Assert.Equal("N/A", model.CustomerName);
            Assert.Equal("N/A", model.CustomerPhone);
            Assert.Equal("", model.Seats);
            Assert.Equal(" VND", model.TotalAmount);
            Assert.False(model.IsSuccess);
        }

        [Fact]
        public void VerificationResult_MemberNotNull_AccountNull()
        {
            // Arrange
            var invoice = new Invoice
            {
                InvoiceId = "INV3",
                AccountId = "A3",
                Seat = "A1",
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test" },
                    ShowDate = DateOnly.FromDateTime(System.DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) }
                },
                TotalMoney = 100000
            };
            var invoiceService = new Mock<IInvoiceService>();
            invoiceService.Setup(s => s.GetById("INV3")).Returns(invoice);

            var memberRepo = new Mock<IMemberRepository>();
            // member.Account null
            memberRepo.Setup(m => m.GetByAccountId("A3")).Returns(new Member { Account = null });

            var ctrl = CreateController(invoiceService: invoiceService.Object, memberRepository: memberRepo.Object);

            var result = ctrl.VerificationResult("INV3", "success") as ViewResult;
            Assert.NotNull(result);
            var model = Assert.IsType<TicketVerificationResultViewModel>(result.Model);
            Assert.Equal("N/A", model.CustomerName);
            Assert.Equal("N/A", model.CustomerPhone);
            Assert.Equal("A1", model.Seats);
            Assert.Equal("100,000 VND", model.TotalAmount);
            Assert.True(model.IsSuccess);
        }

        [Fact]
        public void ConfirmCheckIn_NoNameIdentifier_StaffIdUnknown()
        {
            var service = new Mock<ITicketVerificationService>();
            service.Setup(s => s.ConfirmCheckIn("INV1", "Unknown")).Returns(new TicketVerificationResultViewModel { IsSuccess = true, Message = "OK" });
            var ctrl = CreateController(ticketVerificationService: service.Object);
            // Không có claim NameIdentifier
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var result = ctrl.ConfirmCheckIn(new QRCodeController.ConfirmCheckInRequest { InvoiceId = "INV1" }) as JsonResult;
            Assert.True((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void CheckInLog_Property_Getters_Coverage()
        {
            var log = new QRCodeController.CheckInLog
            {
                Id = 1,
                InvoiceId = "INV",
                StaffId = "S1",
                CheckInTime = System.DateTime.Now,
                Status = "OK",
                Note = "Test"
            };
            Assert.Equal(1, log.Id);
            Assert.Equal("INV", log.InvoiceId);
            Assert.Equal("S1", log.StaffId);
            Assert.NotNull(log.CheckInTime);
            Assert.Equal("OK", log.Status);
            Assert.Equal("Test", log.Note);
        }

        [Fact]
        public void ConfirmCheckInRequest_Property_Getter_Coverage()
        {
            var req = new QRCodeController.ConfirmCheckInRequest { InvoiceId = "INV" };
            Assert.Equal("INV", req.InvoiceId);
        }

        [Fact]
        public void VerifyTicketRequest_Property_Getter_Coverage()
        {
            var req = new VerifyTicketRequest { InvoiceId = "INV" };
            Assert.Equal("INV", req.InvoiceId);
        }

        [Fact]
        public void CheckInLog_AllProperties_Coverage()
        {
            var log = new QRCodeController.CheckInLog();
            log.Id = 2;
            log.InvoiceId = "INV2";
            log.StaffId = "S2";
            log.CheckInTime = DateTime.Now;
            log.Status = "DONE";
            log.Note = "abc";
            // Truy cập từng property
            var id = log.Id;
            var invoiceId = log.InvoiceId;
            var staffId = log.StaffId;
            var checkInTime = log.CheckInTime;
            var status = log.Status;
            var note = log.Note;

            // Thêm assert kiểm tra giá trị cuối cùng
            Assert.Equal(2, id);
            Assert.Equal("INV2", invoiceId);
            Assert.Equal("S2", staffId);
            Assert.Equal("DONE", status);
            Assert.Equal("abc", note);
            Assert.True(checkInTime <= DateTime.Now && checkInTime > DateTime.Now.AddMinutes(-1));
        }
        [Fact]
        public void ConfirmCheckInRequest_AllProperties_Coverage()
        {
            var req = new QRCodeController.ConfirmCheckInRequest();
            req.InvoiceId = "INV3";
            Assert.Equal("INV3", req.InvoiceId);
        }

        [Fact]
        public void VerifyTicketRequest_AllProperties_Coverage()
        {
            var req = new VerifyTicketRequest();
            req.InvoiceId = "INV4";
            Assert.Equal("INV4", req.InvoiceId);
        }

        [Fact]
        public void All_CheckInLog_Property_Getters_Coverage()
        {
            var log = new MovieTheater.Controllers.QRCodeController.CheckInLog();
            log.Id = 10;
            log.InvoiceId = "INV10";
            log.StaffId = "S10";
            log.CheckInTime = System.DateTime.Now;
            log.Status = "DONE";
            log.Note = "abc";
            Assert.Equal(10, log.Id);
            Assert.Equal("INV10", log.InvoiceId);
            Assert.Equal("S10", log.StaffId);
            Assert.Equal("DONE", log.Status);
            Assert.Equal("abc", log.Note);
            Assert.True(log.CheckInTime <= DateTime.Now && log.CheckInTime > DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public void All_ConfirmCheckInRequest_Property_Getters_Coverage()
        {
            var req = new MovieTheater.Controllers.QRCodeController.ConfirmCheckInRequest();
            req.InvoiceId = "INV20";
            Assert.Equal("INV20", req.InvoiceId);
        }

        [Fact]
        public void All_VerifyTicketRequest_Property_Getters_Coverage()
        {
            var req = new MovieTheater.Controllers.VerifyTicketRequest();
            req.InvoiceId = "INV30";
            Assert.Equal("INV30", req.InvoiceId);
        }

        [Fact]
        public void CheckInLog_DefaultConstructor_Coverage()
        {
            var log = new MovieTheater.Controllers.QRCodeController.CheckInLog();
            Assert.NotNull(log);
        }

        [Fact]
        public void ConfirmCheckInRequest_DefaultConstructor_Coverage()
        {
            var req = new MovieTheater.Controllers.QRCodeController.ConfirmCheckInRequest();
            Assert.NotNull(req);
        }

        [Fact]
        public void VerifyTicketRequest_DefaultConstructor_Coverage()
        {
            var req = new MovieTheater.Controllers.VerifyTicketRequest();
            Assert.NotNull(req);
        }
        [Fact]
        public void CheckInLog_AllProperties_MultiValue_Coverage()
        {
            var log = new MovieTheater.Controllers.QRCodeController.CheckInLog();
            log.Id = 1; log.Id = 2;
            log.InvoiceId = "A"; log.InvoiceId = "B";
            log.StaffId = "X"; log.StaffId = "Y";
            log.CheckInTime = System.DateTime.Now;
            log.Status = "OK"; log.Status = "NO";
            log.Note = "abc"; log.Note = "def";
            Assert.Equal(2, log.Id);
            Assert.Equal("B", log.InvoiceId);
            Assert.Equal("Y", log.StaffId);
            Assert.Equal("NO", log.Status);
            Assert.Equal("def", log.Note);
            Assert.True(log.CheckInTime <= DateTime.Now && log.CheckInTime > DateTime.Now.AddMinutes(-1));
        }

        [Fact]
        public void ConfirmCheckInRequest_AllProperties_MultiValue_Coverage()
        {
            var req = new MovieTheater.Controllers.QRCodeController.ConfirmCheckInRequest();
            req.InvoiceId = "A"; req.InvoiceId = "B";
            Assert.Equal("B", req.InvoiceId);
        }

        [Fact]
        public void VerifyTicketRequest_AllProperties_MultiValue_Coverage()
        {
            var req = new MovieTheater.Controllers.VerifyTicketRequest();
            req.InvoiceId = "A"; req.InvoiceId = "B";
            Assert.Equal("B", req.InvoiceId);
        }
    }
}