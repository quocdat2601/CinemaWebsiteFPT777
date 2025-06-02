using Microsoft.EntityFrameworkCore;
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
                return "AC001";
            }

            if (int.TryParse(latestAccount.AccountId.Substring(2, 3), out int number))
            {
                return $"AC{(number + 1):D3}";
            }

            return $"AC{DateTime.Now:yyyyMMddHHmmss}";
        }

        public void Add(Account account)
        {
            if (string.IsNullOrEmpty(account.AccountId))
            {
                account.AccountId = GenerateAccountId();
            }
            _context.Accounts.Add(account);
        }

        public void Delete(string id)
        {
            var account = _context.Accounts.FirstOrDefault(m => m.AccountId == id);
            if (account != null)
            {
                _context.Accounts.Remove(account);
            }
        }
        public Account? GetById(string id)
        {
            return _context.Accounts.FirstOrDefault(a => a.AccountId == id);
        }

        public Account? GetByUsername(string username)
        {
            return _context.Accounts.FirstOrDefault(a => a.Username == username);
        }
        public Account? GetAccountByEmail(string email)
        {
            return _context.Accounts.FirstOrDefault(a => a.Email == email);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
        public void Update(Account account)
        {
            var existing = _context.Accounts.FirstOrDefault(a => a.AccountId == account.AccountId);
            if (existing == null) return; 
            existing.Address = account.Address;
            existing.DateOfBirth = account.DateOfBirth;
            existing.Email = account.Email;
            existing.FullName = account.FullName;
            existing.Gender = account.Gender;
            existing.IdentityCard = account.IdentityCard;
            existing.Image = account.Image;
            existing.Password = account.Password;
            existing.PhoneNumber = account.PhoneNumber;
            existing.RegisterDate = account.RegisterDate;
            existing.RoleId = account.RoleId;
            existing.Status = account.Status;
            existing.Username = account.Username;
            _context.SaveChanges();
        }

        public IEnumerable<Account> GetAll()
        {
            return _context.Accounts.ToList();
        }
        public Account? Authenticate(string username)
        {
            return _context.Accounts
                .FirstOrDefault(a => a.Username == username);
        }

    }
}
