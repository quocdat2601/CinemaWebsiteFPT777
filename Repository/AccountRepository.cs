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

            // Fallback: tạo ID ngắn hơn để đảm bảo không vượt quá 10 ký tự
            var timestamp = DateTime.Now.ToString("MMddHHmm");
            return $"AC{timestamp}";
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
            return _context.Accounts
                .Include(e => e.Employees)
                .Include(a => a.Members)
                .Include(a => a.Rank)
                .FirstOrDefault(a => a.AccountId == id);
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

        //DEDUCT SCORE AFTER USE SCORE
        public async Task DeductScoreAsync(string accountId, int scoreToDeduct)
        {
            var account = await _context.Accounts
                .Include(a => a.Members)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null) return;

            // Nếu có nhiều Member, lấy member chính hoặc member đầu tiên
            var member = account.Members.FirstOrDefault();

            if (member != null && member.Score >= scoreToDeduct)
            {
                member.Score -= scoreToDeduct;
                await _context.SaveChangesAsync();
            }
        }

    }
}
