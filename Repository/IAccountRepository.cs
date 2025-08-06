using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IAccountRepository
    {
        string GenerateAccountId();
        public Account? GetById(string id);
        Account? Authenticate(string username);
        public Account? GetByUsername(string username);
        public void Add(Account account);
        public void Delete(string id);
        public void Update(Account account);
        public void Save();
        Account GetAccountByEmail(string email);
        IEnumerable<Account> GetAll();
        Task DeductScoreAsync(string userId, int points);
        public void ToggleStatus(string accountId);
    }
}
