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
        private MovieTheaterContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "RankRepoTestDb" + System.Guid.NewGuid())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public async Task GetAllRanksAsync_ReturnsAllRanks()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze" });
            context.Ranks.Add(new Rank { RankId = 2, RankName = "Silver" });
            context.SaveChanges();
            var repo = new RankRepository(context);
            // Act
            var result = await repo.GetAllRanksAsync();
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.RankName == "Bronze");
            Assert.Contains(result, r => r.RankName == "Silver");
        }

        [Fact]
        public async Task GetRankByIdAsync_ReturnsCorrectRank()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze" });
            context.Ranks.Add(new Rank { RankId = 2, RankName = "Silver" });
            context.SaveChanges();
            var repo = new RankRepository(context);
            // Act
            var result = await repo.GetRankByIdAsync(2);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Silver", result.RankName);
        }

        [Fact]
        public async Task GetAccountWithMemberAsync_ReturnsAccountWithMembersAndRank()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var rank = new Rank { RankId = 1, RankName = "Bronze" };
            var account = new Account { AccountId = "A1", Username = "user", Rank = rank, RankId = 1 };
            var member = new Member { MemberId = "M1", AccountId = "A1", Account = account };
            context.Ranks.Add(rank);
            context.Accounts.Add(account);
            context.Members.Add(member);
            context.SaveChanges();
            var repo = new RankRepository(context);
            // Act
            var result = await repo.GetAccountWithMemberAsync("A1");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("A1", result.AccountId);
            Assert.NotNull(result.Rank);
            Assert.Equal("Bronze", result.Rank.RankName);
            Assert.NotNull(result.Members);
            Assert.Single(result.Members);
            Assert.Equal("M1", result.Members.First().MemberId);
        }
    }
} 