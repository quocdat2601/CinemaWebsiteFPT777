using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IEmployeeService
    {
        public bool Register(RegisterViewModel model);
        public IEnumerable<Employee> GetAll();
        public Employee? GetById(string id);
        public bool Update(string id, RegisterViewModel model);
        public bool Delete(string employeeId);
        public void Save();
        public void ToggleStatus(string employeeId);
    }
}
