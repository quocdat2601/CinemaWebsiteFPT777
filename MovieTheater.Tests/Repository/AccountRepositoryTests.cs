using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class AccountRepositoryTests
    {
        private MovieTheaterContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "AccountRepoTestDb" + Guid.NewGuid())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public void GenerateAccountId_FirstAccount_ReturnsAC001()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GenerateAccountId();

            // Assert
            Assert.Equal("AC001", result);
        }

        [Fact]
        public void GenerateAccountId_WithExistingAccounts_ReturnsNextId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Accounts.Add(new Account { AccountId = "AC001" });
            context.Accounts.Add(new Account { AccountId = "AC002" });
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GenerateAccountId();

            // Assert
            Assert.Equal("AC003", result);
        }

        [Fact]
        public void GenerateAccountId_WithInvalidFormat_ReturnsTimestampBasedId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Accounts.Add(new Account { AccountId = "INVALID" });
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GenerateAccountId();

            // Assert
            Assert.StartsWith("AC", result);
            Assert.True(result.Length > 3);
        }

        [Fact]
        public void Add_AccountWithoutId_GeneratesIdAndAdds()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);
            var account = new Account { Username = "testuser", Email = "test@example.com" };

            // Act
            repo.Add(account);
            repo.Save();

            // Assert
            Assert.NotNull(account.AccountId);
            Assert.StartsWith("AC", account.AccountId);
            Assert.Single(context.Accounts);
        }

        [Fact]
        public void Add_AccountWithId_AddsAsIs()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);
            var account = new Account { AccountId = "CUSTOM001", Username = "testuser" };

            // Act
            repo.Add(account);
            repo.Save();

            // Assert
            Assert.Equal("CUSTOM001", account.AccountId);
            Assert.Single(context.Accounts);
        }

        [Fact]
        public void GetById_ExistingAccount_ReturnsAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            context.Accounts.Add(account);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetById("AC001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AC001", result.AccountId);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void GetById_NonExistentAccount_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetById("NONEXISTENT");

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
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetByUsername("testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void GetByUsername_NonExistentAccount_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetByUsername("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAccountByEmail_ExistingAccount_ReturnsAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Email = "test@example.com" };
            context.Accounts.Add(account);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetAccountByEmail("test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetAccountByEmail_NonExistentAccount_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetAccountByEmail("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Authenticate_ExistingAccount_ReturnsAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            context.Accounts.Add(account);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.Authenticate("testuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void Authenticate_NonExistentAccount_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.Authenticate("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAll_ReturnsAllAccounts()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Accounts.Add(new Account { AccountId = "AC001", Username = "user1" });
            context.Accounts.Add(new Account { AccountId = "AC002", Username = "user2" });
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            var result = repo.GetAll();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Username == "user1");
            Assert.Contains(result, a => a.Username == "user2");
        }

        [Fact]
        public void Update_ExistingAccount_UpdatesProperties()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var originalAccount = new Account 
            { 
                AccountId = "AC001", 
                Username = "olduser", 
                Email = "old@example.com",
                FullName = "Old Name"
            };
            context.Accounts.Add(originalAccount);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            var updatedAccount = new Account
            {
                AccountId = "AC001",
                Username = "newuser",
                Email = "new@example.com",
                FullName = "New Name"
            };

            // Act
            repo.Update(updatedAccount);

            // Assert
            var result = context.Accounts.First(a => a.AccountId == "AC001");
            Assert.Equal("newuser", result.Username);
            Assert.Equal("new@example.com", result.Email);
            Assert.Equal("New Name", result.FullName);
        }

        [Fact]
        public void Update_NonExistentAccount_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);
            var account = new Account { AccountId = "NONEXISTENT", Username = "test" };

            // Act
            repo.Update(account);

            // Assert
            Assert.Empty(context.Accounts);
        }

        [Fact]
        public void Delete_ExistingAccount_RemovesAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            context.Accounts.Add(account);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            repo.Delete("AC001");
            repo.Save();

            // Assert
            Assert.Empty(context.Accounts);
        }

        [Fact]
        public void Delete_NonExistentAccount_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            repo.Delete("NONEXISTENT");
            repo.Save();

            // Assert
            Assert.Empty(context.Accounts);
        }

        [Fact]
        public async Task DeductScoreAsync_AccountWithMemberAndSufficientScore_DeductsScore()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            var member = new Member { MemberId = "M001", AccountId = "AC001", Score = 100 };
            context.Accounts.Add(account);
            context.Members.Add(member);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            await repo.DeductScoreAsync("AC001", 50);

            // Assert
            var updatedMember = context.Members.First(m => m.MemberId == "M001");
            Assert.Equal(50, updatedMember.Score);
        }

        [Fact]
        public async Task DeductScoreAsync_AccountWithMemberInsufficientScore_DoesNotDeduct()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            var member = new Member { MemberId = "M001", AccountId = "AC001", Score = 30 };
            context.Accounts.Add(account);
            context.Members.Add(member);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            await repo.DeductScoreAsync("AC001", 50);

            // Assert
            var updatedMember = context.Members.First(m => m.MemberId == "M001");
            Assert.Equal(30, updatedMember.Score);
        }

        [Fact]
        public async Task DeductScoreAsync_AccountWithoutMembers_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "AC001", Username = "testuser" };
            context.Accounts.Add(account);
            context.SaveChanges();
            var repo = new AccountRepository(context);

            // Act
            await repo.DeductScoreAsync("AC001", 50);

            // Assert
            // No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public async Task DeductScoreAsync_NonExistentAccount_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new AccountRepository(context);

            // Act
            await repo.DeductScoreAsync("NONEXISTENT", 50);

            // Assert
            // No exception should be thrown
            Assert.True(true);
        }
    }
} 