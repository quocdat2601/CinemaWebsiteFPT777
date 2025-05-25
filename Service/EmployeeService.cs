using Microsoft.EntityFrameworkCore;
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

        public bool Register(RegisterViewModel model)
        {
            // Set RoleId to 2 for employees
            model.RoleId = 2;
            return _accountService.Register(model);
        }

        public void Update(Employee employee)
        {
            _repository.Update(employee);
            _repository.Save();
        }

        public void Delete(string employeeId)
        {
            _repository.Delete(employeeId);
            _repository.Save();
        }

        public void Save()
        {
            _repository.Save();
        }
    }
}
