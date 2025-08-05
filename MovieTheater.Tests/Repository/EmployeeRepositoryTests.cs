using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class EmployeeRepositoryTests
    {
        private MovieTheaterContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "EmployeeRepoTestDb" + Guid.NewGuid())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public void GenerateEmployeeId_FirstEmployee_ReturnsEM001()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GenerateEmployeeId();

            // Assert
            Assert.Equal("EM001", result);
        }

        [Fact]
        public void GenerateEmployeeId_WithExistingEmployees_ReturnsNextId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Employees.Add(new Employee { EmployeeId = "EM001" });
            context.Employees.Add(new Employee { EmployeeId = "EM002" });
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GenerateEmployeeId();

            // Assert
            Assert.Equal("EM003", result);
        }

        [Fact]
        public void GenerateEmployeeId_WithInvalidFormat_ReturnsTimestampBasedId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Employees.Add(new Employee { EmployeeId = "INVALID" });
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GenerateEmployeeId();

            // Assert
            Assert.StartsWith("E", result);
            Assert.True(result.Length > 3);
        }

        [Fact]
        public void Add_EmployeeWithoutId_GeneratesIdAndAdds()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);
            var employee = new Employee { Status = true };

            // Act
            repo.Add(employee);
            repo.Save();

            // Assert
            Assert.NotNull(employee.EmployeeId);
            Assert.StartsWith("EM", employee.EmployeeId);
            Assert.Single(context.Employees);
        }

        [Fact]
        public void Add_EmployeeWithId_AddsAsIs()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);
            var employee = new Employee { EmployeeId = "CUSTOM001", Status = true };

            // Act
            repo.Add(employee);
            repo.Save();

            // Assert
            Assert.Equal("CUSTOM001", employee.EmployeeId);
            Assert.Single(context.Employees);
        }

        [Fact]
        public void GetById_ExistingEmployee_ReturnsEmployee()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            var employee = new Employee { EmployeeId = "EM001", AccountId = "AC001", Status = true };
            context.Accounts.Add(account);
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetById("EM001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EM001", result.EmployeeId);
            Assert.NotNull(result.Account);
            Assert.Equal("AC001", result.Account.AccountId);
        }

        [Fact]
        public void GetById_NonExistentEmployee_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetById("EM999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByUsername_ExistingAccount_ReturnsAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            context.Accounts.Add(account);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetByUsername("testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AC001", result.AccountId);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void GetByUsername_NonExistentAccount_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetByUsername("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAll_ReturnsAllEmployeesWithAccounts()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account1 = new Account { AccountId = "AC001", Username = "user1" };
            var account2 = new Account { AccountId = "AC002", Username = "user2" };
            var employee1 = new Employee { EmployeeId = "EM001", AccountId = "AC001", Status = true };
            var employee2 = new Employee { EmployeeId = "EM002", AccountId = "AC002", Status = false };
            context.Accounts.AddRange(account1, account2);
            context.Employees.AddRange(employee1, employee2);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetAll().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, emp => Assert.NotNull(emp.Account));
        }

        [Fact]
        public void Update_ExistingEmployee_UpdatesEmployee()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var employee = new Employee { EmployeeId = "EM001", Status = true };
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            employee.Status = false;
            repo.Update(employee);
            repo.Save();

            // Assert
            var updatedEmployee = context.Employees.Find("EM001");
            Assert.NotNull(updatedEmployee);
            Assert.False(updatedEmployee.Status);
        }

        [Fact]
        public void Delete_ExistingEmployeeWithAccount_RemovesEmployeeAndAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            var employee = new Employee { EmployeeId = "EM001", AccountId = "AC001", Status = true };
            context.Accounts.Add(account);
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            repo.Delete("EM001");
            repo.Save();

            // Assert
            Assert.Empty(context.Employees);
            Assert.Empty(context.Accounts);
        }

        [Fact]
        public void Delete_ExistingEmployeeWithoutAccount_DoesNotRemoveEmployee()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var employee = new Employee { EmployeeId = "EM001", Status = true };
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            repo.Delete("EM001");
            repo.Save();

            // Assert
            Assert.Single(context.Employees);
            Assert.Equal("EM001", context.Employees.First().EmployeeId);
        }

        [Fact]
        public void Delete_NonExistentEmployee_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);

            // Act
            repo.Delete("EM999");
            repo.Save();

            // Assert
            Assert.Empty(context.Employees);
        }

        [Fact]
        public void Delete_EmployeeWithNonExistentAccount_DoesNotRemoveEmployee()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var employee = new Employee { EmployeeId = "EM001", AccountId = "AC999", Status = true };
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            repo.Delete("EM001");
            repo.Save();

            // Assert
            Assert.Single(context.Employees);
            Assert.Equal("EM001", context.Employees.First().EmployeeId);
        }

        [Fact]
        public void ToggleStatus_ExistingEmployee_TogglesStatus()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var employee = new Employee { EmployeeId = "EM001", Status = true };
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            repo.ToggleStatus("EM001");
            repo.Save();

            // Assert
            var updatedEmployee = context.Employees.Find("EM001");
            Assert.NotNull(updatedEmployee);
            Assert.False(updatedEmployee.Status);
        }

        [Fact]
        public void ToggleStatus_NonExistentEmployee_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);

            // Act
            repo.ToggleStatus("EM999");
            repo.Save();

            // Assert
            Assert.Empty(context.Employees);
        }

        [Fact]
        public void Save_WithChanges_PersistsChanges()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);
            var employee = new Employee { EmployeeId = "EM001", Status = true };

            // Act
            repo.Add(employee);
            repo.Save();

            // Assert
            Assert.Single(context.Employees);
            var savedEmployee = context.Employees.Find("EM001");
            Assert.NotNull(savedEmployee);
        }

        [Fact]
        public void GetAll_WithNoEmployees_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetAll();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetById_EmployeeWithoutAccount_ReturnsEmployeeWithoutAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var employee = new Employee { EmployeeId = "EM001", Status = true };
            context.Employees.Add(employee);
            context.SaveChanges();
            var repo = new EmployeeRepository(context);

            // Act
            var result = repo.GetById("EM001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EM001", result.EmployeeId);
            Assert.Null(result.Account);
        }
    }
} 