using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class RankServiceTests
    {
        private MovieTheaterContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "RankServiceTestDb" + System.Guid.NewGuid())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public void GetAllRanks_ShouldReturnAllRanksSorted()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.AddRange(
                new Rank { RankId = 2, RankName = "Silver", RequiredPoints = 100 },
                new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 }
            );
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            // Act
            var result = service.GetAllRanks();
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Bronze", result[0].CurrentRankName);
            Assert.Equal("Silver", result[1].CurrentRankName);
        }

        [Fact]
        public void GetRankInfoForUser_ShouldReturnNull_WhenUserNotFound()
        {
            // Arrange
            var accountRepo = new Mock<IAccountRepository>();
            var memberRepo = new Mock<IMemberRepository>();
            var rankRepo = new Mock<IRankRepository>();
            using var context = CreateInMemoryContext();
            var service = new RankService(accountRepo.Object, context, memberRepo.Object, rankRepo.Object);
            // Act
            var result = service.GetRankInfoForUser("notfound");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRankInfoForUser_ShouldReturnNull_WhenMemberNotFound()
        {
            // Arrange
            var accountRepo = new Mock<IAccountRepository>();
            var memberRepo = new Mock<IMemberRepository>();
            var rankRepo = new Mock<IRankRepository>();
            using var context = CreateInMemoryContext();
            accountRepo.Setup(r => r.GetById("id")).Returns(new Account { AccountId = "id", RankId = 1 });
            memberRepo.Setup(r => r.GetByAccountId("id")).Returns((Member)null);
            var service = new RankService(accountRepo.Object, context, memberRepo.Object, rankRepo.Object);
            // Act
            var result = service.GetRankInfoForUser("id");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRankInfoForUser_ShouldReturnNull_WhenCurrentRankNotFound()
        {
            // Arrange
            var accountRepo = new Mock<IAccountRepository>();
            var memberRepo = new Mock<IMemberRepository>();
            var rankRepo = new Mock<IRankRepository>();
            using var context = CreateInMemoryContext();
            accountRepo.Setup(r => r.GetById("id")).Returns(new Account { AccountId = "id", RankId = 1 });
            memberRepo.Setup(r => r.GetByAccountId("id")).Returns(new Member { AccountId = "id", Score = 10, TotalPoints = 10 });
            rankRepo.Setup(r => r.GetAllRanksAsync()).ReturnsAsync(new List<Rank>()); // No ranks
            var service = new RankService(accountRepo.Object, context, memberRepo.Object, rankRepo.Object);
            // Act
            var result = service.GetRankInfoForUser("id");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRankInfoForUserAsync_ShouldReturnMaxRank_WhenNoNextRank()
        {
            // Arrange
            var accountRepo = new Mock<IAccountRepository>();
            var memberRepo = new Mock<IMemberRepository>();
            var rankRepo = new Mock<IRankRepository>();
            using var context = CreateInMemoryContext();
            accountRepo.Setup(r => r.GetById("id")).Returns(new Account { AccountId = "id", RankId = 2 });
            memberRepo.Setup(r => r.GetByAccountId("id")).Returns(new Member { AccountId = "id", Score = 100, TotalPoints = 100 });
            var ranks = new List<Rank> {
                new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 },
                new Rank { RankId = 2, RankName = "Silver", RequiredPoints = 100 }
            };
            rankRepo.Setup(r => r.GetAllRanksAsync()).ReturnsAsync(ranks);
            var service = new RankService(accountRepo.Object, context, memberRepo.Object, rankRepo.Object);
            // Act
            var result = await service.GetRankInfoForUserAsync("id");
            // Assert
            Assert.False(result.HasNextRank);
            Assert.Equal(100, result.ProgressToNextRank);
        }

        [Fact]
        public async Task GetRankInfoForUserAsync_ShouldReturnProgressToNextRank()
        {
            // Arrange
            var accountRepo = new Mock<IAccountRepository>();
            var memberRepo = new Mock<IMemberRepository>();
            var rankRepo = new Mock<IRankRepository>();
            using var context = CreateInMemoryContext();
            accountRepo.Setup(r => r.GetById("id")).Returns(new Account { AccountId = "id", RankId = 1 });
            memberRepo.Setup(r => r.GetByAccountId("id")).Returns(new Member { AccountId = "id", Score = 50, TotalPoints = 50 });
            var ranks = new List<Rank> {
                new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 },
                new Rank { RankId = 2, RankName = "Silver", RequiredPoints = 100 }
            };
            rankRepo.Setup(r => r.GetAllRanksAsync()).ReturnsAsync(ranks);
            var service = new RankService(accountRepo.Object, context, memberRepo.Object, rankRepo.Object);
            // Act
            var result = await service.GetRankInfoForUserAsync("id");
            // Assert
            Assert.True(result.HasNextRank);
            Assert.True(result.ProgressToNextRank > 0 && result.ProgressToNextRank < 100);
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new RankService(null, context, null, null);
            // Act
            var result = service.GetById(99);
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetById_ShouldReturnRank_WhenFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            // Act
            var result = service.GetById(1);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bronze", result.CurrentRankName);
        }

        [Fact]
        public void Create_ShouldReturnFalse_WhenModelIsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new RankService(null, context, null, null);
            // Act
            var result = service.Create(null);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Create_ShouldThrow_WhenDuplicateName()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankName = "Bronze", RequiredPointsForCurrentRank = 10 };
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => service.Create(model));
        }

        [Fact]
        public void Create_ShouldThrow_WhenDuplicateRequiredPoints()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 10 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankName = "Silver", RequiredPointsForCurrentRank = 10 };
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => service.Create(model));
        }

        [Fact]
        public void Create_ShouldAddRank_WhenValid()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankName = "Gold", RequiredPointsForCurrentRank = 100 };
            // Act
            var result = service.Create(model);
            // Assert
            Assert.True(result);
            Assert.Single(context.Ranks.Where(r => r.RankName == "Gold"));
        }

        [Fact]
        public void Update_ShouldReturnFalse_WhenNotFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankId = 99, CurrentRankName = "Bronze", RequiredPointsForCurrentRank = 0 };
            // Act
            var result = service.Update(model);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_ShouldThrow_WhenDuplicateName()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 });
            context.Ranks.Add(new Rank { RankId = 2, RankName = "Silver", RequiredPoints = 10 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankId = 2, CurrentRankName = "Bronze", RequiredPointsForCurrentRank = 10 };
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => service.Update(model));
        }

        [Fact]
        public void Update_ShouldThrow_WhenDuplicateRequiredPoints()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 });
            context.Ranks.Add(new Rank { RankId = 2, RankName = "Silver", RequiredPoints = 10 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankId = 2, CurrentRankName = "Silver", RequiredPointsForCurrentRank = 0 };
            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => service.Update(model));
        }

        [Fact]
        public void Update_ShouldUpdateRank_WhenValid()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            var model = new RankInfoViewModel { CurrentRankId = 1, CurrentRankName = "Bronze Updated", RequiredPointsForCurrentRank = 0 };
            // Act
            var result = service.Update(model);
            // Assert
            Assert.True(result);
            Assert.Equal("Bronze Updated", context.Ranks.First().RankName);
        }

        [Fact]
        public void Delete_ShouldReturnFalse_WhenNotFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new RankService(null, context, null, null);
            // Act
            var result = service.Delete(99);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Delete_ShouldRemoveRank_WhenFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Ranks.Add(new Rank { RankId = 1, RankName = "Bronze", RequiredPoints = 0 });
            context.SaveChanges();
            var service = new RankService(null, context, null, null);
            // Act
            var result = service.Delete(1);
            // Assert
            Assert.True(result);
            Assert.Empty(context.Ranks);
        }
    }
} 