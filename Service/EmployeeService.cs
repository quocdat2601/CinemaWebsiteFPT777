using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IAccountService _accountService;

        public EmployeeService(IEmployeeRepository repository, IAccountService accountService)
        {
            _repository = repository;
            _accountService = accountService;
        }

        public IEnumerable<Employee> GetAll()
        {
            return _repository.GetAll();
        }

        public Employee? GetById(string id)
        {
            return _repository.GetById(id);
        }

        //FIX REGIS TO DBO EMPLOYEE
        public bool Register(RegisterViewModel model)
        {
            model.RoleId = 2;
            var result = _accountService.Register(model);
            return result;
        }

        public void ToggleStatus(string employeeId)
        {
            _repository.ToggleStatus(employeeId);
            _repository.Save();
        }

        public bool Update(string id, RegisterViewModel model)
        {
            var employee = _repository.GetById(id);
            if (employee == null) return false;
            var accountID = employee.AccountId;
            return _accountService.Update(accountID, model);
        }

        public bool Delete(string employeeId)
        {
            var employee = _repository.GetById(employeeId);
            if (employee == null) return false;
            _repository.Delete(employeeId);
            _repository.Save();
            return true;
        }

        public void Save()
        {
            _repository.Save();
        }
    }
}
