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

        public IEnumerable<Employee> GetAll()
        {
            return _context.Employees.Include(m => m.Account).ToList();
        }

        public Employee? GetById(string employeeId)
        {
            return _context.Employees
                .Include(e => e.Account)
                .FirstOrDefault(m => m.EmployeeId == employeeId);
        }
        public Account? GetByUsername(string username)
        {
            return _context.Accounts.FirstOrDefault(a => a.Username == username);
        }

        public string GenerateEmployeeId()
        {
            var latestEmployee = _context.Employees
                .OrderByDescending(a => a.EmployeeId)
                .FirstOrDefault();

            if (latestEmployee == null)
            {
                return "EM001";
            }

            if (int.TryParse(latestEmployee.EmployeeId.Substring(2,3), out int number))
            {
                return $"EM{(number + 1):D3}";
            }

            return $"E{DateTime.Now:yyyyMMddHHmmss}";
        }

        public void Add(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.EmployeeId))
            {
                employee.EmployeeId = GenerateEmployeeId();
            }
            _context.Employees.Add(employee);
        }

        public void Update(Employee employee)
        {
            _context.Employees.Update(employee);
            _context.SaveChanges();
        }

        public void Delete(string employeeId)
        {
            var employee = _context.Employees.Find(employeeId);
            var account = _context.Accounts.Find(employee.AccountId);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
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
