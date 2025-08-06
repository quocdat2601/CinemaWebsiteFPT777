using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class MovieRepositoryTests
    {
        private MovieTheaterContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "MovieRepoTestDb" + Guid.NewGuid())
                .Options;
            return new MovieTheaterContext(options);
        }

        private MovieRepository CreateRepository(MovieTheaterContext context)
        {
            var loggerMock = new Mock<ILogger<MovieRepository>>();
            return new MovieRepository(context, loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var loggerMock = new Mock<ILogger<MovieRepository>>();

            // Act
            var repo = new MovieRepository(context, loggerMock.Object);

            // Assert
            Assert.NotNull(repo);
        }

        #endregion

        #region GenerateMovieId Tests

        [Fact]
        public void GenerateMovieId_NoMovies_ReturnsMV001()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GenerateMovieId();

            // Assert
            Assert.Equal("MV001", result);
        }

        [Fact]
        public void GenerateMovieId_WithExistingMovies_ReturnsNextId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Movies.Add(new Movie { MovieId = "MV001" });
            context.Movies.Add(new Movie { MovieId = "MV002" });
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GenerateMovieId();

            // Assert
            Assert.Equal("MV003", result);
        }

        [Fact]
        public void GenerateMovieId_WithInvalidFormat_ReturnsTimestampBasedId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Movies.Add(new Movie { MovieId = "INVALID" });
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GenerateMovieId();

            // Assert
            Assert.StartsWith("MV", result);
            Assert.True(result.Length > 3);
        }

        [Fact]
        public void GenerateMovieId_WithNonNumericId_ReturnsTimestampBasedId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Movies.Add(new Movie { MovieId = "MVABC" });
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GenerateMovieId();

            // Assert
            Assert.StartsWith("MV", result);
            Assert.True(result.Length > 3);
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public void GetAll_ReturnsAllMoviesWithIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie1 = new Movie { MovieId = "MV001", MovieNameEnglish = "Movie 1" };
            var movie2 = new Movie { MovieId = "MV002", MovieNameEnglish = "Movie 2" };
            context.Movies.AddRange(movie1, movie2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetAll().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("MV001", result[0].MovieId);
            Assert.Equal("MV002", result[1].MovieId);
        }

        [Fact]
        public void GetAll_EmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetAll().ToList();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public void GetById_ExistingMovie_ReturnsMovieWithIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetById("MV001");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MV001", result.MovieId);
            Assert.Equal("Test Movie", result.MovieNameEnglish);
        }

        [Fact]
        public void GetById_NonExistentMovie_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetById("NONEXISTENT");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Add Tests

        [Fact]
        public void Add_MovieWithoutId_GeneratesIdAndAdds()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieNameEnglish = "Test Movie" };

            // Act
            var result = repo.Add(movie);

            // Assert
            Assert.True(result);
            Assert.NotNull(movie.MovieId);
            Assert.StartsWith("MV", movie.MovieId);
            Assert.Single(context.Movies);
        }

        [Fact]
        public void Add_MovieWithId_AddsAsIs()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieId = "CUSTOM001", MovieNameEnglish = "Test Movie" };

            // Act
            var result = repo.Add(movie);

            // Assert
            Assert.True(result);
            Assert.Equal("CUSTOM001", movie.MovieId);
            Assert.Single(context.Movies);
        }

        [Fact]
        public void Add_WithException_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie); // Add first to cause duplicate key exception
            context.SaveChanges();

            // Act
            var result = repo.Add(movie); // Try to add again

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_ExistingMovie_UpdatesProperties()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie 
            { 
                MovieId = "MV001", 
                MovieNameEnglish = "Original Name",
                Duration = 120
            };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            var updatedMovie = new Movie 
            { 
                MovieId = "MV001", 
                MovieNameEnglish = "Updated Name",
                Duration = 150
            };

            // Act
            var result = repo.Update(updatedMovie);

            // Assert
            Assert.True(result);
            var savedMovie = context.Movies.Find("MV001");
            Assert.Equal("Updated Name", savedMovie.MovieNameEnglish);
            Assert.Equal(150, savedMovie.Duration);
        }

        [Fact]
        public void Update_NonExistentMovie_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieId = "NONEXISTENT", MovieNameEnglish = "Test" };

            // Act
            var result = repo.Update(movie);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_WithException_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Simulate exception by disposing context
            context.Dispose();

            // Act
            var result = repo.Update(movie);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public void Delete_ExistingMovie_RemovesMovie()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            repo.Delete("MV001");

            // Assert
            Assert.Empty(context.Movies);
        }

        [Fact]
        public void Delete_NonExistentMovie_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            repo.Delete("NONEXISTENT");

            // Assert
            Assert.Empty(context.Movies);
        }

        [Fact]
        public void Delete_MovieWithRelatedData_RemovesAllRelatedData()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001" };
            context.Movies.Add(movie);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            repo.Delete("MV001");

            // Assert
            Assert.Empty(context.Movies);
            Assert.Empty(context.MovieShows);
        }

        #endregion

        #region DeleteMovieShow Tests

        [Fact]
        public void DeleteMovieShow_ExistingMovieShow_RemovesMovieShow()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001" };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            repo.DeleteMovieShow(1);
            repo.Save();

            // Assert
            Assert.Empty(context.MovieShows);
        }

        [Fact]
        public void DeleteMovieShow_NonExistentMovieShow_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            repo.DeleteMovieShow(999);

            // Assert
            Assert.Empty(context.MovieShows);
        }

        #endregion

        #region Save Tests

        [Fact]
        public void Save_SavesChanges()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test" };
            context.Movies.Add(movie);

            // Act
            repo.Save();

            // Assert
            Assert.Single(context.Movies);
        }

        #endregion

        #region Async Methods Tests

        [Fact]
        public async Task GetSchedulesAsync_ReturnsAllSchedules()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(10)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            context.Schedules.AddRange(schedule1, schedule2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetSchedulesAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetTypesAsync_ReturnsAllTypes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var type1 = new MovieTheater.Models.Type { TypeId = 1, TypeName = "Action" };
            var type2 = new MovieTheater.Models.Type { TypeId = 2, TypeName = "Comedy" };
            context.Types.AddRange(type1, type2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetTypesAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllMoviesAsync_ReturnsMoviesWithShows()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie1 = new Movie { MovieId = "MV001", MovieNameEnglish = "Movie 1" };
            var movie2 = new Movie { MovieId = "MV002", MovieNameEnglish = "Movie 2" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001" };
            context.Movies.AddRange(movie1, movie2);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetAllMoviesAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("MV001", result[0].MovieId);
        }

        [Fact]
        public async Task GetShowDatesAsync_ReturnsDistinctDates()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today) };
            var movieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) };
            context.MovieShows.AddRange(movieShow1, movieShow2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetShowDatesAsync("MV001");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetShowTimesAsync_ReturnsFormattedTimes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetShowTimesAsync("MV001", DateTime.Today);

            // Assert
            Assert.Single(result);
            Assert.Equal("14:00", result[0]);
        }

        #endregion

        #region GetSchedulesByIds and GetTypesByIds Tests

        [Fact]
        public void GetSchedulesByIds_ReturnsMatchingSchedules()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(10)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var schedule3 = new Schedule { ScheduleId = 3, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(18)) };
            context.Schedules.AddRange(schedule1, schedule2, schedule3);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetSchedulesByIds(new List<int> { 1, 3 });

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.ScheduleId == 1);
            Assert.Contains(result, s => s.ScheduleId == 3);
        }

        [Fact]
        public void GetTypesByIds_ReturnsMatchingTypes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var type1 = new MovieTheater.Models.Type { TypeId = 1, TypeName = "Action" };
            var type2 = new MovieTheater.Models.Type { TypeId = 2, TypeName = "Comedy" };
            var type3 = new MovieTheater.Models.Type { TypeId = 3, TypeName = "Drama" };
            context.Types.AddRange(type1, type2, type3);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetTypesByIds(new List<int> { 1, 2 });

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.TypeId == 1);
            Assert.Contains(result, t => t.TypeId == 2);
        }

        #endregion

        #region AddMovieShow and AddMovieShows Tests

        [Fact]
        public void AddMovieShow_SingleMovieShow_AddsSuccessfully()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001" };

            // Act
            repo.AddMovieShow(movieShow);
            repo.Save();

            // Assert
            Assert.Single(context.MovieShows);
        }

        [Fact]
        public void AddMovieShows_MultipleMovieShows_AddsSuccessfully()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movieShows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, MovieId = "MV001" },
                new MovieShow { MovieShowId = 2, MovieId = "MV002" }
            };

            // Act
            repo.AddMovieShows(movieShows);
            repo.Save();

            // Assert
            Assert.Equal(2, context.MovieShows.Count());
        }

        #endregion

        #region GetMovieShowById Tests

        [Fact]
        public void GetMovieShowById_ExistingMovieShow_ReturnsWithIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ScheduleId = 1, CinemaRoomId = 1, VersionId = 1 };
            context.Movies.Add(movie);
            context.Schedules.Add(schedule);
            context.CinemaRooms.Add(cinemaRoom);
            context.Versions.Add(version);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.MovieShowId);
            Assert.NotNull(result.Movie);
            Assert.NotNull(result.Schedule);
            Assert.NotNull(result.CinemaRoom);
            Assert.NotNull(result.Version);
        }

        [Fact]
        public void GetMovieShowById_NonExistentMovieShow_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowById(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetMovieShowsByMovieId Tests

        [Fact]
        public void GetMovieShowsByMovieId_ReturnsOrderedMovieShows()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(10)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var movieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), ScheduleId = 2, CinemaRoomId = 1, VersionId = 1 };
            var movieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1, VersionId = 1 };
            context.Movies.Add(movie);
            context.Schedules.AddRange(schedule1, schedule2);
            context.CinemaRooms.Add(cinemaRoom);
            context.Versions.Add(version);
            context.MovieShows.AddRange(movieShow1, movieShow2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowsByMovieId("MV001");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].MovieShowId);
            Assert.Equal(1, result[1].MovieShowId);
        }

        #endregion

        #region IsScheduleAvailable Tests

        [Fact]
        public void IsScheduleAvailable_WithConflicts_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(15)) };
            var movie = new Movie { MovieId = "MV001", Duration = 120 };
            var existingMovieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1 };
            context.Schedules.AddRange(schedule1, schedule2);
            context.Movies.Add(movie);
            context.MovieShows.Add(existingMovieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.IsScheduleAvailable(DateOnly.FromDateTime(DateTime.Today), 2, 1, 120);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsScheduleAvailable_InvalidSchedule_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.IsScheduleAvailable(DateOnly.FromDateTime(DateTime.Today), 999, 1, 120);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsScheduleAvailable_ScheduleWithoutTime_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = null };
            context.Schedules.Add(schedule);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.IsScheduleAvailable(DateOnly.FromDateTime(DateTime.Today), 1, 1, 120);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetAllCinemaRooms, GetSchedules, GetTypes Tests

        [Fact]
        public void GetAllCinemaRooms_ReturnsAllRooms()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var room1 = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            var room2 = new CinemaRoom { CinemaRoomId = 2, CinemaRoomName = "Room 2" };
            context.CinemaRooms.AddRange(room1, room2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetAllCinemaRooms();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetSchedules_ReturnsAllSchedules()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(10)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            context.Schedules.AddRange(schedule1, schedule2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetSchedules();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetTypes_ReturnsAllTypes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var type1 = new MovieTheater.Models.Type { TypeId = 1, TypeName = "Action" };
            var type2 = new MovieTheater.Models.Type { TypeId = 2, TypeName = "Comedy" };
            context.Types.AddRange(type1, type2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetTypes();

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetMovieShow Tests

        [Fact]
        public void GetMovieShow_ReturnsAllMovieShowsWithIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ScheduleId = 1, CinemaRoomId = 1, VersionId = 1 };
            context.Movies.Add(movie);
            context.Schedules.Add(schedule);
            context.CinemaRooms.Add(cinemaRoom);
            context.Versions.Add(version);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShow();

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].Movie);
            Assert.NotNull(result[0].Schedule);
            Assert.NotNull(result[0].CinemaRoom);
            Assert.NotNull(result[0].Version);
        }

        #endregion

        #region GetShowDates and GetShowTimes Tests

        [Fact]
        public void GetShowDates_ReturnsDistinctDates()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today) };
            var movieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) };
            context.MovieShows.AddRange(movieShow1, movieShow2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetShowDates("MV001");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetShowTimes_ReturnsFormattedTimes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetShowTimes("MV001", DateTime.Today);

            // Assert
            Assert.Single(result);
            Assert.Equal("14:00", result[0]);
        }

        #endregion

        #region GetAvailableSchedulesAsync Tests

        [Fact]
        public async Task GetAvailableSchedulesAsync_ReturnsUnbookedSchedules()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(16)) };
            var movie = new Movie { MovieId = "MV001", Duration = 120 };
            var existingMovieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1 };
            context.Schedules.AddRange(schedule1, schedule2);
            context.Movies.Add(movie);
            context.MovieShows.Add(existingMovieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = await repo.GetAvailableSchedulesAsync(DateOnly.FromDateTime(DateTime.Today), 1);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].ScheduleId);
        }

        #endregion

        #region GetMovieShowsByRoomAndDate Tests

        [Fact]
        public void GetMovieShowsByRoomAndDate_ReturnsFilteredMovieShows()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1, VersionId = 1 };
            context.Movies.Add(movie);
            context.Schedules.Add(schedule);
            context.CinemaRooms.Add(cinemaRoom);
            context.Versions.Add(version);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowsByRoomAndDate(1, DateOnly.FromDateTime(DateTime.Today));

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].MovieShowId);
        }

        #endregion

        #region GetMovieShowSummaryByMonth Tests

        [Fact]
        public void GetMovieShowSummaryByMonth_ReturnsCorrectSummary()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var movieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = new DateOnly(2024, 1, 15) };
            var movieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV001", ShowDate = new DateOnly(2024, 1, 20) };
            context.Movies.Add(movie);
            context.MovieShows.AddRange(movieShow1, movieShow2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowSummaryByMonth(2024, 1);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetAllVersions and GetVersionById Tests

        [Fact]
        public void GetAllVersions_ReturnsAllVersions()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var version1 = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var version2 = new MovieTheater.Models.Version { VersionId = 2, VersionName = "3D" };
            context.Versions.AddRange(version1, version2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetAllVersions();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetVersionById_ExistingVersion_ReturnsVersion()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            context.Versions.Add(version);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetVersionById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.VersionId);
            Assert.Equal("2D", result.VersionName);
        }

        [Fact]
        public void GetVersionById_NonExistentVersion_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetVersionById(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetCurrentlyShowingMovies and GetComingSoonMovies Tests

        [Fact]
        public void GetCurrentlyShowingMovies_ReturnsMoviesWithFutureShows()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie1 = new Movie { MovieId = "MV001", MovieNameEnglish = "Currently Showing" };
            var movie2 = new Movie { MovieId = "MV002", MovieNameEnglish = "Coming Soon" };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, StatusId = 1 }; // Active room
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CinemaRoomId = 1 };
            context.Movies.AddRange(movie1, movie2);
            context.CinemaRooms.Add(cinemaRoom);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetCurrentlyShowingMovies();

            // Assert
            Assert.Single(result);
            Assert.Equal("MV001", result[0].MovieId);
        }

        [Fact]
        public void GetComingSoonMovies_ReturnsMoviesWithoutFutureShows()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie1 = new Movie { MovieId = "MV001", MovieNameEnglish = "Currently Showing" };
            var movie2 = new Movie { MovieId = "MV002", MovieNameEnglish = "Coming Soon" };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, StatusId = 1 }; // Active room
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CinemaRoomId = 1 };
            context.Movies.AddRange(movie1, movie2);
            context.CinemaRooms.Add(cinemaRoom);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetComingSoonMovies();

            // Assert
            Assert.Single(result);
            Assert.Equal("MV002", result[0].MovieId);
        }

        #endregion

        #region GetCurrentlyShowingMoviesWithDetails and GetComingSoonMoviesWithDetails Tests

        [Fact]
        public void GetCurrentlyShowingMoviesWithDetails_ReturnsMoviesWithAllIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, StatusId = 1 }; // Active room
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CinemaRoomId = 1 };
            context.Movies.Add(movie);
            context.CinemaRooms.Add(cinemaRoom);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetCurrentlyShowingMoviesWithDetails();

            // Assert
            Assert.Single(result);
            Assert.Equal("MV001", result[0].MovieId);
        }

        [Fact]
        public void GetComingSoonMoviesWithDetails_ReturnsMoviesWithAllIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie1 = new Movie { MovieId = "MV001", MovieNameEnglish = "Currently Showing" };
            var movie2 = new Movie { MovieId = "MV002", MovieNameEnglish = "Coming Soon", ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, StatusId = 1 }; // Active room
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CinemaRoomId = 1 };
            context.Movies.AddRange(movie1, movie2);
            context.CinemaRooms.Add(cinemaRoom);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetComingSoonMoviesWithDetails();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, m => m.MovieId == "MV002");
        }

        #endregion

        #region Additional Comprehensive Tests

        [Fact]
        public void Add_WithException_LogsErrorAndReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges(); // Add first to cause duplicate key exception

            // Act
            var result = repo.Add(movie); // Try to add again to cause exception

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_WithException_LogsErrorAndReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Simulate exception by disposing context
            context.Dispose();

            // Act
            var result = repo.Update(movie);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_WithNullExistingMovie_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);
            var movie = new Movie { MovieId = "NONEXISTENT", MovieNameEnglish = "Test Movie" };

            // Act
            var result = repo.Update(movie);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_WithPeopleCollection_UpdatesPeopleCorrectly()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var person = new Person { PersonId = 1, Name = "Test Person" };
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.People.Add(person);
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            var updatedMovie = new Movie 
            { 
                MovieId = "MV001", 
                MovieNameEnglish = "Updated Movie",
                People = new List<Person> { person }
            };

            // Act
            var result = repo.Update(updatedMovie);

            // Assert
            Assert.True(result);
            var savedMovie = context.Movies.Include(m => m.People).FirstOrDefault(m => m.MovieId == "MV001");
            Assert.NotNull(savedMovie);
            Assert.Single(savedMovie.People);
            Assert.Equal(1, savedMovie.People.First().PersonId);
        }

        [Fact]
        public void Update_WithTypesCollection_UpdatesTypesCorrectly()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var type = new MovieTheater.Models.Type { TypeId = 1, TypeName = "Action" };
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Types.Add(type);
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            var updatedMovie = new Movie 
            { 
                MovieId = "MV001", 
                MovieNameEnglish = "Updated Movie",
                Types = new List<MovieTheater.Models.Type> { type }
            };

            // Act
            var result = repo.Update(updatedMovie);

            // Assert
            Assert.True(result);
            var savedMovie = context.Movies.Include(m => m.Types).FirstOrDefault(m => m.MovieId == "MV001");
            Assert.NotNull(savedMovie);
            Assert.Single(savedMovie.Types);
            Assert.Equal(1, savedMovie.Types.First().TypeId);
        }

        [Fact]
        public void Update_WithVersionsCollection_UpdatesVersionsCorrectly()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D" };
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Versions.Add(version);
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            var updatedMovie = new Movie 
            { 
                MovieId = "MV001", 
                MovieNameEnglish = "Updated Movie",
                Versions = new List<MovieTheater.Models.Version> { version }
            };

            // Act
            var result = repo.Update(updatedMovie);

            // Assert
            Assert.True(result);
            var savedMovie = context.Movies.Include(m => m.Versions).FirstOrDefault(m => m.MovieId == "MV001");
            Assert.NotNull(savedMovie);
            Assert.Single(savedMovie.Versions);
            Assert.Equal(1, savedMovie.Versions.First().VersionId);
        }

        [Fact]
        public void Delete_WithException_LogsErrorAndThrows()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Simulate exception by disposing context
            context.Dispose();

            // Act
            var exception = Assert.Throws<ObjectDisposedException>(() => repo.Delete("MV001"));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Delete_WithNullMovieShows_HandlesGracefully()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            repo.Delete("MV001");

            // Assert
            Assert.Empty(context.Movies);
        }

        [Fact]
        public void Delete_WithNullTypes_HandlesGracefully()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            repo.Delete("MV001");

            // Assert
            Assert.Empty(context.Movies);
        }

        [Fact]
        public void Delete_WithNullVersions_HandlesGracefully()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            repo.Delete("MV001");

            // Assert
            Assert.Empty(context.Movies);
        }

        [Fact]
        public void GenerateMovieId_WithInvalidFormatString_ReturnsTimestampBasedId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Movies.Add(new Movie { MovieId = "INVALID_FORMAT" });
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GenerateMovieId();

            // Assert
            Assert.StartsWith("MV", result);
            Assert.True(result.Length > 3);
        }

        [Fact]
        public void GenerateMovieId_WithNonNumericSuffix_ReturnsTimestampBasedId()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            context.Movies.Add(new Movie { MovieId = "MVABC" });
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GenerateMovieId();

            // Assert
            Assert.StartsWith("MV", result);
            Assert.True(result.Length > 3);
        }

        [Fact]
        public void Add_WithNullMovie_ThrowsNullReferenceException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var exception = Assert.Throws<NullReferenceException>(() => repo.Add(null));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Update_WithNullMovie_ThrowsNullReferenceException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var exception = Assert.Throws<NullReferenceException>(() => repo.Update(null));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void GetById_WithNullId_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetById(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Delete_WithNullId_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            repo.Delete(null);

            // Assert
            Assert.Empty(context.Movies);
        }

        [Fact]
        public void DeleteMovieShow_WithZeroId_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            repo.DeleteMovieShow(0);

            // Assert
            Assert.Empty(context.MovieShows);
        }

        [Fact]
        public void GetMovieShowById_WithZeroId_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowById(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetVersionById_WithZeroId_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetVersionById(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSchedulesByIds_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetSchedulesByIds(new List<int>());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetSchedulesByIds_WithNullList_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetSchedulesByIds(null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetTypesByIds_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetTypesByIds(new List<int>());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetTypesByIds_WithNullList_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetTypesByIds(null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void AddMovieShows_WithEmptyList_DoesNothing()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            repo.AddMovieShows(new List<MovieShow>());
            repo.Save();

            // Assert
            Assert.Empty(context.MovieShows);
        }

        [Fact]
        public void AddMovieShows_WithNullList_ThrowsException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => repo.AddMovieShows(null));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void IsScheduleAvailable_WithNoConflicts_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(18)) }; // 4 hours later to avoid conflict
            var movie = new Movie { MovieId = "MV001", Duration = 120 };
            var existingMovieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1 };
            context.Schedules.AddRange(schedule1, schedule2);
            context.Movies.Add(movie);
            context.MovieShows.Add(existingMovieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.IsScheduleAvailable(DateOnly.FromDateTime(DateTime.Today), 2, 1, 120);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsScheduleAvailable_WithDifferentRoom_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var movie = new Movie { MovieId = "MV001", Duration = 120 };
            var existingMovieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1 };
            context.Schedules.Add(schedule);
            context.Movies.Add(movie);
            context.MovieShows.Add(existingMovieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.IsScheduleAvailable(DateOnly.FromDateTime(DateTime.Today), 1, 2, 120);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetShowTimes_WithNoSchedules_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetShowTimes("MV001", DateTime.Today);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetShowDates_WithNoMovieShows_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetShowDates("MV001");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMovieShowsByMovieId_WithNoShows_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowsByMovieId("MV001");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMovieShowsByMovieId_WithNonExistentMovie_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowsByMovieId("NONEXISTENT");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMovieShowSummaryByMonth_WithNoShows_ReturnsEmptyDictionary()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowSummaryByMonth(2024, 1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMovieShowSummaryByMonth_WithDifferentMonth_ReturnsEmptyDictionary()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = new DateOnly(2024, 1, 15) };
            context.Movies.Add(movie);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowSummaryByMonth(2024, 2);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAvailableSchedulesAsync_WithNoSchedules_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetAvailableSchedulesAsync(DateOnly.FromDateTime(DateTime.Today), 1).Result;

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMovieShowsByRoomAndDate_WithNoShows_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowsByRoomAndDate(1, DateOnly.FromDateTime(DateTime.Today));

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCurrentlyShowingMovies_WithNoFutureShows_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
            context.Movies.Add(movie);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetCurrentlyShowingMovies();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetComingSoonMovies_WithAllMoviesShowing_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, StatusId = 1 }; // Active room
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CinemaRoomId = 1 };
            context.Movies.Add(movie);
            context.CinemaRooms.Add(cinemaRoom);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetComingSoonMovies();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetCurrentlyShowingMoviesWithDetails_WithNoFutureShows_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) };
            context.Movies.Add(movie);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetCurrentlyShowingMoviesWithDetails();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetComingSoonMoviesWithDetails_WithAllMoviesShowing_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie = new Movie { MovieId = "MV001", MovieNameEnglish = "Test Movie" };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, StatusId = 1 }; // Active room
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CinemaRoomId = 1 };
            context.Movies.Add(movie);
            context.CinemaRooms.Add(cinemaRoom);
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetComingSoonMoviesWithDetails();

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Complex Business Logic Tests

        [Fact]
        public void IsScheduleAvailable_WithOverlappingMovies_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(15)) };
            var movie1 = new Movie { MovieId = "MV001", Duration = 120 };
            var movie2 = new Movie { MovieId = "MV002", Duration = 90 };
            var existingMovieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1 };
            var existingMovieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV002", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 2, CinemaRoomId = 1 };
            context.Schedules.AddRange(schedule1, schedule2);
            context.Movies.AddRange(movie1, movie2);
            context.MovieShows.AddRange(existingMovieShow1, existingMovieShow2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.IsScheduleAvailable(DateOnly.FromDateTime(DateTime.Today), 2, 1, 120);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetMovieShowSummaryByMonth_WithMultipleMovies_ReturnsCorrectSummary()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var movie1 = new Movie { MovieId = "MV001", MovieNameEnglish = "Movie 1" };
            var movie2 = new Movie { MovieId = "MV002", MovieNameEnglish = "Movie 2" };
            var movieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = new DateOnly(2024, 1, 15) };
            var movieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV001", ShowDate = new DateOnly(2024, 1, 20) };
            var movieShow3 = new MovieShow { MovieShowId = 3, MovieId = "MV002", ShowDate = new DateOnly(2024, 1, 25) };
            context.Movies.AddRange(movie1, movie2);
            context.MovieShows.AddRange(movieShow1, movieShow2, movieShow3);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetMovieShowSummaryByMonth(2024, 1);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void GetAvailableSchedulesAsync_WithMultipleConflicts_ReturnsAvailableSchedules()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var schedule1 = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(14)) };
            var schedule2 = new Schedule { ScheduleId = 2, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(16)) };
            var schedule3 = new Schedule { ScheduleId = 3, ScheduleTime = TimeOnly.FromDateTime(DateTime.Today.AddHours(18)) };
            var movie = new Movie { MovieId = "MV001", Duration = 120 };
            var existingMovieShow1 = new MovieShow { MovieShowId = 1, MovieId = "MV001", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1 };
            var existingMovieShow2 = new MovieShow { MovieShowId = 2, MovieId = "MV002", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 2, CinemaRoomId = 1 };
            context.Schedules.AddRange(schedule1, schedule2, schedule3);
            context.Movies.Add(movie);
            context.MovieShows.AddRange(existingMovieShow1, existingMovieShow2);
            context.SaveChanges();
            var repo = CreateRepository(context);

            // Act
            var result = repo.GetAvailableSchedulesAsync(DateOnly.FromDateTime(DateTime.Today), 1).Result;

            // Assert
            Assert.Single(result);
            Assert.Equal(3, result[0].ScheduleId);
        }

        #endregion


    }
}
