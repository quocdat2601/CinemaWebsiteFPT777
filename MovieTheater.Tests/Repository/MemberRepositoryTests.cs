using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class MemberRepositoryTests : IDisposable
    {
        private readonly MovieTheaterContext _context;
        private readonly MemberRepository _repository;

        public MemberRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MovieTheaterContext(options);
            _repository = new MemberRepository(_context);
            
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add Ranks
            var ranks = new List<Rank>
            {
                new Rank { RankId = 1, RankName = "Bronze", DiscountPercentage = 0, ColorGradient = "linear-gradient(45deg, #cd7f32, #b8860b)", IconClass = "fas fa-medal" },
                new Rank { RankId = 2, RankName = "Silver", DiscountPercentage = 5, ColorGradient = "linear-gradient(45deg, #c0c0c0, #a9a9a9)", IconClass = "fas fa-medal" },
                new Rank { RankId = 3, RankName = "Gold", DiscountPercentage = 10, ColorGradient = "linear-gradient(45deg, #ffd700, #ffb347)", IconClass = "fas fa-medal" }
            };
            _context.Ranks.AddRange(ranks);

            // Add Accounts
            var accounts = new List<Account>
            {
                new Account { AccountId = "ACC001", FullName = "John Doe", Email = "john@example.com", PhoneNumber = "0123456789", IdentityCard = "123456789", RankId = 1 },
                new Account { AccountId = "ACC002", FullName = "Jane Smith", Email = "jane@example.com", PhoneNumber = "0987654321", IdentityCard = "987654321", RankId = 2 },
                new Account { AccountId = "ACC003", FullName = "Bob Johnson", Email = "bob@example.com", PhoneNumber = "0555666777", IdentityCard = "555666777", RankId = 3 }
            };
            _context.Accounts.AddRange(accounts);

            // Add Members
            var members = new List<Member>
            {
                new Member { MemberId = "MB001", AccountId = "ACC001", Score = 100 },
                new Member { MemberId = "MB002", AccountId = "ACC002", Score = 250 },
                new Member { MemberId = "MB003", AccountId = "ACC003", Score = 500 }
            };
            _context.Members.AddRange(members);

            _context.SaveChanges();
        }

        [Fact]
        public void GetAll_ReturnsAllMembers()
        {
            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Equal(3, result.Count());
            Assert.All(result, member => Assert.NotNull(member.Account));
        }

        [Fact]
        public void GetById_ExistingMember_ReturnsMember()
        {
            // Arrange
            string memberId = "MB001";

            // Act
            var result = _repository.GetById(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.Equal("ACC001", result.AccountId);
            Assert.Equal(100, result.Score);
        }

        [Fact]
        public void GetById_NonExistingMember_ReturnsNull()
        {
            // Arrange
            string memberId = "MB999";

            // Act
            var result = _repository.GetById(memberId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GenerateMemberId_WithExistingMembers_ReturnsNextId()
        {
            // Act
            var newId = _repository.GenerateMemberId();

            // Assert
            Assert.StartsWith("MB", newId);
            Assert.Equal("MB004", newId); // Next after MB003
        }

        [Fact]
        public void GenerateMemberId_WithNoExistingMembers_ReturnsMB001()
        {
            // Arrange - Clear all members
            _context.Members.RemoveRange(_context.Members.ToList());
            _context.SaveChanges();

            // Act
            var newId = _repository.GenerateMemberId();

            // Assert
            Assert.Equal("MB001", newId);
        }

        [Fact]
        public void GenerateMemberId_WithInvalidMemberIds_ReturnsTimestampBasedId()
        {
            // Arrange - Add member with invalid ID format that cannot be parsed (non-numeric)
            var invalidMember = new Member { MemberId = "MBABC", AccountId = "ACC999", Score = 0 };
            _context.Members.Add(invalidMember);
            _context.SaveChanges();

            // Act
            var newId = _repository.GenerateMemberId();

            // Assert
            Assert.StartsWith("MB", newId);
            // Check if it contains current date and time (format: yyyyMMddHHmmss)
            var currentDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            Assert.Contains(currentDateTime.Substring(0, 8), newId); // Check date part only
        }

        [Fact]
        public void Add_ValidMember_AddsToContext()
        {
            // Arrange
            var member = new Member
            {
                AccountId = "ACC004",
                Score = 150
            };

            // Act
            _repository.Add(member);
            _repository.Save(); // Save changes to database

            // Assert
            var addedMember = _context.Members.FirstOrDefault(m => m.AccountId == "ACC004");
            Assert.NotNull(addedMember);
            Assert.NotNull(addedMember.MemberId); // Should be auto-generated
        }

        [Fact]
        public void Add_MemberWithExistingId_KeepsExistingId()
        {
            // Arrange
            var member = new Member
            {
                MemberId = "MB999",
                AccountId = "ACC004",
                Score = 150
            };

            // Act
            _repository.Add(member);
            _repository.Save(); // Save changes to database

            // Assert
            var addedMember = _context.Members.FirstOrDefault(m => m.MemberId == "MB999");
            Assert.NotNull(addedMember);
            Assert.Equal("MB999", addedMember.MemberId);
        }

        [Fact]
        public void Update_ExistingMember_UpdatesSuccessfully()
        {
            // Arrange
            var member = _repository.GetById("MB001");
            member.Score = 200;

            // Act
            _repository.Update(member);

            // Assert
            var updatedMember = _repository.GetById("MB001");
            Assert.Equal(200, updatedMember.Score);
        }

        [Fact]
        public void Delete_ExistingMember_RemovesFromContext()
        {
            // Arrange
            string memberId = "MB001";

            // Act
            _repository.Delete(memberId);

            // Assert
            var deletedMember = _repository.GetById(memberId);
            Assert.Null(deletedMember);
        }

        [Fact]
        public void Delete_NonExistingMember_DoesNothing()
        {
            // Arrange
            string memberId = "MB999";

            // Act
            _repository.Delete(memberId);

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void Save_SavesChangesToDatabase()
        {
            // Arrange
            var member = new Member
            {
                MemberId = "MB999",
                AccountId = "ACC004",
                Score = 300
            };
            _context.Members.Add(member);

            // Act
            _repository.Save();

            // Assert
            var savedMember = _repository.GetById("MB999");
            Assert.NotNull(savedMember);
        }

        [Fact]
        public void GetByIdentityCard_ExistingIdentityCard_ReturnsMember()
        {
            // Arrange
            string identityCard = "123456789";

            // Act
            var result = _repository.GetByIdentityCard(identityCard);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MB001", result.MemberId);
            Assert.NotNull(result.Account);
            Assert.Equal(identityCard, result.Account.IdentityCard);
        }

        [Fact]
        public void GetByIdentityCard_NonExistingIdentityCard_ReturnsNull()
        {
            // Arrange
            string identityCard = "999999999";

            // Act
            var result = _repository.GetByIdentityCard(identityCard);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByAccountId_ExistingAccountId_ReturnsMember()
        {
            // Arrange
            string accountId = "ACC002";

            // Act
            var result = _repository.GetByAccountId(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MB002", result.MemberId);
            Assert.Equal(accountId, result.AccountId);
            Assert.NotNull(result.Account);
            Assert.NotNull(result.Account.Rank);
        }

        [Fact]
        public void GetByAccountId_NonExistingAccountId_ReturnsNull()
        {
            // Arrange
            string accountId = "ACC999";

            // Act
            var result = _repository.GetByAccountId(accountId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByMemberId_ExistingMemberId_ReturnsMember()
        {
            // Arrange
            string memberId = "MB003";

            // Act
            var result = _repository.GetByMemberId(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.Equal("ACC003", result.AccountId);
            Assert.NotNull(result.Account);
            Assert.NotNull(result.Account.Rank);
        }

        [Fact]
        public void GetByMemberId_NonExistingMemberId_ReturnsNull()
        {
            // Arrange
            string memberId = "MB999";

            // Act
            var result = _repository.GetByMemberId(memberId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetByIdWithAccount_ExistingMemberId_ReturnsMember()
        {
            // Arrange
            string memberId = "MB001";

            // Act
            var result = _repository.GetByIdWithAccount(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.NotNull(result.Account);
            Assert.Equal("John Doe", result.Account.FullName);
        }

        [Fact]
        public void GetByIdWithAccount_NonExistingMemberId_ReturnsNull()
        {
            // Arrange
            string memberId = "MB999";

            // Act
            var result = _repository.GetByIdWithAccount(memberId);

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 