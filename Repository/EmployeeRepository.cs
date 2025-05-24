using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class EmployeeRepository : IEmployeeRepository
    
    {
        private readonly MovieTheaterContext _context;

        public EmployeeRepository(MovieTheaterContext context)
        {
            _context = context;
        }
        public Account? GetByUsername(string username)
        {
            return _context.Accounts.FirstOrDefault(a => a.Username == username);
        }
        public IEnumerable<Account> GetAll()
        {
            return _context.Accounts
                .Where(e => e.RoleId.Equals(2))
                .ToList();
        }

        public Account? GetById(string accountId)
        {
            return _context.Accounts
                .FirstOrDefault(e => e.AccountId == accountId && e.RoleId.Equals(2));
        }

        public void Add(Account account)
        {
            _context.Accounts.Add(account);
            _context.SaveChanges();
        }

        public void Update(Account account)
        {
            _context.Accounts.Update(account);
            _context.SaveChanges();
        }

        public void Delete(string accountId)
        {
            var account = _context.Accounts.Find(accountId);
            if (account != null)
            {
                _context.Accounts.Remove(account);
                _context.SaveChanges();
            }
        }
        
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
