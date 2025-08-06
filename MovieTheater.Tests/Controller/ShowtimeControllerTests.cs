using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using Moq;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MovieTheater.Tests.Controller
{
    public class ShowtimeControllerTests
    {
        private readonly Mock<IMovieRepository> _mockMovieRepository;
        private readonly ShowtimeController _controller;

        public ShowtimeControllerTests()
        {
            _mockMovieRepository = new Mock<IMovieRepository>();
            _controller = new ShowtimeController(_mockMovieRepository.Object);
        }

        [Fact]
        public void Constructor_WithValidRepository_CreatesInstance()
        {
            // Arrange & Act
            var controller = new ShowtimeController(_mockMovieRepository.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void List_ReturnsViewResult()
        {
            // Act
            var result = _controller.List();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Details_WithValidId_ReturnsViewResult()
        {
            // Arrange
            var id = 1;

            // Act
            var result = _controller.Details(id);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Create_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Create_Post_WithValidCollection_RedirectsToIndex()
        {
            // Arrange
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = _controller.Create(collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void Create_Post_WithException_ReturnsRedirect()
        {
            // Arrange
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            
            // Mock repository to throw exception
            _mockMovieRepository.Setup(r => r.GetMovieShow()).Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Create(collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public void Edit_Get_WithValidId_ReturnsViewResult()
        {
            // Arrange
            var id = 1;

            // Act
            var result = _controller.Edit(id);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Edit_Post_WithValidCollection_RedirectsToIndex()
        {
            // Arrange
            var id = 1;
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = _controller.Edit(id, collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void Edit_Post_WithException_ReturnsRedirect()
        {
            // Arrange
            var id = 1;
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            
            // Mock repository to throw exception
            _mockMovieRepository.Setup(r => r.GetMovieShow()).Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Edit(id, collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public void Delete_Get_WithValidId_ReturnsViewResult()
        {
            // Arrange
            var id = 1;

            // Act
            var result = _controller.Delete(id);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Delete_Post_WithValidCollection_RedirectsToIndex()
        {
            // Arrange
            var id = 1;
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = _controller.Delete(id, collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var redirectResult = result as RedirectToActionResult;
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void Delete_Post_WithException_ReturnsRedirect()
        {
            // Arrange
            var id = 1;
            var collection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            
            // Mock repository to throw exception
            _mockMovieRepository.Setup(r => r.GetMovieShow()).Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Delete(id, collection);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public void Select_WithNoAvailableDates_ReturnsEmptyModel()
        {
            // Arrange
            var date = "15/06/2024";
            var returnUrl = "/test";

            // Mock to return empty list
            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(new List<MovieShow>());

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.Equal("~/Views/Showtime/Select.cshtml", viewResult.ViewName);
            
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.NotNull(model);
            Assert.Empty(model.AvailableDates);
            Assert.Empty(model.Movies);
        }

        [Fact]
        public void Select_WithValidDate_ReturnsCorrectModel()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie",
                        LargeImage = "/images/test.jpg"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1, // Active
                        UnavailableEndDate = null
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            // Mock the repository to return the list
            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.Equal("~/Views/Showtime/Select.cshtml", viewResult.ViewName);
            
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.NotNull(model);
            Assert.Single(model.AvailableDates);
            Assert.Single(model.Movies);
            Assert.Equal(returnUrl, model.ReturnUrl);
        }

        [Fact]
        public void Select_WithInvalidDate_UsesToday()
        {
            // Arrange
            var date = "invalid-date";
            var returnUrl = "/test";
            var today = DateOnly.FromDateTime(DateTime.Today);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = today,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Equal(today, model.SelectedDate);
        }

        [Fact]
        public void Select_WithNullDate_UsesToday()
        {
            // Arrange
            string? date = null;
            var returnUrl = "/test";
            var today = DateOnly.FromDateTime(DateTime.Today);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = today,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Equal(today, model.SelectedDate);
        }

        [Fact]
        public void Select_WithDisabledCinemaRoom_ExcludesFromResults()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 3, // Disabled
                        UnavailableEndDate = DateTime.Today.AddDays(1) // Still unavailable
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Empty(model.AvailableDates);
            Assert.Empty(model.Movies);
        }

        [Fact]
        public void Select_WithMultipleVersions_GroupsCorrectly()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            // Create a single Movie object to ensure proper grouping
            var movie = new Movie
            {
                MovieId = "MV001",
                MovieNameEnglish = "Test Movie"
            };

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = movie,
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                },
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = movie,
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(16, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 2,
                        VersionName = "3D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Single(model.Movies);
            Assert.Equal(2, model.Movies[0].VersionShowtimes.Count);
        }

        [Fact]
        public void Select_WithNullScheduleTime_ExcludesFromResults()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = null
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Empty(model.Movies);
        }

        [Fact]
        public void Select_WithNullMovie_ExcludesFromResults()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = null,
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Empty(model.Movies);
        }

        [Fact]
        public void Select_WithNullMovieName_UsesDefaultName()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = null
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Single(model.Movies);
            Assert.Equal("Unknown", model.Movies[0].MovieName);
        }

        [Fact]
        public void Select_WithNullPosterUrl_UsesDefaultImage()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie",
                        LargeImage = null,
                        SmallImage = null
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Single(model.Movies);
            Assert.Equal("/images/default-movie.png", model.Movies[0].PosterUrl);
        }

        [Fact]
        public void Select_WithSmallImage_UsesSmallImage()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie",
                        LargeImage = null,
                        SmallImage = "/images/small.jpg"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Single(model.Movies);
            Assert.Equal("/images/small.jpg", model.Movies[0].PosterUrl);
        }

        [Fact]
        public void Select_WithLargeImage_UsesLargeImage()
        {
            // Arrange
            var tomorrow = DateTime.Today.AddDays(1);
            var date = tomorrow.ToString("dd/MM/yyyy");
            var returnUrl = "/test";
            var selectedDate = DateOnly.FromDateTime(tomorrow);

            var movieShows = new List<MovieShow>
            {
                new MovieShow
                {
                    ShowDate = selectedDate,
                    Movie = new Movie
                    {
                        MovieId = "MV001",
                        MovieNameEnglish = "Test Movie",
                        LargeImage = "/images/large.jpg",
                        SmallImage = "/images/small.jpg"
                    },
                    CinemaRoom = new CinemaRoom
                    {
                        StatusId = 1
                    },
                    Schedule = new Schedule
                    {
                        ScheduleTime = new TimeOnly(14, 0)
                    },
                    Version = new MovieTheater.Models.Version
                    {
                        VersionId = 1,
                        VersionName = "2D"
                    }
                }
            };

            _mockMovieRepository.Setup(r => r.GetMovieShow())
                .Returns(movieShows);

            // Act
            var result = _controller.Select(date, returnUrl);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            var model = viewResult.Model as ShowtimeSelectionViewModel;
            Assert.Single(model.Movies);
            Assert.Equal("/images/large.jpg", model.Movies[0].PosterUrl);
        }
    }
} 