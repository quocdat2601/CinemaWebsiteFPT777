using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JwtTestController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly IAccountService _accountService;

        public JwtTestController(IJwtService jwtService, IAccountService accountService)
        {
            _jwtService = jwtService;
            _accountService = accountService;
        }

        [HttpPost("test-generate")]
        [Authorize]
        [HttpPost("test-generate")]
        public IActionResult TestGenerateToken([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (_accountService.Authenticate(model.Username, model.Password, out var account))
            {
                var token = _jwtService.GenerateToken(account);
                return Ok(new { Token = token });
            }
            return BadRequest("Invalid credentials");
        }

        [Authorize]
        [HttpGet("test-validate")]
        public IActionResult TestValidateToken()
        {
            var token = Request.Cookies["JwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("No token found");
            }

            var isValid = _jwtService.ValidateToken(token);
            if (!isValid)
            {
                return BadRequest("Invalid token");
            }

            // Get information from token
            var user = HttpContext.User;
            var userId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                UserId = userId,
                Role = role,
                IsValid = isValid
            });
        }
    }
}