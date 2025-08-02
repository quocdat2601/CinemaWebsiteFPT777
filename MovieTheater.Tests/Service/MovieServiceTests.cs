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

        [Fact]
        public void GetAll_WithValidData_ReturnsAllMovies()
        {
            // Arrange
            var expectedMovies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers", Duration = 120 },
                new Movie { MovieId = "2", MovieNameEnglish = "Batman", Duration = 110 }
            };

            _mockMovieRepository.Setup(x => x.GetAll()).Returns(expectedMovies);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            var movies = result.ToList();
            Assert.Equal(2, movies.Count);
            Assert.Equal("1", movies[0].MovieId);
            Assert.Equal("2", movies[1].MovieId);
            _mockMovieRepository.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAll_WithEmptyData_ReturnsEmptyList()
        {
            // Arrange
            var expectedMovies = new List<Movie>();
            _mockMovieRepository.Setup(x => x.GetAll()).Returns(expectedMovies);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.NotNull(result);
            var movies = result.ToList();
            Assert.Empty(movies);
            _mockMovieRepository.Verify(x => x.GetAll(), Times.Once);
        }

        [Fact]
        public void Save_CallsRepositorySave()
        {
            // Arrange
            // No setup needed

            // Act
            _service.Save();

            // Assert
            _mockMovieRepository.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public async Task GetSchedulesAsync_WithValidData_ReturnsSchedules()
        {
            // Arrange
            var expectedSchedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) }
            };

            _mockMovieRepository.Setup(x => x.GetSchedulesAsync()).ReturnsAsync(expectedSchedules);

            // Act
            var result = await _service.GetSchedulesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ScheduleId);
            Assert.Equal(2, result[1].ScheduleId);
            _mockMovieRepository.Verify(x => x.GetSchedulesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTypesAsync_WithValidData_ReturnsTypes()
        {
            // Arrange
            var expectedTypes = new List<Models.Type>
            {
                new Models.Type { TypeId = 1, TypeName = "Action" },
                new Models.Type { TypeId = 2, TypeName = "Drama" }
            };

            _mockMovieRepository.Setup(x => x.GetTypesAsync()).ReturnsAsync(expectedTypes);

            // Act
            var result = await _service.GetTypesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].TypeId);
            Assert.Equal(2, result[1].TypeId);
            _mockMovieRepository.Verify(x => x.GetTypesAsync(), Times.Once);
        }

        [Fact]
        public void GetMovieShow_WithValidData_ReturnsMovieShows()
        {
            // Arrange
            var expectedMovieShows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, MovieId = "1" },
                new MovieShow { MovieShowId = 2, MovieId = "2" }
            };

            _mockMovieRepository.Setup(x => x.GetMovieShow()).Returns(expectedMovieShows);

            // Act
            var result = _service.GetMovieShow();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].MovieShowId);
            Assert.Equal(2, result[1].MovieShowId);
            _mockMovieRepository.Verify(x => x.GetMovieShow(), Times.Once);
        }

        [Fact]
        public void GetMovieShowById_WithValidId_ReturnsMovieShow()
        {
            // Arrange
            var movieShowId = 1;
            var expectedMovieShow = new MovieShow { MovieShowId = movieShowId, MovieId = "1" };

            _mockMovieRepository.Setup(x => x.GetMovieShowById(movieShowId)).Returns(expectedMovieShow);

            // Act
            var result = _service.GetMovieShowById(movieShowId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(movieShowId, result.MovieShowId);
            _mockMovieRepository.Verify(x => x.GetMovieShowById(movieShowId), Times.Once);
        }

        [Fact]
        public void GetMovieShowById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var movieShowId = 999;
            _mockMovieRepository.Setup(x => x.GetMovieShowById(movieShowId)).Returns((MovieShow?)null);

            // Act
            var result = _service.GetMovieShowById(movieShowId);

            // Assert
            Assert.Null(result);
            _mockMovieRepository.Verify(x => x.GetMovieShowById(movieShowId), Times.Once);
        }



        [Fact]
        public void IsScheduleAvailable_WithAvailableSchedule_ReturnsTrue()
        {
            // Arrange
            var showDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var scheduleId = 1;
            var cinemaRoomId = 1;
            var movieDuration = 120;

            _mockMovieRepository.Setup(x => x.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration)).Returns(true);

            // Act
            var result = _service.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration), Times.Once);
        }

        [Fact]
        public void IsScheduleAvailable_WithUnavailableSchedule_ReturnsFalse()
        {
            // Arrange
            var showDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var scheduleId = 1;
            var cinemaRoomId = 1;
            var movieDuration = 120;

            _mockMovieRepository.Setup(x => x.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration)).Returns(false);

            // Act
            var result = _service.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration);

            // Assert
            Assert.False(result);
            _mockMovieRepository.Verify(x => x.IsScheduleAvailable(showDate, scheduleId, cinemaRoomId, movieDuration), Times.Once);
        }

        [Fact]
        public void AddMovieShows_WithValidData_ReturnsTrue()
        {
            // Arrange
            var movieShows = new List<MovieShow>
            {
                new MovieShow { MovieId = "1", ShowDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), ScheduleId = 1, CinemaRoomId = 1 },
                new MovieShow { MovieId = "2", ShowDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), ScheduleId = 2, CinemaRoomId = 1 }
            };

            var movie1 = new Movie { MovieId = "1", Duration = 120 };
            var movie2 = new Movie { MovieId = "2", Duration = 110 };

            _mockMovieRepository.Setup(x => x.GetById("1")).Returns(movie1);
            _mockMovieRepository.Setup(x => x.GetById("2")).Returns(movie2);
            _mockMovieRepository.Setup(x => x.IsScheduleAvailable(It.IsAny<DateOnly>(), 1, 1, 120)).Returns(true);
            _mockMovieRepository.Setup(x => x.IsScheduleAvailable(It.IsAny<DateOnly>(), 2, 1, 110)).Returns(true);
            _mockMovieRepository.Setup(x => x.AddMovieShows(movieShows));

            // Act
            var result = _service.AddMovieShows(movieShows);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.AddMovieShows(movieShows), Times.Once);
        }

        [Fact]
        public void AddMovieShows_WithEmptyList_ReturnsTrue()
        {
            // Arrange
            var movieShows = new List<MovieShow>();

            // Act
            var result = _service.AddMovieShows(movieShows);

            // Assert
            Assert.True(result);
            _mockMovieRepository.Verify(x => x.AddMovieShows(movieShows), Times.Once);
        }

        [Fact]
        public void AddMovieShows_WithNullList_ReturnsFalse()
        {
            // Arrange
            List<MovieShow>? movieShows = null;

            // Act
            var result = _service.AddMovieShows(movieShows);

            // Assert
            Assert.False(result);
            _mockMovieRepository.Verify(x => x.AddMovieShows(movieShows), Times.Never);
        }



        [Fact]
        public void GetAllVersions_WithValidData_ReturnsVersions()
        {
            // Arrange
            var expectedVersions = new List<Models.Version>
            {
                new Models.Version { VersionId = 1, VersionName = "2D" },
                new Models.Version { VersionId = 2, VersionName = "3D" }
            };

            _mockMovieRepository.Setup(x => x.GetAllVersions()).Returns(expectedVersions);

            // Act
            var result = _service.GetAllVersions();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].VersionId);
            Assert.Equal(2, result[1].VersionId);
            _mockMovieRepository.Verify(x => x.GetAllVersions(), Times.Once);
        }



        [Fact]
        public void GetAllCinemaRooms_WithValidData_ReturnsCinemaRooms()
        {
            // Arrange
            var expectedCinemaRooms = new List<CinemaRoom>
            {
                new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room A" },
                new CinemaRoom { CinemaRoomId = 2, CinemaRoomName = "Room B" }
            };

            _mockMovieRepository.Setup(x => x.GetAllCinemaRooms()).Returns(expectedCinemaRooms);

            // Act
            var result = _service.GetAllCinemaRooms();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].CinemaRoomId);
            Assert.Equal(2, result[1].CinemaRoomId);
            _mockMovieRepository.Verify(x => x.GetAllCinemaRooms(), Times.Once);
        }

        [Fact]
        public void GetSchedules_WithValidData_ReturnsSchedules()
        {
            // Arrange
            var expectedSchedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) }
            };

            _mockMovieRepository.Setup(x => x.GetSchedules()).Returns(expectedSchedules);

            // Act
            var result = _service.GetSchedules();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ScheduleId);
            Assert.Equal(2, result[1].ScheduleId);
            _mockMovieRepository.Verify(x => x.GetSchedules(), Times.Once);
        }

        [Fact]
        public void GetTypes_WithValidData_ReturnsTypes()
        {
            // Arrange
            var expectedTypes = new List<Models.Type>
            {
                new Models.Type { TypeId = 1, TypeName = "Action" },
                new Models.Type { TypeId = 2, TypeName = "Drama" }
            };

            _mockMovieRepository.Setup(x => x.GetTypes()).Returns(expectedTypes);

            // Act
            var result = _service.GetTypes();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].TypeId);
            Assert.Equal(2, result[1].TypeId);
            _mockMovieRepository.Verify(x => x.GetTypes(), Times.Once);
        }



        [Fact]
        public async Task GetAvailableSchedulesAsync_WithValidData_ReturnsSchedules()
        {
            // Arrange
            var showDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var cinemaRoomId = 1;
            var expectedSchedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) }
            };

            _mockMovieRepository.Setup(x => x.GetAvailableSchedulesAsync(showDate, cinemaRoomId)).ReturnsAsync(expectedSchedules);

            // Act
            var result = await _service.GetAvailableSchedulesAsync(showDate, cinemaRoomId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].ScheduleId);
            Assert.Equal(2, result[1].ScheduleId);
            _mockMovieRepository.Verify(x => x.GetAvailableSchedulesAsync(showDate, cinemaRoomId), Times.Once);
        }

        [Fact]
        public void GetShowDates_WithValidMovieId_ReturnsShowDates()
        {
            // Arrange
            var movieId = "1";
            var expectedShowDates = new List<DateOnly>
            {
                DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                DateOnly.FromDateTime(DateTime.Now.AddDays(2))
            };

            _mockMovieRepository.Setup(x => x.GetShowDates(movieId)).Returns(expectedShowDates);

            // Act
            var result = _service.GetShowDates(movieId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockMovieRepository.Verify(x => x.GetShowDates(movieId), Times.Once);
        }

        [Fact]
        public void GetMovieShowsByRoomAndDate_WithValidData_ReturnsMovieShows()
        {
            // Arrange
            var cinemaRoomId = 1;
            var showDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var expectedMovieShows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, CinemaRoomId = cinemaRoomId },
                new MovieShow { MovieShowId = 2, CinemaRoomId = cinemaRoomId }
            };

            _mockMovieRepository.Setup(x => x.GetMovieShowsByRoomAndDate(cinemaRoomId, showDate)).Returns(expectedMovieShows);

            // Act
            var result = _service.GetMovieShowsByRoomAndDate(cinemaRoomId, showDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, ms => Assert.Equal(cinemaRoomId, ms.CinemaRoomId));
            _mockMovieRepository.Verify(x => x.GetMovieShowsByRoomAndDate(cinemaRoomId, showDate), Times.Once);
        }

        [Fact]
        public void GetMovieShowsByMovieId_WithValidMovieId_ReturnsMovieShows()
        {
            // Arrange
            var movieId = "1";
            var expectedMovieShows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, MovieId = movieId },
                new MovieShow { MovieShowId = 2, MovieId = movieId }
            };

            _mockMovieRepository.Setup(x => x.GetMovieShowsByMovieId(movieId)).Returns(expectedMovieShows);

            // Act
            var result = _service.GetMovieShowsByMovieId(movieId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, ms => Assert.Equal(movieId, ms.MovieId));
            _mockMovieRepository.Verify(x => x.GetMovieShowsByMovieId(movieId), Times.Once);
        }

        [Fact]
        public void GetVersionById_WithValidVersionId_ReturnsVersion()
        {
            // Arrange
            var versionId = 1;
            var expectedVersion = new Models.Version { VersionId = versionId, VersionName = "2D" };

            _mockMovieRepository.Setup(x => x.GetVersionById(versionId)).Returns(expectedVersion);

            // Act
            var result = _service.GetVersionById(versionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(versionId, result.VersionId);
            _mockMovieRepository.Verify(x => x.GetVersionById(versionId), Times.Once);
        }

        [Fact]
        public void GetVersionById_WithInvalidVersionId_ReturnsNull()
        {
            // Arrange
            var versionId = 999;
            _mockMovieRepository.Setup(x => x.GetVersionById(versionId)).Returns((Models.Version?)null);

            // Act
            var result = _service.GetVersionById(versionId);

            // Assert
            Assert.Null(result);
            _mockMovieRepository.Verify(x => x.GetVersionById(versionId), Times.Once);
        }

        [Fact]
        public void GetCurrentlyShowingMovies_WithValidData_ReturnsMovies()
        {
            // Arrange
            var expectedMovies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)) },
                new Movie { MovieId = "2", MovieNameEnglish = "Batman", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)) }
            };

            _mockMovieRepository.Setup(x => x.GetCurrentlyShowingMovies()).Returns(expectedMovies);

            // Act
            var result = _service.GetCurrentlyShowingMovies();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].MovieId);
            Assert.Equal("2", result[1].MovieId);
            _mockMovieRepository.Verify(x => x.GetCurrentlyShowingMovies(), Times.Once);
        }

        [Fact]
        public void GetComingSoonMovies_WithValidData_ReturnsMovies()
        {
            // Arrange
            var expectedMovies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers 2", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)) },
                new Movie { MovieId = "2", MovieNameEnglish = "Batman 2", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(15)) }
            };

            _mockMovieRepository.Setup(x => x.GetComingSoonMovies()).Returns(expectedMovies);

            // Act
            var result = _service.GetComingSoonMovies();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].MovieId);
            Assert.Equal("2", result[1].MovieId);
            _mockMovieRepository.Verify(x => x.GetComingSoonMovies(), Times.Once);
        }

        [Fact]
        public void GetCurrentlyShowingMoviesWithDetails_WithValidData_ReturnsMovies()
        {
            // Arrange
            var expectedMovies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)) },
                new Movie { MovieId = "2", MovieNameEnglish = "Batman", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)) }
            };

            _mockMovieRepository.Setup(x => x.GetCurrentlyShowingMoviesWithDetails()).Returns(expectedMovies);

            // Act
            var result = _service.GetCurrentlyShowingMoviesWithDetails();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].MovieId);
            Assert.Equal("2", result[1].MovieId);
            _mockMovieRepository.Verify(x => x.GetCurrentlyShowingMoviesWithDetails(), Times.Once);
        }

        [Fact]
        public void GetComingSoonMoviesWithDetails_WithValidData_ReturnsMovies()
        {
            // Arrange
            var expectedMovies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Avengers 2", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10)) },
                new Movie { MovieId = "2", MovieNameEnglish = "Batman 2", FromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(15)) }
            };

            _mockMovieRepository.Setup(x => x.GetComingSoonMoviesWithDetails()).Returns(expectedMovies);

            // Act
            var result = _service.GetComingSoonMoviesWithDetails();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result[0].MovieId);
            Assert.Equal("2", result[1].MovieId);
            _mockMovieRepository.Verify(x => x.GetComingSoonMoviesWithDetails(), Times.Once);
        }
    }
} 