using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using MovieTheater.Service;
using MovieTheater.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace MovieTheater.Tests.Service
{
    public class JwtServiceTests
    {
        private readonly JwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public JwtServiceTests()
        {
            _jwtSettings = new JwtSettings
            {
                SecretKey = "supersecretkey123456789012345678901234567890", // 42 chars for HmacSha256
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationInMinutes = 60
            };
            var options = new Mock<IOptions<JwtSettings>>();
            options.Setup(o => o.Value).Returns(_jwtSettings);
            _jwtService = new JwtService(options.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeJwtService()
        {
            // Arrange
            var jwtSettings = new JwtSettings
            {
                SecretKey = "testsecretkey123456789012345678901234567890",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationInMinutes = 30
            };
            var options = new Mock<IOptions<JwtSettings>>();
            options.Setup(o => o.Value).Returns(jwtSettings);

            // Act
            var jwtService = new JwtService(options.Object);

            // Assert
            Assert.NotNull(jwtService);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateValidToken_ForAdminAccount()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "admin123",
                Username = "admin",
                RoleId = 1,
                Status = 1,
                Image = "/image/admin.jpg"
            };

            // Act
            var token = _jwtService.GenerateToken(account);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate the generated token
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateValidToken_ForEmployeeAccount()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "emp123",
                Username = "employee",
                RoleId = 2,
                Status = 1,
                Image = "/image/employee.jpg"
            };

            // Act
            var token = _jwtService.GenerateToken(account);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate the generated token
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateValidToken_ForCustomerAccount()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "cust123",
                Username = "customer",
                RoleId = 3,
                Status = 1,
                Image = "/image/customer.jpg"
            };

            // Act
            var token = _jwtService.GenerateToken(account);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate the generated token
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateValidToken_ForGuestAccount()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "guest123",
                Username = "guest",
                RoleId = 999, // Unknown role ID
                Status = 1,
                Image = "/image/guest.jpg"
            };

            // Act
            var token = _jwtService.GenerateToken(account);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate the generated token
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void GenerateToken_ShouldUseDefaultImage_WhenImageIsNull()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "user123",
                Username = "user",
                RoleId = 1,
                Status = 1,
                Image = null
            };

            // Act
            var token = _jwtService.GenerateToken(account);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate the generated token
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void GenerateToken_ShouldUseDefaultImage_WhenImageIsEmpty()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "user123",
                Username = "user",
                RoleId = 1,
                Status = 1,
                Image = ""
            };

            // Act
            var token = _jwtService.GenerateToken(account);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            
            // Validate the generated token
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }

        [Fact]
        public void GenerateToken_ShouldIncludeAllRequiredClaims()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "test123",
                Username = "testuser",
                RoleId = 1,
                Status = 1,
                Image = "/image/test.jpg"
            };

            // Act
            var token = _jwtService.GenerateToken(account);
            var principal = _jwtService.GetPrincipalFromToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal(account.AccountId, principal.FindFirst("AccountId")?.Value);
            Assert.Equal(account.Username, principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal("Admin", principal.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal(account.Status.ToString(), principal.FindFirst("Status")?.Value);
            Assert.Equal(account.Image, principal.FindFirst("Image")?.Value);
        }

        [Fact]
        public void ValidateToken_ShouldReturnTrue_ForValidToken()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "valid123",
                Username = "validuser",
                RoleId = 1,
                Status = 1,
                Image = "/image/valid.jpg"
            };
            var token = _jwtService.GenerateToken(account);

            // Act
            var isValid = _jwtService.ValidateToken(token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_ForInvalidToken()
        {
            // Act
            var isValid = _jwtService.ValidateToken("invalid.token.value");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_ForEmptyToken()
        {
            // Act
            var isValid = _jwtService.ValidateToken("");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_ForNullToken()
        {
            // Act
            var isValid = _jwtService.ValidateToken(null);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_ForMalformedToken()
        {
            // Act
            var isValid = _jwtService.ValidateToken("not.a.valid.jwt.token");

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetPrincipalFromToken_ShouldReturnPrincipal_ForValidToken()
        {
            // Arrange
            var account = new Account
            {
                AccountId = "principal123",
                Username = "principaluser",
                RoleId = 1,
                Status = 1,
                Image = "/image/principal.jpg"
            };
            var token = _jwtService.GenerateToken(account);

            // Act
            var principal = _jwtService.GetPrincipalFromToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal(account.AccountId, principal.FindFirst("AccountId")?.Value);
            Assert.Equal(account.Username, principal.FindFirst(ClaimTypes.Name)?.Value);
        }

        [Fact]
        public void GetPrincipalFromToken_ShouldReturnNull_ForInvalidToken()
        {
            // Act
            var principal = _jwtService.GetPrincipalFromToken("invalid.token.value");

            // Assert
            Assert.Null(principal);
        }

        [Fact]
        public void GetPrincipalFromToken_ShouldReturnNull_ForEmptyToken()
        {
            // Act
            var principal = _jwtService.GetPrincipalFromToken("");

            // Assert
            Assert.Null(principal);
        }

        [Fact]
        public void GetPrincipalFromToken_ShouldReturnNull_ForNullToken()
        {
            // Act
            var principal = _jwtService.GetPrincipalFromToken(null);

            // Assert
            Assert.Null(principal);
        }

        [Fact]
        public void GetPrincipalFromToken_ShouldReturnNull_ForMalformedToken()
        {
            // Act
            var principal = _jwtService.GetPrincipalFromToken("not.a.valid.jwt.token");

            // Assert
            Assert.Null(principal);
        }

        [Theory]
        [InlineData(1, "Admin")]
        [InlineData(2, "Employee")]
        [InlineData(3, "Customer")]
        [InlineData(999, "Guest")]
        [InlineData(0, "Guest")]
        [InlineData(-1, "Guest")]
        public void GenerateToken_ShouldMapRoleIdToCorrectRoleName(int roleId, string expectedRole)
        {
            // Arrange
            var account = new Account
            {
                AccountId = "role123",
                Username = "roleuser",
                RoleId = roleId,
                Status = 1,
                Image = "/image/role.jpg"
            };

            // Act
            var token = _jwtService.GenerateToken(account);
            var principal = _jwtService.GetPrincipalFromToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal(expectedRole, principal.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public void GenerateToken_ShouldHandleNullAccount()
        {
            // Act & Assert
            var exception = Assert.Throws<NullReferenceException>(() => _jwtService.GenerateToken(null));
            Assert.NotNull(exception);
        }

        [Fact]
        public void GenerateToken_ShouldHandleAccountWithNullValues()
        {
            // Arrange
            var account = new Account
            {
                AccountId = null,
                Username = null,
                RoleId = null,
                Status = null,
                Image = null
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => _jwtService.GenerateToken(account));
            Assert.NotNull(exception);
        }
    }
} 