using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace MovieTheater.Tests.Service
{
    public class ForgetPasswordTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
        private readonly Mock<IMemberRepository> _memberRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<EmailService> _emailServiceMock;
        private readonly Mock<ILogger<AccountService>> _loggerMock;
        private readonly Mock<MovieTheaterContext> _contextMock;
        private readonly AccountService _accountService;

        public ForgetPasswordTests()
        {
            _accountRepositoryMock = new Mock<IAccountRepository>();
            _employeeRepositoryMock = new Mock<IEmployeeRepository>();
            _memberRepositoryMock = new Mock<IMemberRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _emailServiceMock = new Mock<EmailService>();
            _loggerMock = new Mock<ILogger<AccountService>>();
            _contextMock = new Mock<MovieTheaterContext>();

            _accountService = new AccountService(
                _accountRepositoryMock.Object,
                _employeeRepositoryMock.Object,
                _memberRepositoryMock.Object,
                _httpContextAccessorMock.Object,
                _emailServiceMock.Object,
                _loggerMock.Object,
                _contextMock.Object
            );
        }

        [Fact]
        public void SendForgetPasswordOtp_WithValidEmail_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var account = new Account
            {
                AccountId = "1",
                Email = email,
                FullName = "Test User",
                Status = 1
            };

            _accountRepositoryMock.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _emailServiceMock.Setup(e => e.SendEmail(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            // Act
            var result = _accountService.SendForgetPasswordOtp(email);

            // Assert
            Assert.True(result);
            _accountRepositoryMock.Verify(r => r.GetAccountByEmail(email), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmail(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void SendForgetPasswordOtp_WithInvalidEmail_ShouldReturnFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _accountRepositoryMock.Setup(r => r.GetAccountByEmail(email)).Returns((Account)null);

            // Act
            var result = _accountService.SendForgetPasswordOtp(email);

            // Assert
            Assert.False(result);
            _accountRepositoryMock.Verify(r => r.GetAccountByEmail(email), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void VerifyForgetPasswordOtp_WithValidOtp_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var otp = "123456";

            // First send OTP to store it
            var account = new Account { AccountId = "1", Email = email, FullName = "Test User", Status = 1 };
            _accountRepositoryMock.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _emailServiceMock.Setup(e => e.SendEmail(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            _accountService.SendForgetPasswordOtp(email);

            // Act
            var result = _accountService.VerifyForgetPasswordOtp(email, otp);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyForgetPasswordOtp_WithInvalidOtp_ShouldReturnFalse()
        {
            // Arrange
            var email = "test@example.com";
            var invalidOtp = "999999";

            // Act
            var result = _accountService.VerifyForgetPasswordOtp(email, invalidOtp);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ResetPassword_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var newPassword = "NewPassword123!";
            var account = new Account { AccountId = "1", Email = email, FullName = "Test User", Status = 1 };

            _accountRepositoryMock.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _accountRepositoryMock.Setup(r => r.Update(It.IsAny<Account>())).Verifiable();
            _accountRepositoryMock.Setup(r => r.Save()).Verifiable();

            // Act
            var result = _accountService.ResetPassword(email, newPassword);

            // Assert
            Assert.True(result);
            _accountRepositoryMock.Verify(r => r.Update(It.IsAny<Account>()), Times.Once);
            _accountRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void ResetPassword_WithInvalidEmail_ShouldReturnFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var newPassword = "NewPassword123!";

            _accountRepositoryMock.Setup(r => r.GetAccountByEmail(email)).Returns((Account)null);

            // Act
            var result = _accountService.ResetPassword(email, newPassword);

            // Assert
            Assert.False(result);
            _accountRepositoryMock.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
            _accountRepositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Fact]
        public void SendForgetPasswordOtpEmail_WithValidEmail_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var otp = "123456";
            var account = new Account { AccountId = "1", Email = email, FullName = "Test User", Status = 1 };

            _accountRepositoryMock.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _emailServiceMock.Setup(e => e.SendEmail(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            // Act
            var result = _accountService.SendForgetPasswordOtpEmail(email, otp);

            // Assert
            Assert.True(result);
            _emailServiceMock.Verify(e => e.SendEmail(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void StoreForgetPasswordOtp_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var otp = "123456";
            var expiry = DateTime.UtcNow.AddMinutes(10);

            // Act
            var result = _accountService.StoreForgetPasswordOtp(email, otp, expiry);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ClearForgetPasswordOtp_ShouldNotThrowException()
        {
            // Arrange
            var email = "test@example.com";

            // Act & Assert
            var exception = Record.Exception(() => _accountService.ClearForgetPasswordOtp(email));
            Assert.Null(exception);
        }
    }
}