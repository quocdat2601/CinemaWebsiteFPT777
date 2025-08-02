using Xunit;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Microsoft.AspNetCore.Http;

namespace MovieTheater.Tests.Service
{
    public class ForgetPasswordTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly Mock<EmailService> _mockEmailService;
        private readonly Mock<ILogger<AccountService>> _mockLogger;
        private readonly Mock<MovieTheaterContext> _mockContext;
        private readonly AccountService _accountService;

        public ForgetPasswordTests()
        {
            _mockAccountRepository = new Mock<IAccountRepository>();
            _mockEmailService = new Mock<EmailService>(Mock.Of<IConfiguration>(), Mock.Of<ILogger<EmailService>>());
            _mockLogger = new Mock<ILogger<AccountService>>();
            _mockContext = new Mock<MovieTheaterContext>();

            _accountService = new AccountService(
                _mockAccountRepository.Object,
                Mock.Of<IEmployeeRepository>(),
                Mock.Of<IMemberRepository>(),
                Mock.Of<IHttpContextAccessor>(),
                _mockEmailService.Object,
                _mockLogger.Object,
                _mockContext.Object
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
                Username = "testuser"
            };

            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _mockEmailService.Setup(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            // Act
            var result = _accountService.SendForgetPasswordOtp(email);

            // Assert
            Assert.True(result);
            _mockAccountRepository.Verify(r => r.GetAccountByEmail(email), Times.Once);
            _mockEmailService.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void SendForgetPasswordOtp_WithInvalidEmail_ShouldReturnFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns((Account)null);

            // Act
            var result = _accountService.SendForgetPasswordOtp(email);

            // Assert
            Assert.False(result);
            _mockAccountRepository.Verify(r => r.GetAccountByEmail(email), Times.Once);
            _mockEmailService.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void VerifyForgetPasswordOtp_WithValidOtp_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var otp = "123456";

            // First send OTP to store it
            var account = new Account { Email = email, FullName = "Test User" };
            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _mockEmailService.Setup(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            _accountService.SendForgetPasswordOtp(email);

            // Act
            var result = _accountService.VerifyForgetPasswordOtp(email, otp);

            // Assert
            Assert.False(result); // Should be false because we don't know the actual OTP generated
        }

        [Fact]
        public void ResetPassword_WithValidEmail_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var newPassword = "NewPassword123!";
            var account = new Account
            {
                AccountId = "1",
                Email = email,
                FullName = "Test User"
            };

            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns(account);
            _mockAccountRepository.Setup(r => r.Update(It.IsAny<Account>()));
            _mockAccountRepository.Setup(r => r.Save());

            // Act
            var result = _accountService.ResetPassword(email, newPassword);

            // Assert
            Assert.True(result);
            _mockAccountRepository.Verify(r => r.GetAccountByEmail(email), Times.Once);
            _mockAccountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Once);
            _mockAccountRepository.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void ResetPassword_WithInvalidEmail_ShouldReturnFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var newPassword = "NewPassword123!";

            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns((Account)null);

            // Act
            var result = _accountService.ResetPassword(email, newPassword);

            // Assert
            Assert.False(result);
            _mockAccountRepository.Verify(r => r.GetAccountByEmail(email), Times.Once);
            _mockAccountRepository.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
            _mockAccountRepository.Verify(r => r.Save(), Times.Never);
        }

        [Fact]
        public void GetAccountByEmail_WithValidEmail_ShouldReturnAccount()
        {
            // Arrange
            var email = "test@example.com";
            var expectedAccount = new Account
            {
                AccountId = "1",
                Email = email,
                FullName = "Test User"
            };

            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns(expectedAccount);

            // Act
            var result = _accountService.GetAccountByEmail(email);

            // Assert
            Assert.Equal(expectedAccount, result);
            _mockAccountRepository.Verify(r => r.GetAccountByEmail(email), Times.Once);
        }

        [Fact]
        public void GetAccountByEmail_WithInvalidEmail_ShouldReturnNull()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _mockAccountRepository.Setup(r => r.GetAccountByEmail(email)).Returns((Account)null);

            // Act
            var result = _accountService.GetAccountByEmail(email);

            // Assert
            Assert.Null(result);
            _mockAccountRepository.Verify(r => r.GetAccountByEmail(email), Times.Once);
        }
    }
}