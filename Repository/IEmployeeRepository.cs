using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IEmployeeRepository
    {
        public IEnumerable<Account> GetAll();
        public Account? GetById(string accountId);
        public void Add(Account account);
        public void Update(Account account);
        public void Delete(string accountId);
        public void Save();
        public Account? GetByUsername(string username);

    }
}
