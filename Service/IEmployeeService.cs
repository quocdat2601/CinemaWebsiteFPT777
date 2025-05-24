using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IEmployeeService
    {
        public bool Register(RegisterViewModel model);
        public IEnumerable<Account> GetAll();
        public Account? GetById(string id);
        public void Update(Account account);
        public void Delete(string accountId);
        public void Save();

    }
}
