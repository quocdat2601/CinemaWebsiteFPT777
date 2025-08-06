using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class RankRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly RankRepository _repository;

        public RankRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new RankRepository(_context);
            SeedData();
        }

        private void SeedData()
        {
            var ranks = new List<Rank>
            {
                new Rank 
                { 
                    RankId = 1, 
                    RankName = "Bronze", 
                    RequiredPoints = 500,
                    ColorGradient = "linear-gradient(45deg, #cd7f32, #b8860b)",
                    IconClass = "fas fa-medal"
                },
                new Rank 
                { 
                    RankId = 2, 
                    RankName = "Silver", 
                    RequiredPoints = 1000,
                    ColorGradient = "linear-gradient(45deg, #c0c0c0, #a9a9a9)",
                    IconClass = "fas fa-medal"
                },
                new Rank 
                { 
                    RankId = 3, 
                    RankName = "Gold", 
                    RequiredPoints = 2000,
                    ColorGradient = "linear-gradient(45deg, #ffd700, #ffed4e)",
                    IconClass = "fas fa-medal"
                }
            };

            var accounts = new List<Account>
            {
                new Account 
                { 
                    AccountId = "acc1", 
                    Username = "user1", 
                    Email = "user1@test.com",
                    RankId = 1
                },
                new Account 
                { 
                    AccountId = "acc2", 
                    Username = "user2", 
                    Email = "user2@test.com",
                    RankId = 2
                }
            };

            var members = new List<Member>
            {
                new Member 
                { 
                    MemberId = "mem1", 
                    AccountId = "acc1", 
                    TotalPoints = 500
                },
                new Member 
                { 
                    MemberId = "mem2", 
                    AccountId = "acc2", 
                    TotalPoints = 1500
                }
            };

            _context.Ranks.AddRange(ranks);
            _context.Accounts.AddRange(accounts);
            _context.Members.AddRange(members);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllRanksAsync_ReturnsAllRanks()
        {
            // Act
            var result = await _repository.GetAllRanksAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, r => r.RankName == "Bronze");
            Assert.Contains(result, r => r.RankName == "Silver");
            Assert.Contains(result, r => r.RankName == "Gold");
        }

        [Fact]
        public async Task GetAllRanksAsync_WhenNoRanks_ReturnsEmptyList()
        {
            // Arrange
            _context.Ranks.RemoveRange(_context.Ranks);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllRanksAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRankByIdAsync_WithValidId_ReturnsRank()
        {
            // Act
            var result = await _repository.GetRankByIdAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Silver", result.RankName);
            Assert.Equal(1000, result.RequiredPoints);
        }

        [Fact]
        public async Task GetRankByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetRankByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRankByIdAsync_WithZeroId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetRankByIdAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRankByIdAsync_WithNegativeId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetRankByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithValidAccountId_ReturnsAccountWithMemberAndRank()
        {
            // Act
            var result = await _repository.GetAccountWithMemberAsync("acc1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("acc1", result.AccountId);
            Assert.Equal("user1", result.Username);
            Assert.NotNull(result.Members);
            Assert.Single(result.Members);
            Assert.Equal("mem1", result.Members.First().MemberId);
            Assert.NotNull(result.Rank);
            Assert.Equal("Bronze", result.Rank.RankName);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithInvalidAccountId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetAccountWithMemberAsync("invalid-account");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithEmptyAccountId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetAccountWithMemberAsync("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithNullAccountId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetAccountWithMemberAsync(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithAccountWithoutMember_ReturnsAccountWithoutMember()
        {
            // Arrange
            var accountWithoutMember = new Account 
            { 
                AccountId = "acc3", 
                Username = "user3", 
                Email = "user3@test.com",
                RankId = 1
            };
            _context.Accounts.Add(accountWithoutMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAccountWithMemberAsync("acc3");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("acc3", result.AccountId);
            Assert.NotNull(result.Members);
            Assert.Empty(result.Members);
            Assert.NotNull(result.Rank);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithAccountWithoutRank_ReturnsAccountWithoutRank()
        {
            // Arrange
            var accountWithoutRank = new Account 
            { 
                AccountId = "acc4", 
                Username = "user4", 
                Email = "user4@test.com",
                RankId = null
            };
            _context.Accounts.Add(accountWithoutRank);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAccountWithMemberAsync("acc4");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("acc4", result.AccountId);
            Assert.Null(result.Rank);
        }

        [Fact]
        public async Task GetAllRanksAsync_ReturnsRanksInCorrectOrder()
        {
            // Act
            var result = await _repository.GetAllRanksAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Verify order by RankId
            Assert.Equal(1, result[0].RankId);
            Assert.Equal(2, result[1].RankId);
            Assert.Equal(3, result[2].RankId);
        }

        [Fact]
        public async Task GetRankByIdAsync_WithMaxIntId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetRankByIdAsync(int.MaxValue);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_WithAccountHavingMultipleMembers_ReturnsAccountWithAllMembers()
        {
            // Arrange
            var additionalMember = new Member 
            { 
                MemberId = "mem3", 
                AccountId = "acc1", 
                TotalPoints = 750
            };
            _context.Members.Add(additionalMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAccountWithMemberAsync("acc1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("acc1", result.AccountId);
            Assert.NotNull(result.Members);
            Assert.Equal(2, result.Members.Count);
            Assert.Contains(result.Members, m => m.MemberId == "mem1");
            Assert.Contains(result.Members, m => m.MemberId == "mem3");
        }
    }
} 