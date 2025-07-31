using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IEmployeeRepository
    {
        public IEnumerable<Employee> GetAll();
        public Employee? GetById(string employeeId);
        public void Add(Employee employee);
        public void Update(Employee employee);
        public void Delete(string employeeId);
        public void Save();
        public string GenerateEmployeeId();
        public Account? GetByUsername(string username);
        public void ToggleStatus(string employeeId);
    }
}
