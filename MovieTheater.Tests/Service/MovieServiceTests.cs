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
        public void CheckDurationConflict_WithNoConflict_ReturnsEmptyString()
        {
            // Arrange
            var movieId = "1";
            var newDuration = 120;
            var movieShows = new List<MovieShow>();
            var allMovieShows = new List<MovieShow>();

            _mockMovieRepository.Setup(x => x.GetMovieShowsByMovieId(movieId)).Returns(movieShows);
            _mockMovieRepository.Setup(x => x.GetMovieShow()).Returns(allMovieShows);

            // Act
            var result = _service.CheckDurationConflict(movieId, newDuration);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CheckDurationConflict_WithConflict_ReturnsConflictMessage()
        {
            // Arrange
            var movieId = "1";
            var newDuration = 120;
            var showDate = DateOnly.FromDateTime(DateTime.Now);
            
            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 1,
                    ShowDate = showDate,
                    Schedule = new Schedule { ScheduleTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)) },
                    CinemaRoom = new CinemaRoom { CinemaRoomName = "Room 1" }
                }
            };
            var allMovieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 2,
                    CinemaRoomId = 1,
                    ShowDate = showDate,
                    Schedule = new Schedule { ScheduleTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(15)) },
                    Movie = new Movie { Duration = 90, MovieNameEnglish = "Test Movie" }
                }
            };

            _mockMovieRepository.Setup(x => x.GetMovieShowsByMovieId(movieId)).Returns(movieShows);
            _mockMovieRepository.Setup(x => x.GetMovieShow()).Returns(allMovieShows);

            // Act
            var result = _service.CheckDurationConflict(movieId, newDuration);

            // Assert
            Assert.NotEqual(string.Empty, result);
            Assert.Contains("conflict", result.ToLower());
        }

        [Fact]
        public void CheckDurationConflict_WithEmptyData_ReturnsEmptyString()
        {
            // Arrange
            var movieId = "1";
            var newDuration = 120;
            var movieShows = new List<MovieShow>();
            var allMovieShows = new List<MovieShow>();

            _mockMovieRepository.Setup(x => x.GetMovieShowsByMovieId(movieId)).Returns(movieShows);
            _mockMovieRepository.Setup(x => x.GetMovieShow()).Returns(allMovieShows);

            // Act
            var result = _service.CheckDurationConflict(movieId, newDuration);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CheckDurationConflict_WithNullData_ReturnsEmptyString()
        {
            // Arrange
            var movieId = "1";
            var newDuration = 120;

            _mockMovieRepository.Setup(x => x.GetMovieShowsByMovieId(movieId)).Returns((List<MovieShow>)null);
            _mockMovieRepository.Setup(x => x.GetMovieShow()).Returns((List<MovieShow>)null);

            // Act
            var result = _service.CheckDurationConflict(movieId, newDuration);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetShowDatesForMovie_WithValidDateRange_ReturnsDateList()
        {
            // Arrange
            var movie = new Movie
            {
                FromDate = DateOnly.FromDateTime(DateTime.Now),
                ToDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2))
            };

            // Act
            var result = _service.GetShowDatesForMovie(movie);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(movie.FromDate, result[0]);
            Assert.Equal(movie.ToDate, result[2]);
        }

        [Fact]
        public void GetShowDatesForMovie_WithNullDates_ReturnsEmptyList()
        {
            // Arrange
            var movie = new Movie
            {
                FromDate = null,
                ToDate = null
            };

            // Act
            var result = _service.GetShowDatesForMovie(movie);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SaveImageAsync_WithValidFile_ReturnsImagePath()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.Length).Returns(1024);
            mockFile.Setup(x => x.FileName).Returns("test.jpg");
            mockFile.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SaveImageAsync(mockFile.Object);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("/image/", result);
            Assert.EndsWith("test.jpg", result);
        }

        [Fact]
        public async Task SaveImageAsync_WithNullFile_ReturnsEmptyString()
        {
            // Arrange
            IFormFile nullFile = null;

            // Act
            var result = await _service.SaveImageAsync(nullFile);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task SaveImageAsync_WithEmptyFile_ReturnsEmptyString()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(x => x.Length).Returns(0);

            // Act
            var result = await _service.SaveImageAsync(mockFile.Object);

            // Assert
            Assert.Equal(string.Empty, result);
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