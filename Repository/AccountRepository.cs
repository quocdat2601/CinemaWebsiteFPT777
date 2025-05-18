using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly MovieTheaterContext _context;

        public AccountRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public string GenerateAccountId()
        {
            var latestAccount = _context.Accounts
                .OrderByDescending(a => a.AccountId)
                .FirstOrDefault();

            if (latestAccount == null)
            {
                return "ACC0001";
            }

            if (int.TryParse(latestAccount.AccountId.Substring(3), out int number))
            {
                return $"ACC{(number + 1):D4}";
            }

            return $"ACC{DateTime.Now:yyyyMMddHHmmss}";
        }

        public void Add(Account account)
        {
            if (string.IsNullOrEmpty(account.AccountId))
            {
                account.AccountId = GenerateAccountId();
            }
            _context.Accounts.Add(account);
        }

        public Account? GetByUsername(string username)
        {
            return _context.Accounts.FirstOrDefault(a => a.Username == username);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
        public Account? Authenticate(string username, string password)
        {
            return _context.Accounts
                .FirstOrDefault(a => a.Username == username && a.Password == password);
        }

    }
}
