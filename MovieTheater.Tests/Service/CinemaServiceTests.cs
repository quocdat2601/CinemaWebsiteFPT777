using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class CinemaServiceTests
    {
        private readonly Mock<ICinemaRepository> _repositoryMock;
        private readonly CinemaService _service;

        public CinemaServiceTests()
        {
            _repositoryMock = new Mock<ICinemaRepository>();
            _service = new CinemaService(_repositoryMock.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllCinemaRooms()
        {
            // Arrange
            var expectedRooms = new List<CinemaRoom>
            {
                new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" },
                new CinemaRoom { CinemaRoomId = 2, CinemaRoomName = "Room 2" }
            };
            _repositoryMock.Setup(r => r.GetAll()).Returns(expectedRooms);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.Equal(expectedRooms, result);
            _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WithValidId_ReturnsCinemaRoom()
        {
            // Arrange
            var expectedRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            _repositoryMock.Setup(r => r.GetById(1)).Returns(expectedRoom);

            // Act
            var result = _service.GetById(1);

            // Assert
            Assert.Equal(expectedRoom, result);
            _repositoryMock.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void GetById_WithNullId_ReturnsNull()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetById(null)).Returns((CinemaRoom)null);

            // Act
            var result = _service.GetById(null);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetById(null), Times.Once);
        }

        [Fact]
        public void GetById_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetById(999)).Returns((CinemaRoom)null);

            // Act
            var result = _service.GetById(999);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetById(999), Times.Once);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomName = "New Room" };

            // Act
            _service.Add(cinemaRoom);

            // Assert
            _repositoryMock.Verify(r => r.Add(cinemaRoom), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            _repositoryMock.Setup(r => r.Delete(1)).Returns(Task.CompletedTask);
            _repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.Delete(1), Times.Once);
            _repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_CallsRepositorySave()
        {
            // Arrange
            _repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            // Act
            await _service.SaveAsync();

            // Assert
            _repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public async Task Update_WithValidCinemaRoom_ReturnsTrueAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Updated Room" };
            _repositoryMock.Setup(r => r.Update(cinemaRoom)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.Update(cinemaRoom);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.Update(cinemaRoom), Times.Once);
        }

        [Fact]
        public async Task Update_WhenRepositoryThrowsException_ReturnsFalseAsync()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Updated Room" };
            _repositoryMock.Setup(r => r.Update(cinemaRoom)).Throws(new Exception("Test exception"));

            // Act
            var result = await _service.Update(cinemaRoom);

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.Update(cinemaRoom), Times.Once);
        }

        [Fact]
        public async Task Enable_WithValidCinemaRoom_ReturnsTrue()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            _repositoryMock.Setup(r => r.Enable(cinemaRoom)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.Enable(cinemaRoom);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.Enable(cinemaRoom), Times.Once);
        }

        [Fact]
        public async Task Enable_WhenRepositoryThrowsException_ReturnsFalse()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            _repositoryMock.Setup(r => r.Enable(cinemaRoom)).Throws(new Exception("Test exception"));

            // Act
            var result = await _service.Enable(cinemaRoom);

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.Enable(cinemaRoom), Times.Once);
        }

        [Fact]
        public async Task Disable_WithValidCinemaRoom_ReturnsTrue()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            _repositoryMock.Setup(r => r.Disable(cinemaRoom)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.Disable(cinemaRoom);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.Disable(cinemaRoom), Times.Once);
        }

        [Fact]
        public async Task Disable_WhenRepositoryThrowsException_ReturnsFalse()
        {
            // Arrange
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            _repositoryMock.Setup(r => r.Disable(cinemaRoom)).Throws(new Exception("Test exception"));

            // Act
            var result = await _service.Disable(cinemaRoom);

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.Disable(cinemaRoom), Times.Once);
        }

        [Fact]
        public void GetRoomsByVersion_WithValidVersionId_ReturnsRooms()
        {
            // Arrange
            var expectedRooms = new List<CinemaRoom>
            {
                new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1", VersionId = 1 },
                new CinemaRoom { CinemaRoomId = 2, CinemaRoomName = "Room 2", VersionId = 1 }
            };
            _repositoryMock.Setup(r => r.GetRoomsByVersion(1)).Returns(expectedRooms);

            // Act
            var result = _service.GetRoomsByVersion(1);

            // Assert
            Assert.Equal(expectedRooms, result);
            _repositoryMock.Verify(r => r.GetRoomsByVersion(1), Times.Once);
        }

        [Fact]
        public void GetRoomsByVersion_WithNonExistentVersionId_ReturnsEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetRoomsByVersion(999)).Returns(new List<CinemaRoom>());

            // Act
            var result = _service.GetRoomsByVersion(999);

            // Assert
            Assert.Empty(result);
            _repositoryMock.Verify(r => r.GetRoomsByVersion(999), Times.Once);
        }

        [Fact]
        public void GetAll_WithEmptyRepository_ReturnsEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAll()).Returns(new List<CinemaRoom>());

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.Empty(result);
            _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public async Task Update_WithNullCinemaRoom_ThrowsArgumentNullException()
        {
            // Arrange
            CinemaRoom cinemaRoom = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.Update(cinemaRoom));
        }

        [Fact]
        public void Add_WithNullCinemaRoom_ThrowsArgumentNullException()
        {
            // Arrange
            CinemaRoom cinemaRoom = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Add(cinemaRoom));
        }

        [Fact]
        public async Task Enable_WithNullCinemaRoom_ThrowsArgumentNullException()
        {
            // Arrange
            CinemaRoom cinemaRoom = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.Enable(cinemaRoom));
        }

        [Fact]
        public async Task Disable_WithNullCinemaRoom_ThrowsArgumentNullException()
        {
            // Arrange
            CinemaRoom cinemaRoom = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.Disable(cinemaRoom));
        }
    }
}