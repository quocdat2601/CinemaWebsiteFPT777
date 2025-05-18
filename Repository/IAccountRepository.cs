using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IAccountRepository
    {
        string GenerateAccountId();
        void Add(Account account);
        Account? GetByUsername(string username);
        void Save();
        Account? Authenticate(string username, string password);

    }
}
