using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class ScheduleRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly ScheduleRepository _repository;

        public ScheduleRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new ScheduleRepository(_context);
        }

        [Fact]
        public void GetAllScheduleTimes_ReturnsAllSchedulesOrderedByTime()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(10, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 30) },
                new Schedule { ScheduleId = 3, ScheduleTime = new TimeOnly(9, 0) }
            };
            _context.Schedules.AddRange(schedules);
            _context.SaveChanges();

            // Act
            var result = _repository.GetAllScheduleTimes();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(3, result[0].ScheduleId); // 9:00
            Assert.Equal(1, result[1].ScheduleId); // 10:00
            Assert.Equal(2, result[2].ScheduleId); // 14:30
        }

        [Fact]
        public void GetAllScheduleTimes_WhenNoSchedules_ReturnsEmptyList()
        {
            // Act
            var result = _repository.GetAllScheduleTimes();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithNoExistingShows_ReturnsAllSchedulesAfterOpeningTime()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(8, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 3, ScheduleTime = new TimeOnly(10, 0) }
            };
            _context.Schedules.AddRange(schedules);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Schedules.Count); // 9:00 and 10:00 (8:00 is before opening time 8:30)
            Assert.Equal(new TimeSpan(8, 30, 0), result.LastShowEndTime);
            Assert.False(result.HasExistingShows);
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithExistingShows_ReturnsSchedulesAfterLastShow()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) },
                new Schedule { ScheduleId = 3, ScheduleTime = new TimeOnly(16, 0) }
            };
            _context.Schedules.AddRange(schedules);

            var movie = new Movie { MovieId = "1", Duration = 120 }; // 2 hours
            _context.Movies.Add(movie);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 1,
                    ShowDate = new DateOnly(2024, 1, 1),
                    MovieId = "1",
                    ScheduleId = 1 // 9:00 show
                }
            };
            _context.MovieShows.AddRange(movieShows);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            // Last show ends at 9:00 + 2:15 = 11:15
            Assert.Equal(new TimeSpan(11, 15, 0), result.LastShowEndTime);
            Assert.True(result.HasExistingShows);
            // Should return schedules after 11:15 (14:00 and 16:00)
            Assert.Equal(2, result.Schedules.Count);
            Assert.Equal(2, result.Schedules[0].ScheduleId); // 14:00
            Assert.Equal(3, result.Schedules[1].ScheduleId); // 16:00
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithMultipleExistingShows_ReturnsSchedulesAfterLastShow()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) },
                new Schedule { ScheduleId = 3, ScheduleTime = new TimeOnly(16, 0) },
                new Schedule { ScheduleId = 4, ScheduleTime = new TimeOnly(18, 0) }
            };
            _context.Schedules.AddRange(schedules);

            var movie = new Movie { MovieId = "1", Duration = 90 }; // 1.5 hours
            _context.Movies.Add(movie);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 1,
                    ShowDate = new DateOnly(2024, 1, 1),
                    MovieId = "1",
                    ScheduleId = 1 // 9:00 show
                },
                new MovieShow
                {
                    MovieShowId = 2,
                    CinemaRoomId = 1,
                    ShowDate = new DateOnly(2024, 1, 1),
                    MovieId = "1",
                    ScheduleId = 2 // 14:00 show
                }
            };
            _context.MovieShows.AddRange(movieShows);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 90;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            // Last show ends at 14:00 + 1:45 = 15:45
            Assert.Equal(new TimeSpan(15, 45, 0), result.LastShowEndTime);
            Assert.True(result.HasExistingShows);
            // Should return schedules after 15:45 (16:00 and 18:00)
            Assert.Equal(2, result.Schedules.Count);
            Assert.Equal(3, result.Schedules[0].ScheduleId); // 16:00
            Assert.Equal(4, result.Schedules[1].ScheduleId); // 18:00
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithDifferentRoomId_ReturnsAllSchedules()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) }
            };
            _context.Schedules.AddRange(schedules);

            var movie = new Movie { MovieId = "1", Duration = 120 };
            _context.Movies.Add(movie);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 2, // Different room
                    ShowDate = new DateOnly(2024, 1, 1),
                    MovieId = "1",
                    ScheduleId = 1
                }
            };
            _context.MovieShows.AddRange(movieShows);
            _context.SaveChanges();

            var cinemaRoomId = 1; // Querying for room 1
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new TimeSpan(8, 30, 0), result.LastShowEndTime); // Default opening time
            Assert.False(result.HasExistingShows);
            Assert.Equal(2, result.Schedules.Count); // All schedules available
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithDifferentDate_ReturnsAllSchedules()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) }
            };
            _context.Schedules.AddRange(schedules);

            var movie = new Movie { MovieId = "1", Duration = 120 };
            _context.Movies.Add(movie);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 1,
                    ShowDate = new DateOnly(2024, 1, 2), // Different date
                    MovieId = "1",
                    ScheduleId = 1
                }
            };
            _context.MovieShows.AddRange(movieShows);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1); // Querying for different date
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new TimeSpan(8, 30, 0), result.LastShowEndTime); // Default opening time
            Assert.False(result.HasExistingShows);
            Assert.Equal(2, result.Schedules.Count); // All schedules available
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithNullScheduleTime_ExcludesNullSchedules()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = null }, // Null schedule time
                new Schedule { ScheduleId = 3, ScheduleTime = new TimeOnly(14, 0) }
            };
            _context.Schedules.AddRange(schedules);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Schedules.Count); // Only schedules with valid times
            Assert.Equal(1, result.Schedules[0].ScheduleId); // 9:00
            Assert.Equal(3, result.Schedules[1].ScheduleId); // 14:00
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithNullMovieDuration_ExcludesShowsWithNullDuration()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(9, 0) },
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(14, 0) }
            };
            _context.Schedules.AddRange(schedules);

            var movie = new Movie { MovieId = "1", Duration = null }; // Null duration
            _context.Movies.Add(movie);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    MovieShowId = 1,
                    CinemaRoomId = 1,
                    ShowDate = new DateOnly(2024, 1, 1),
                    MovieId = "1",
                    ScheduleId = 1
                }
            };
            _context.MovieShows.AddRange(movieShows);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new TimeSpan(8, 30, 0), result.LastShowEndTime); // Default opening time
            Assert.True(result.HasExistingShows); // Has shows but with null duration
            Assert.Equal(2, result.Schedules.Count); // All schedules available
        }

        [Fact]
        public void GetAvailableScheduleTimes_WithNoAvailableSchedules_ReturnsEmptyList()
        {
            // Arrange
            var schedules = new List<Schedule>
            {
                new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(7, 0) }, // Before opening time
                new Schedule { ScheduleId = 2, ScheduleTime = new TimeOnly(8, 0) }  // Before opening time
            };
            _context.Schedules.AddRange(schedules);
            _context.SaveChanges();

            var cinemaRoomId = 1;
            var showDate = new DateOnly(2024, 1, 1);
            var movieDurationMinutes = 120;
            var cleaningTimeMinutes = 15;

            // Act
            var result = _repository.GetAvailableScheduleTimes(cinemaRoomId, showDate, movieDurationMinutes, cleaningTimeMinutes);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Schedules);
            Assert.Equal(new TimeSpan(8, 30, 0), result.LastShowEndTime);
            Assert.False(result.HasExistingShows);
        }
    }
} 