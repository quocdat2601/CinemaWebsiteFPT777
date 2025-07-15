using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class MovieServiceTests
    {
        private readonly Mock<IMovieRepository> _mockMovieRepository;
        private readonly Mock<ILogger<MovieService>> _mockLogger;
        private readonly MovieService _service;

        public MovieServiceTests()
        {
            _mockMovieRepository = new Mock<IMovieRepository>();
            _mockLogger = new Mock<ILogger<MovieService>>();
            _service = new MovieService(_mockMovieRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public void SearchMovies_WithValidSearchTerm_ReturnsFilteredMovies()
        {
            // Arrange
            var searchTerm = "Avengers";
            var allMovies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers", Duration = 120 },
                new Movie { MovieId = "2", MovieNameEnglish = "Batman", Duration = 110 },
                new Movie { MovieId = "3", MovieNameEnglish = "Avengers 2", Duration = 130 }
            };

            _mockMovieRepository.Setup(x => x.GetAll()).Returns(allMovies);

            // Act
            var result = _service.SearchMovies(searchTerm);

            // Assert
            var movies = result.ToList();
            Assert.Equal(2, movies.Count);
            Assert.All(movies, movie => Assert.Contains("avengers", movie.MovieNameEnglish.ToLower()));
        }

        [Fact]
        public void SearchMovies_WithEmptySearchTerm_ReturnsAllMoviesSorted()
        {
            // Arrange
            var allMovies = new List<Movie>
            {
                new Movie { MovieId = "2", MovieNameEnglish = "Batman", Duration = 110 },
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers", Duration = 120 },
                new Movie { MovieId = "3", MovieNameEnglish = "Zorro", Duration = 130 }
            };

            _mockMovieRepository.Setup(x => x.GetAll()).Returns(allMovies);

            // Act
            var result = _service.SearchMovies("");

            // Assert
            var movies = result.ToList();
            Assert.Equal(3, movies.Count);
            Assert.Equal("Avengers", movies[0].MovieNameEnglish);
            Assert.Equal("Batman", movies[1].MovieNameEnglish);
            Assert.Equal("Zorro", movies[2].MovieNameEnglish);
        }

        [Fact]
        public void SearchMovies_WithNullSearchTerm_ReturnsAllMoviesSorted()
        {
            // Arrange
            var allMovies = new List<Movie>
            {
                new Movie { MovieId = "2", MovieNameEnglish = "Batman", Duration = 110 },
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers", Duration = 120 }
            };

            _mockMovieRepository.Setup(x => x.GetAll()).Returns(allMovies);

            // Act
            var result = _service.SearchMovies(null);

            // Assert
            var movies = result.ToList();
            Assert.Equal(2, movies.Count);
            Assert.Equal("Avengers", movies[0].MovieNameEnglish);
            Assert.Equal("Batman", movies[1].MovieNameEnglish);
        }

        [Fact]
        public void ConvertToEmbedUrl_WithLongYoutubeUrl_ReturnsEmbedUrl()
        {
            // Arrange
            var longUrl = "https://www.youtube.com/watch?v=abc123";

            // Act
            var result = _service.ConvertToEmbedUrl(longUrl);

            // Assert
            Assert.Equal("https://www.youtube.com/embed/abc123", result);
        }

        [Fact]
        public void ConvertToEmbedUrl_WithShortYoutubeUrl_ReturnsEmbedUrl()
        {
            // Arrange
            var shortUrl = "https://youtu.be/abc123";

            // Act
            var result = _service.ConvertToEmbedUrl(shortUrl);

            // Assert
            Assert.Equal("https://www.youtube.com/embed/abc123", result);
        }

        [Fact]
        public void ConvertToEmbedUrl_WithNonYoutubeUrl_ReturnsOriginalUrl()
        {
            // Arrange
            var nonYoutubeUrl = "https://vimeo.com/123456";

            // Act
            var result = _service.ConvertToEmbedUrl(nonYoutubeUrl);

            // Assert
            Assert.Equal(nonYoutubeUrl, result);
        }

        [Fact]
        public void ConvertToEmbedUrl_WithEmptyUrl_ReturnsEmptyUrl()
        {
            // Arrange
            var emptyUrl = "";

            // Act
            var result = _service.ConvertToEmbedUrl(emptyUrl);

            // Assert
            Assert.Equal(emptyUrl, result);
        }

        [Fact]
        public void ConvertToEmbedUrl_WithNullUrl_ReturnsNull()
        {
            // Arrange
            string nullUrl = null;

            // Act
            var result = _service.ConvertToEmbedUrl(nullUrl);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AddMovie_WithValidMovie_ReturnsTrue()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", MovieNameEnglish = "Test Movie" };
            _mockMovieRepository.Setup(x => x.Add(movie)).Returns(true);

            // Act
            var result = _service.AddMovie(movie);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.Add(movie), Times.Once);
        }

        [Fact]
        public void AddMovie_WhenRepositoryFails_ReturnsFalse()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", MovieNameEnglish = "Test Movie" };
            _mockMovieRepository.Setup(x => x.Add(movie)).Returns(false);

            // Act
            var result = _service.AddMovie(movie);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddMovie_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", MovieNameEnglish = "Test Movie" };
            _mockMovieRepository.Setup(x => x.Add(movie)).Throws(new Exception("Database error"));

            // Act
            var result = _service.AddMovie(movie);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdateMovie_WithValidMovie_ReturnsTrue()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", MovieNameEnglish = "Updated Movie" };
            _mockMovieRepository.Setup(x => x.Update(movie)).Returns(true);

            // Act
            var result = _service.UpdateMovie(movie);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.Update(movie), Times.Once);
        }

        [Fact]
        public void UpdateMovie_WhenRepositoryFails_ReturnsFalse()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", MovieNameEnglish = "Updated Movie" };
            _mockMovieRepository.Setup(x => x.Update(movie)).Returns(false);

            // Act
            var result = _service.UpdateMovie(movie);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DeleteMovie_WithValidId_ReturnsTrue()
        {
            // Arrange
            var movieId = "1";
            _mockMovieRepository.Setup(x => x.Delete(movieId));

            // Act
            var result = _service.DeleteMovie(movieId);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.Delete(movieId), Times.Once);
            _mockMovieRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void GetById_WithValidId_ReturnsMovie()
        {
            // Arrange
            var movieId = "1";
            var expectedMovie = new Movie { MovieId = movieId, MovieNameEnglish = "Test Movie" };
            _mockMovieRepository.Setup(x => x.GetById(movieId)).Returns(expectedMovie);

            // Act
            var result = _service.GetById(movieId);

            // Assert
            Assert.Equal(expectedMovie, result);
        }

        [Fact]
        public void GetById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var movieId = "invalid";
            _mockMovieRepository.Setup(x => x.GetById(movieId)).Returns((Movie)null);

            // Act
            var result = _service.GetById(movieId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AddMovieShow_WithValidData_ReturnsTrue()
        {
            // Arrange
            var movieShow = new MovieShow
            {
                MovieId = "1",
                ShowDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                ScheduleId = 1,
                CinemaRoomId = 1
            };
            var movie = new Movie { MovieId = "1", Duration = 120 };

            _mockMovieRepository.Setup(x => x.GetById("1")).Returns(movie);
            _mockMovieRepository.Setup(x => x.IsScheduleAvailable(It.IsAny<DateOnly>(), 1, 1, 120)).Returns(true);
            _mockMovieRepository.Setup(x => x.AddMovieShow(movieShow));

            // Act
            var result = _service.AddMovieShow(movieShow);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.AddMovieShow(movieShow), Times.Once);
            _mockMovieRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void AddMovieShow_WithInvalidMovie_ReturnsFalse()
        {
            // Arrange
            var movieShow = new MovieShow
            {
                MovieId = "invalid",
                ShowDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                ScheduleId = 1,
                CinemaRoomId = 1
            };

            _mockMovieRepository.Setup(x => x.GetById("invalid")).Returns((Movie)null);

            // Act
            var result = _service.AddMovieShow(movieShow);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddMovieShow_WithUnavailableSchedule_ReturnsFalse()
        {
            // Arrange
            var movieShow = new MovieShow
            {
                MovieId = "1",
                ShowDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                ScheduleId = 1,
                CinemaRoomId = 1
            };
            var movie = new Movie { MovieId = "1", Duration = 120 };

            _mockMovieRepository.Setup(x => x.GetById("1")).Returns(movie);
            _mockMovieRepository.Setup(x => x.IsScheduleAvailable(It.IsAny<DateOnly>(), 1, 1, 120)).Returns(false);

            // Act
            var result = _service.AddMovieShow(movieShow);

            // Assert
            Assert.False(result);
        }
    }
} 