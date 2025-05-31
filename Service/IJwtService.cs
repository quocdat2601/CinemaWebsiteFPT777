using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IJwtService
    {
        string GenerateToken(Account account);
        bool ValidateToken(string token);
    }
} 