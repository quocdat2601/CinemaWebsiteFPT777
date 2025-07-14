using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Tests
{
    public class AccountServiceTests
    {
        [Fact]
        public void Authenticate_ReturnsTrue_WhenPasswordIsCorrect()
        {
            // Arrange
            var mockRepo = new Mock<IAccountRepository>();
            var testUsername = "testuser";
            var testPassword = "password123";
            var hasher = new PasswordHasher<Account>();
            var hashedPassword = hasher.HashPassword(null, testPassword);

            var account = new Account { Username = testUsername, Password = hashedPassword };
            mockRepo.Setup(r => r.Authenticate(testUsername)).Returns(account);

            var service = new AccountService(
                mockRepo.Object, null, null, null, null, null, null);

            // Act
            var result = service.Authenticate(testUsername, testPassword, out var returnedAccount);

            // Assert
            Assert.True(result);
            Assert.NotNull(returnedAccount);
            Assert.Equal(testUsername, returnedAccount.Username);
        }

        [Fact]
        public void Authenticate_ReturnsFalse_WhenAccountNotFound()
        {
            // Arrange
            var mockRepo = new Mock<IAccountRepository>();
            var testUsername = "nonexistent";
            var testPassword = "password123";

            mockRepo.Setup(r => r.Authenticate(testUsername)).Returns((Account)null);

            var service = new AccountService(
                mockRepo.Object, null, null, null, null, null, null);

            // Act
            var result = service.Authenticate(testUsername, testPassword, out var returnedAccount);

            // Assert
            Assert.False(result);
            Assert.Null(returnedAccount);
        }
    }
}
