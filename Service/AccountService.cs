using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;

        public AccountService(IAccountRepository repository)
        {
            _repository = repository;
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
                RoleId = 3 
            };

            _repository.Add(account);
            _repository.Save();
            return true;
        }
        public bool Authenticate(string username, string password, out Account? account)
        {
            account = _repository.Authenticate(username, password);
            return account != null;
        }

    }
}
