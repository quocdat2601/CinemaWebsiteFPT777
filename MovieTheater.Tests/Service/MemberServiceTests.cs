using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _mockRepository;
        private readonly MemberService _service;

        public MemberServiceTests()
        {
            _mockRepository = new Mock<IMemberRepository>();
            _service = new MemberService(_mockRepository.Object);
        }

        [Fact]
        public void GetById_WhenMemberExists_ReturnsMember()
        {
            // Arrange
            var memberId = "M001";
            var expectedMember = new Member { MemberId = memberId, Score = 100, TotalPoints = 500 };
            _mockRepository.Setup(r => r.GetById(memberId)).Returns(expectedMember);

            // Act
            var result = _service.GetById(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.Equal(100, result.Score);
            _mockRepository.Verify(r => r.GetById(memberId), Times.Once);
        }

        [Fact]
        public void GetById_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            var memberId = "M999";
            _mockRepository.Setup(r => r.GetById(memberId)).Returns((Member?)null);

            // Act
            var result = _service.GetById(memberId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetById(memberId), Times.Once);
        }

        [Fact]
        public void GetByAccountId_WhenMemberExists_ReturnsMember()
        {
            // Arrange
            var accountId = "ACC001";
            var expectedMember = new Member { MemberId = "M001", AccountId = accountId, Score = 100 };
            _mockRepository.Setup(r => r.GetByAccountId(accountId)).Returns(expectedMember);

            // Act
            var result = _service.GetByAccountId(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result.AccountId);
            _mockRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetByAccountId_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            var accountId = "ACC999";
            _mockRepository.Setup(r => r.GetByAccountId(accountId)).Returns((Member?)null);

            // Act
            var result = _service.GetByAccountId(accountId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetByMemberId_WhenMemberExists_ReturnsMember()
        {
            // Arrange
            var memberId = "M001";
            var expectedMember = new Member { MemberId = memberId, Score = 100 };
            _mockRepository.Setup(r => r.GetByMemberId(memberId)).Returns(expectedMember);

            // Act
            var result = _service.GetByMemberId(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            _mockRepository.Verify(r => r.GetByMemberId(memberId), Times.Once);
        }

        [Fact]
        public void GetByMemberId_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            var memberId = "M999";
            _mockRepository.Setup(r => r.GetByMemberId(memberId)).Returns((Member?)null);

            // Act
            var result = _service.GetByMemberId(memberId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByMemberId(memberId), Times.Once);
        }

        [Fact]
        public void GetByIdWithAccount_WhenMemberExists_ReturnsMember()
        {
            // Arrange
            var memberId = "M001";
            var expectedMember = new Member 
            { 
                MemberId = memberId, 
                Account = new Account { AccountId = "ACC001", Username = "testuser" }
            };
            _mockRepository.Setup(r => r.GetByIdWithAccount(memberId)).Returns(expectedMember);

            // Act
            var result = _service.GetByIdWithAccount(memberId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberId, result.MemberId);
            Assert.NotNull(result.Account);
            _mockRepository.Verify(r => r.GetByIdWithAccount(memberId), Times.Once);
        }

        [Fact]
        public void GetByIdWithAccount_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            var memberId = "M999";
            _mockRepository.Setup(r => r.GetByIdWithAccount(memberId)).Returns((Member?)null);

            // Act
            var result = _service.GetByIdWithAccount(memberId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdWithAccount(memberId), Times.Once);
        }

        [Fact]
        public void GetByIdWithAccountAndRank_WhenMemberExists_ReturnsMember()
        {
            // Arrange
            var accountId = "ACC001";
            var expectedMember = new Member 
            { 
                MemberId = "M001", 
                AccountId = accountId,
                Account = new Account { AccountId = accountId, Username = "testuser" }
            };
            _mockRepository.Setup(r => r.GetByAccountId(accountId)).Returns(expectedMember);

            // Act
            var result = _service.GetByIdWithAccountAndRank(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result.AccountId);
            Assert.NotNull(result.Account);
            _mockRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetByIdWithAccountAndRank_WhenMemberDoesNotExist_ReturnsNull()
        {
            // Arrange
            var accountId = "ACC999";
            _mockRepository.Setup(r => r.GetByAccountId(accountId)).Returns((Member?)null);

            // Act
            var result = _service.GetByIdWithAccountAndRank(accountId);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByAccountId(accountId), Times.Once);
        }

        [Fact]
        public void GetAll_ReturnsAllMembers()
        {
            // Arrange
            var expectedMembers = new List<Member>
            {
                new Member { MemberId = "M001", Score = 100 },
                new Member { MemberId = "M002", Score = 200 },
                new Member { MemberId = "M003", Score = 300 }
            };
            _mockRepository.Setup(r => r.GetAll()).Returns(expectedMembers);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            _mockRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAll_WhenNoMembers_ReturnsEmptyList()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAll()).Returns(new List<Member>());

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.Empty(result);
            _mockRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var member = new Member { MemberId = "M001", Score = 100 };

            // Act
            _service.Add(member);

            // Assert
            _mockRepository.Verify(r => r.Add(member), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var member = new Member { MemberId = "M001", Score = 150 };

            // Act
            _service.Update(member);

            // Assert
            _mockRepository.Verify(r => r.Update(member), Times.Once);
        }

        [Fact]
        public void Delete_CallsRepositoryDelete()
        {
            // Arrange
            var memberId = "M001";

            // Act
            _service.Delete(memberId);

            // Assert
            _mockRepository.Verify(r => r.Delete(memberId), Times.Once);
        }

        [Fact]
        public void Save_CallsRepositorySave()
        {
            // Act
            _service.Save();

            // Assert
            _mockRepository.Verify(r => r.Save(), Times.Once);
        }
    }
} 