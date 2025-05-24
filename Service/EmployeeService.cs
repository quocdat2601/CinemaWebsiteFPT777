using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class EmployeeService : IEmployeeService
    
    {
        private readonly IEmployeeRepository _repository;

        public EmployeeService(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<Account> GetAll()
        {
            return _repository.GetAll();
        }

        public Account? GetById(string id)
        {
            return _repository.GetById(id);
        }

        public bool Register(RegisterViewModel model)
        {
            if (_repository.GetByUsername(model.Username) != null)
                return false;

            var account = new Account
            {
                Username = model.Username,
                Password = model.Password,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                IdentityCard = model.IdentityCard,
                Email = model.Email,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                Status = 1,
                RoleId = 2
            };

            _repository.Add(account);
            _repository.Save();
            return true;
        }
        public void Update(Account account)
        {
            _repository.Update(account);
            _repository.Save();
        }

        public void Delete(string accountId)
        {
            _repository.Delete(accountId);
            _repository.Save();
        }

        public void Save()
        {
            _repository.Save();
        }
    }
}
