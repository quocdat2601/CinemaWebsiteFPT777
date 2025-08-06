using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class VersionRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly VersionRepository _repository;

        public VersionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new VersionRepository(_context);
        }

        [Fact]
        public void GetAll_ReturnsAllVersions()
        {
            // Arrange
            var versions = new List<Models.Version>
            {
                new Models.Version { VersionId = 1, VersionName = "2D" },
                new Models.Version { VersionId = 2, VersionName = "3D" },
                new Models.Version { VersionId = 3, VersionName = "IMAX" }
            };
            _context.Versions.AddRange(versions);
            _context.SaveChanges();

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void GetAll_WhenNoVersions_ReturnsEmptyList()
        {
            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetById_WhenVersionExists_ReturnsVersion()
        {
            // Arrange
            var version = new Models.Version { VersionId = 1, VersionName = "2D" };
            _context.Versions.Add(version);
            _context.SaveChanges();

            // Act
            var result = _repository.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VersionId);
            Assert.Equal("2D", result.VersionName);
        }

        [Fact]
        public void GetById_WhenVersionDoesNotExist_ReturnsNull()
        {
            // Act
            var result = _repository.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_AddsVersionToDatabase()
        {
            // Arrange
            var version = new Models.Version { VersionId = 1, VersionName = "2D" };

            // Act
            _repository.Add(version);
            _repository.Save();

            // Assert
            var savedVersion = _context.Versions.Find(1);
            Assert.NotNull(savedVersion);
            Assert.Equal("2D", savedVersion.VersionName);
        }

        [Fact]
        public void Update_WhenVersionExists_UpdatesVersion()
        {
            // Arrange
            var version = new Models.Version { VersionId = 1, VersionName = "2D" };
            _context.Versions.Add(version);
            _context.SaveChanges();

            // Clear the context to avoid tracking issues
            _context.ChangeTracker.Clear();

            var updatedVersion = new Models.Version { VersionId = 1, VersionName = "3D" };

            // Act
            _repository.Update(updatedVersion);

            // Assert
            var result = _context.Versions.Find(1);
            Assert.NotNull(result);
            Assert.Equal("3D", result.VersionName);
        }

        [Fact]
        public void Update_WhenVersionDoesNotExist_ThrowsException()
        {
            // Arrange
            var version = new Models.Version { VersionId = 999, VersionName = "Non-existent" };

            // Act & Assert - Should throw DbUpdateConcurrencyException
            var exception = Assert.Throws<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => _repository.Update(version));
            Assert.NotNull(exception);
        }

        [Fact]
        public void Delete_WhenVersionExists_RemovesVersion()
        {
            // Arrange
            var version = new Models.Version { VersionId = 1, VersionName = "2D" };
            _context.Versions.Add(version);
            _context.SaveChanges();

            // Act
            var result = _repository.Delete(1);

            // Assert
            Assert.True(result);
            var deletedVersion = _context.Versions.Find(1);
            Assert.Null(deletedVersion);
        }

        [Fact]
        public void Delete_WhenVersionDoesNotExist_ReturnsTrue()
        {
            // Act
            var result = _repository.Delete(999);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Save_SavesChangesToDatabase()
        {
            // Arrange
            var version = new Models.Version { VersionId = 1, VersionName = "2D" };
            _repository.Add(version);

            // Act
            _repository.Save();

            // Assert
            var savedVersion = _context.Versions.Find(1);
            Assert.NotNull(savedVersion);
        }
    }
} 