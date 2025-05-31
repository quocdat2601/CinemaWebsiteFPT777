using MovieTheater.Models;
using System.Security.Claims;

namespace MovieTheater.Service
{
    public interface IJwtService
    {
        string GenerateToken(Account account);
        bool ValidateToken(string token);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
    }
} 