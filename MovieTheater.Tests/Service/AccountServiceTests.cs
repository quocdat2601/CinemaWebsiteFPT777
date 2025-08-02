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
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace MovieTheater.Tests.Service
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock = new();
        private readonly Mock<IEmployeeRepository> _employeeRepoMock = new();
        private readonly Mock<IMemberRepository> _memberRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<ILogger<AccountService>> _loggerMock = new();
        private readonly Mock<MovieTheaterContext> _contextMock = new();

        private EmailService CreateFakeEmailService()
        {
            var configMock = new Mock<IConfiguration>();
            var loggerMock = new Mock<ILogger<EmailService>>();
            return new EmailService(configMock.Object, loggerMock.Object);
        }

        private AccountService CreateService()
        {
            return new AccountService(
                _accountRepoMock.Object,
                _employeeRepoMock.Object,
                _memberRepoMock.Object,
                _httpContextAccessorMock.Object,
                CreateFakeEmailService(), // Use a real EmailService with mocked dependencies
                _loggerMock.Object,
                _contextMock.Object
            );
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(IEnumerable<T> elements) where T : class
        {
            var queryable = elements.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return dbSetMock;
        }

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

        [Fact]
        public void Register_ShouldReturnFalse_WhenUsernameExists()
        {
            // Arrange
            var service = CreateService();
            var model = new RegisterViewModel { Username = "existinguser" };
            _accountRepoMock.Setup(r => r.GetByUsername("existinguser")).Returns(new Account());
            
            // Act
            var result = service.Register(model);
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Register_ShouldAddAccount_WhenUsernameDoesNotExist()
        {
            // Arrange
            var service = CreateService();
            var model = new RegisterViewModel
            {
                Username = "newuser",
                Password = "password",
                FullName = "Test User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now),
                Gender = "M",
                IdentityCard = "123",
                Email = "test@example.com",
                Address = "123 St",
                PhoneNumber = "123456789",
                RoleId = 3
            };
            _accountRepoMock.Setup(r => r.GetByUsername("newuser")).Returns((Account)null);
            // Mock Ranks to return an empty DbSet
            var ranksDbSet = CreateMockDbSet<Rank>(new List<Rank>());
            _contextMock.Setup(c => c.Ranks).Returns(ranksDbSet.Object);
            // Act
            var result = service.Register(model);
            // Assert
            Assert.True(result);
            _accountRepoMock.Verify(r => r.Add(It.IsAny<Account>()), Times.Once);
            _accountRepoMock.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void Register_ShouldSetInitialRank_WhenRoleIsMemberAndBronzeRankExists()
        {
            // Arrange
            var service = CreateService();
            var model = new RegisterViewModel
            {
                Username = "member",
                Password = "pw",
                FullName = "Test",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now),
                Gender = "M",
                IdentityCard = "123",
                Email = "test@example.com",
                Address = "123 St",
                PhoneNumber = "123456789",
                RoleId = 3
            };
            var bronzeRank = new Rank { RankId = 1, RequiredPoints = 0, RankName = "Bronze" };
            var ranks = new List<Rank> { bronzeRank }.AsQueryable();
            var mockRankSet = new Moq.Mock<DbSet<Rank>>();
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Provider).Returns(ranks.Provider);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Expression).Returns(ranks.Expression);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.ElementType).Returns(ranks.ElementType);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.GetEnumerator()).Returns(ranks.GetEnumerator());
            _contextMock.Setup(c => c.Ranks).Returns(mockRankSet.Object);
            // Act
            var result = service.Register(model);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Register_ShouldAddEmployee_WhenRoleIsEmployee()
        {
            // Arrange
            var service = CreateService();
            var model = new RegisterViewModel
            {
                Username = "employee",
                Password = "pw",
                FullName = "Test",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now),
                Gender = "M",
                IdentityCard = "123",
                Email = "test@example.com",
                Address = "123 St",
                PhoneNumber = "123456789",
                RoleId = 2
            };
            // Act
            var result = service.Register(model);
            // Assert
            Assert.True(result);
            _employeeRepoMock.Verify(r => r.Add(It.IsAny<Employee>()), Moq.Times.Once);
            _employeeRepoMock.Verify(r => r.Save(), Moq.Times.Once);
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenAccountNotFound()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetById("id")).Returns((Account)null);
            
            // Act
            var result = service.Update("id", new RegisterViewModel());
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_ShouldThrowException_WhenDuplicateUsername()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { Username = "olduser" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            _accountRepoMock.Setup(r => r.GetByUsername("newuser")).Returns(new Account());
            var model = new RegisterViewModel { Username = "newuser" };
            
            // Act & Assert
            Assert.Throws<Exception>(() => service.Update("id", model));
        }

        [Fact]
        public void Update_ShouldUpdateAllFields_WhenAllFieldsProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "olduser", Password = "oldpass" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel
            {
                Username = "newuser",
                Password = "newpass",
                FullName = "New Name",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now),
                Gender = "F",
                IdentityCard = "999",
                Email = "new@example.com",
                Address = "New Address",
                PhoneNumber = "987654321",
                Status = 2
            };
            // Act
            var result = service.Update("id", model);
            // Assert
            Assert.True(result);
            _accountRepoMock.Verify(r => r.Update(account), Times.Once);
            _accountRepoMock.Verify(r => r.Save(), Times.Once);
            Assert.Equal("newuser", account.Username);
            Assert.Equal("newpass", account.Password);
            Assert.Equal("New Name", account.FullName);
            Assert.Equal("F", account.Gender);
            Assert.Equal("999", account.IdentityCard);
            Assert.Equal("new@example.com", account.Email);
            Assert.Equal("New Address", account.Address);
            Assert.Equal("987654321", account.PhoneNumber);
            Assert.Equal(2, account.Status);
        }

        [Fact]
        public void Update_ShouldUpdatePassword_WhenPasswordProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "oldpass" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel { Username = "user", Password = "newpass" };
            // Act
            var result = service.Update("id", model);
            // Assert
            Assert.True(result);
            Assert.Equal("newpass", account.Password);
        }

        [Fact]
        public void Update_ShouldUpdateImage_WhenImageFileProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "pass" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.FileName).Returns("avatar.png");
            fileMock.Setup(f => f.CopyTo(It.IsAny<Stream>())).Callback<Stream>(s => { });
            var model = new RegisterViewModel { Username = "user", ImageFile = fileMock.Object };
            var testDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
            Directory.CreateDirectory(testDir);
            try
            {
                // Act
                var result = service.Update("id", model);
                // Assert
                Assert.True(result);
                Assert.Contains("/images/avatars/", account.Image);
            }
            finally
            {
                // Clean up test directory if empty
                if (Directory.Exists(testDir) && Directory.GetFiles(testDir).Length == 0)
                {
                    Directory.Delete(testDir, false);
                }
            }
        }

        [Fact]
        public void Update_ShouldUpdateImage_WhenImageProvidedAndNoImageFile()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "pw" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel { Username = "user", Image = "/images/new.png", ImageFile = null };
            // Act
            service.Update("id", model);
            // Assert
            Assert.Equal("/images/new.png", account.Image);
        }

        [Fact]
        public void Update_ShouldUpdateStatus_WhenStatusProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "pass" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel { Username = "user", Status = 5 };
            // Act
            var result = service.Update("id", model);
            // Assert
            Assert.True(result);
            Assert.Equal(5, account.Status);
        }

        [Fact]
        public void Update_ShouldNotUpdatePassword_WhenPasswordNotProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "oldpass" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel { Username = "user" };
            // Act
            var result = service.Update("id", model);
            // Assert
            Assert.True(result);
            Assert.Equal("oldpass", account.Password);
        }

        [Fact]
        public void Update_ShouldNotUpdateImage_WhenImageFileNotProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "pass", Image = "oldimg.png" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel { Username = "user" };
            // Act
            var result = service.Update("id", model);
            // Assert
            Assert.True(result);
            Assert.Equal("oldimg.png", account.Image);
        }

        [Fact]
        public void Update_ShouldNotUpdateStatus_WhenStatusNotProvided()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Username = "user", Password = "pass", Status = 1 };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            var model = new RegisterViewModel { Username = "user" };
            // Act
            var result = service.Update("id", model);
            // Assert
            Assert.True(result);
            Assert.Equal(1, account.Status);
        }

        [Fact]
        public void Authenticate_ShouldReturnFalse_WhenAccountIsNull()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.Authenticate("user")).Returns((Account)null);
            
            // Act
            var result = service.Authenticate("user", "pass", out var account);
            
            // Assert
            Assert.False(result);
            Assert.Null(account);
        }

        [Fact]
        public void Authenticate_ShouldThrowArgumentNullException_WhenPasswordIsNull()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { Password = null };
            _accountRepoMock.Setup(r => r.Authenticate("user")).Returns(account);
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.Authenticate("user", "pass", out var _));
        }

        [Fact]
        public void Authenticate_ShouldReturnFalse_WhenPasswordVerificationFails()
        {
            // Arrange
            var service = CreateService();
            var hasher = new PasswordHasher<Account>();
            var account = new Account { Password = hasher.HashPassword(null, "correctpw") };
            _accountRepoMock.Setup(r => r.Authenticate("user")).Returns(account);

            // Act
            var result = service.Authenticate("user", "wrongpw", out var returnedAccount);

            // Assert
            Assert.False(result);
            Assert.Equal(account, returnedAccount);
        }

        [Fact]
        public void Authenticate_ShouldReturnTrue_WhenPasswordVerificationSucceeds()
        {
            // Arrange
            var service = CreateService();
            var hasher = new PasswordHasher<Account>();
            var account = new Account { Password = hasher.HashPassword(null, "correctpw") };
            _accountRepoMock.Setup(r => r.Authenticate("user")).Returns(account);

            // Act
            var result = service.Authenticate("user", "correctpw", out var returnedAccount);

            // Assert
            Assert.True(result);
            Assert.Equal(account, returnedAccount);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnNull_WhenUserIsNull()
        {
            // Arrange
            var service = CreateService();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext)null);
            // Act
            var result = service.GetCurrentUser();
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnNull_WhenUserIdIsNull()
        {
            // Arrange
            var service = CreateService();
            var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirst(It.IsAny<string>())).Returns((System.Security.Claims.Claim)null);
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.User).Returns(userMock.Object);
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);
            // Act
            var result = service.GetCurrentUser();
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnNull_WhenAccountIsNull()
        {
            // Arrange
            var service = CreateService();
            var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirst(It.IsAny<string>())).Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "id"));
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.User).Returns(userMock.Object);
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);
            _accountRepoMock.Setup(r => r.GetById("id")).Returns((Account)null);
            // Act
            var result = service.GetCurrentUser();
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_ShouldReturnProfile_WhenAccountExists()
        {
            // Arrange
            var service = CreateService();
            var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirst(It.IsAny<string>())).Returns(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "id"));
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.User).Returns(userMock.Object);
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);
            var account = new Account
            {
                AccountId = "id",
                Username = "user",
                FullName = "Full Name",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now),
                Gender = "M",
                IdentityCard = "123",
                Email = "a@b.com",
                Address = "Addr",
                PhoneNumber = "123456789",
                Password = "pw",
                Image = "img.png",
                Members = new List<Member> { new Member { AccountId = "id", Score = 42 } }
            };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            // Act
            var result = service.GetCurrentUser();
            // Assert
            Assert.NotNull(result);
            Assert.Equal("id", result.AccountId);
            Assert.Equal("user", result.Username);
            Assert.Equal("Full Name", result.FullName);
            Assert.Equal("M", result.Gender);
            Assert.Equal("123", result.IdentityCard);
            Assert.Equal("a@b.com", result.Email);
            Assert.Equal("Addr", result.Address);
            Assert.Equal("123456789", result.PhoneNumber);
            Assert.Equal("pw", result.Password);
            Assert.Equal("img.png", result.Image);
            Assert.Equal(42, result.Score);
        }

        [Fact]
        public void VerifyCurrentPassword_ShouldReturnFalse_WhenAccountNotFound()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns((Account)null);
            // Act
            var result = service.VerifyCurrentPassword("user", "pw");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyCurrentPassword_ShouldHashPassword_WhenNotHashed()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { Password = "shortpw" };
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns(account);
            // Act
            service.VerifyCurrentPassword("user", "shortpw");
            // Assert
            _accountRepoMock.Verify(r => r.Update(account), Times.Once);
            _accountRepoMock.Verify(r => r.Save(), Times.Once);
            Assert.True(account.Password.Length >= 20); // Hashed password is longer
        }

        [Fact]
        public void VerifyCurrentPassword_ShouldReturnTrue_WhenPasswordCorrect()
        {
            // Arrange
            var service = CreateService();
            var hasher = new PasswordHasher<Account>();
            var account = new Account { Password = hasher.HashPassword(null, "pw") };
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns(account);
            // Act
            var result = service.VerifyCurrentPassword("user", "pw");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyCurrentPassword_ShouldReturnFalse_WhenPasswordIncorrect()
        {
            // Arrange
            var service = CreateService();
            var hasher = new PasswordHasher<Account>();
            var account = new Account { Password = hasher.HashPassword(null, "pw") };
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns(account);
            // Act
            var result = service.VerifyCurrentPassword("user", "wrong");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyCurrentPassword_ShouldReturnFalse_WhenExceptionThrown()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { Password = "pw" };
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns(account);
            // Simulate exception by making password null after hashing
            var loggerMock = new Mock<ILogger<AccountService>>();
            // Act
            var result = service.VerifyCurrentPassword("user", null);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdatePassword_ShouldReturnTrue_WhenAccountFound()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Password = "old" };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            // Act
            var result = service.UpdatePassword("id", "new");
            // Assert
            Assert.True(result);
            _accountRepoMock.Verify(r => r.Update(account), Moq.Times.Once);
            _accountRepoMock.Verify(r => r.Save(), Moq.Times.Once);
        }

        [Fact]
        public void UpdatePassword_ShouldReturnFalse_WhenAccountNotFound()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetById("id")).Returns((Account)null);
            // Act
            var result = service.UpdatePassword("id", "new");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdatePasswordByUsername_ShouldReturnTrue_WhenAccountFound()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { Username = "user", Password = "old" };
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns(account);
            // Act
            var result = service.UpdatePasswordByUsername("user", "new");
            // Assert
            Assert.True(result);
            _accountRepoMock.Verify(r => r.Update(account), Moq.Times.Once);
            _accountRepoMock.Verify(r => r.Save(), Moq.Times.Once);
        }

        [Fact]
        public void UpdatePasswordByUsername_ShouldReturnFalse_WhenAccountNotFound()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns((Account)null);
            // Act
            var result = service.UpdatePasswordByUsername("user", "new");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void StoreOtp_ShouldReturnTrue_WhenNoException()
        {
            // Arrange
            var service = CreateService();
            // Act
            var result = service.StoreOtp("id", "otp", DateTime.UtcNow.AddMinutes(5));
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void StoreOtp_ShouldReturnFalse_WhenExceptionThrown()
        {
            // Arrange
            var service = CreateService();
            // Simulate exception by using a key that throws
            var result = false;
            try
            {
                result = service.StoreOtp(null, "otp", DateTime.UtcNow.AddMinutes(5));
            }
            catch { }
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyOtp_ShouldReturnTrue_WhenOtpMatchesAndNotExpired()
        {
            // Arrange
            var service = CreateService();
            var accountId = "id";
            var otp = "123";
            service.StoreOtp(accountId, otp, DateTime.UtcNow.AddMinutes(5));
            // Act
            var result = service.VerifyOtp(accountId, otp);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyOtp_ShouldReturnFalse_WhenOtpDoesNotMatch()
        {
            // Arrange
            var service = CreateService();
            var accountId = "id";
            service.StoreOtp(accountId, "123", DateTime.UtcNow.AddMinutes(5));
            // Act
            var result = service.VerifyOtp(accountId, "456");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyOtp_ShouldReturnFalse_WhenOtpExpired()
        {
            // Arrange
            var service = CreateService();
            var accountId = "id";
            var otp = "123";
            service.StoreOtp(accountId, otp, DateTime.UtcNow.AddMinutes(-1));
            // Act
            var result = service.VerifyOtp(accountId, otp);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ClearOtp_ShouldRemoveOtp()
        {
            // Arrange
            var service = CreateService();
            var accountId = "id";
            service.StoreOtp(accountId, "otp", DateTime.UtcNow.AddMinutes(5));
            // Act
            service.ClearOtp(accountId);
            var result = service.VerifyOtp(accountId, "otp");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAndClearRankUpgradeNotification_ShouldReturnMessage_WhenPresent()
        {
            // Arrange
            var service = CreateService();
            var accountId = "id";
            var field = typeof(AccountService).GetField("_pendingRankNotifications", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, string>)field.GetValue(null);
            dict[accountId] = "msg";
            // Act
            var result = service.GetAndClearRankUpgradeNotification(accountId);
            // Assert
            Assert.Equal("msg", result);
        }

        [Fact]
        public void GetAndClearRankUpgradeNotification_ShouldReturnNull_WhenAbsent()
        {
            // Arrange
            var service = CreateService();
            // Act
            var result = service.GetAndClearRankUpgradeNotification("notfound");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddScoreAsync_ShouldAddPoints_WhenMemberFound()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Members = new List<Member>() };
            var member = new Member { Score = 10, TotalPoints = 20, AccountId = "id", Account = account };
            account.Members.Add(member);
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            _memberRepoMock.Setup(r => r.Update(member));
            _memberRepoMock.Setup(r => r.Save());
            // Mock DbSet<Member> for context
            var members = new List<Member> { member }.AsQueryable();
            var mockSet = new Moq.Mock<DbSet<Member>>();
            mockSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.Provider);
            mockSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.Expression);
            mockSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.ElementType);
            mockSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());
            _contextMock.Setup(c => c.Members).Returns(mockSet.Object);
            // Mock DbSet<Rank> for context
            var ranks = new List<Rank> { new Rank { RankId = 1, RequiredPoints = 0, RankName = "Bronze" } }.AsQueryable();
            var mockRankSet = new Moq.Mock<DbSet<Rank>>();
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Provider).Returns(ranks.Provider);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Expression).Returns(ranks.Expression);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.ElementType).Returns(ranks.ElementType);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.GetEnumerator()).Returns(ranks.GetEnumerator());
            _contextMock.Setup(c => c.Ranks).Returns(mockRankSet.Object);
            // Act
            await service.AddScoreAsync("id", 5);
            // Assert
            Assert.Equal(15, member.Score);
            Assert.Equal(25, member.TotalPoints);
        }

        [Fact]
        public async Task DeductScoreAsync_ShouldDeductPoints_WhenEnoughPoints()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Members = new List<Member>() };
            var member = new Member { Score = 10, TotalPoints = 20, AccountId = "id", Account = account };
            account.Members.Add(member);
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            _memberRepoMock.Setup(r => r.Update(member));
            _memberRepoMock.Setup(r => r.Save());
            // Mock DbSet<Member> for context
            var members = new List<Member> { member }.AsQueryable();
            var mockSet = new Moq.Mock<DbSet<Member>>();
            mockSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.Provider);
            mockSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.Expression);
            mockSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.ElementType);
            mockSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());
            _contextMock.Setup(c => c.Members).Returns(mockSet.Object);
            // Mock DbSet<Rank> for context
            var ranks = new List<Rank> { new Rank { RankId = 1, RequiredPoints = 0, RankName = "Bronze" } }.AsQueryable();
            var mockRankSet = new Moq.Mock<DbSet<Rank>>();
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Provider).Returns(ranks.Provider);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Expression).Returns(ranks.Expression);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.ElementType).Returns(ranks.ElementType);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.GetEnumerator()).Returns(ranks.GetEnumerator());
            _contextMock.Setup(c => c.Ranks).Returns(mockRankSet.Object);
            // Act
            await service.DeductScoreAsync("id", 5);
            // Assert
            Assert.Equal(5, member.Score);
        }

        [Fact]
        public async Task DeductScoreAsync_ShouldNotDeduct_WhenNotEnoughPoints()
        {
            // Arrange
            var service = CreateService();
            var member = new Member { Score = 2, TotalPoints = 20, AccountId = "id" };
            var account = new Account { AccountId = "id", Members = new List<Member> { member } };
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            // Act
            await service.DeductScoreAsync("id", 5);
            // Assert
            Assert.Equal(2, member.Score);
        }

        [Fact]
        public async Task DeductScoreAsync_ShouldNotThrow_WhenAccountNotFound()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetById("id")).Returns((Account)null);
            // Act
            var exception = await Record.ExceptionAsync(() => service.DeductScoreAsync("id", 5));
            // Assert
            Assert.Null(exception); // Should not throw
            _accountRepoMock.Verify(r => r.GetById("id"), Times.Once);
            _memberRepoMock.Verify(r => r.Update(It.IsAny<Member>()), Times.Never);
            _memberRepoMock.Verify(r => r.Save(), Times.Never);
        }

        [Fact]
        public async Task DeductScoreAsync_ShouldNotSetTotalPointsBelowZero()
        {
            // Arrange
            var service = CreateService();
            var account = new Account { AccountId = "id", Members = new List<Member>() };
            var member = new Member { Score = 10, TotalPoints = 5, AccountId = "id", Account = account };
            account.Members.Add(member);
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(account);
            _memberRepoMock.Setup(r => r.Update(member));
            _memberRepoMock.Setup(r => r.Save());
            // Mock DbSet<Member> and DbSet<Rank> for context
            var members = new List<Member> { member }.AsQueryable();
            var mockSet = new Moq.Mock<DbSet<Member>>();
            mockSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.Provider);
            mockSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.Expression);
            mockSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.ElementType);
            mockSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());
            _contextMock.Setup(c => c.Members).Returns(mockSet.Object);
            var ranks = new List<Rank> { new Rank { RankId = 1, RequiredPoints = 0, RankName = "Bronze" } }.AsQueryable();
            var mockRankSet = new Moq.Mock<DbSet<Rank>>();
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Provider).Returns(ranks.Provider);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.Expression).Returns(ranks.Expression);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.ElementType).Returns(ranks.ElementType);
            mockRankSet.As<IQueryable<Rank>>().Setup(m => m.GetEnumerator()).Returns(ranks.GetEnumerator());
            _contextMock.Setup(c => c.Ranks).Returns(mockRankSet.Object);
            // Act
            await service.DeductScoreAsync("id", 5, deductFromTotalPoints: true);
            // Assert
            Assert.Equal(0, member.TotalPoints);
        }

        [Fact]
        public void GetByUsername_ShouldCallRepository()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetByUsername("user")).Returns(new Account());
            // Act
            var result = service.GetByUsername("user");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetById_ShouldCallRepository()
        {
            // Arrange
            var service = CreateService();
            _accountRepoMock.Setup(r => r.GetById("id")).Returns(new Account());
            // Act
            var result = service.GetById("id");
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CheckAndUpgradeRank_ShouldNotThrow_WhenMemberNotFound_InMemory()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "CheckAndUpgradeRankDb")
                .Options;
            using var context = new MovieTheaterContext(options);
            var service = new AccountService(
                _accountRepoMock.Object,
                _employeeRepoMock.Object,
                _memberRepoMock.Object,
                _httpContextAccessorMock.Object,
                CreateFakeEmailService(),
                _loggerMock.Object,
                context
            );
            // Act & Assert
            service.CheckAndUpgradeRank("id");
        }
    }
}
