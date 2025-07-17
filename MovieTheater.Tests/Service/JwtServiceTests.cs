using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using MovieTheater.Service;
using MovieTheater.Models;
using System.Security.Claims;

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
                SecretKey = "supersecretkey1234567890123456", // 32 chars for HmacSha256
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationInMinutes = 60
            };
            var options = new Mock<IOptions<JwtSettings>>();
            options.Setup(o => o.Value).Returns(_jwtSettings);
            _jwtService = new JwtService(options.Object);
        }

      

        [Fact]
        public void ValidateToken_ShouldReturn_False_ForInvalidToken()
        {
            var isValid = _jwtService.ValidateToken("invalid.token.value");
            Assert.False(isValid);
        }

       

        [Fact]
        public void GetPrincipalFromToken_ShouldReturn_Null_ForInvalidToken()
        {
            var principal = _jwtService.GetPrincipalFromToken("invalid.token.value");
            Assert.Null(principal);
        }
    }
} 