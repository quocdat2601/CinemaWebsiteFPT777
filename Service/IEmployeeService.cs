using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IEmployeeService
    {
        public bool Register(RegisterViewModel model);
        public IEnumerable<Employee> GetAll();
        public Employee? GetById(string id);
        public void Update(Employee employee);
        public void Delete(string employeeId);
        public void Save();

    }
}
