using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Service;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MovieTheater.Models;

namespace MovieTheater.Tests.Controller
{
    public class MovieControllerTests
    {
        private MovieController CreateController(Mock<IMovieService> mockMovieService = null)
        {
            mockMovieService ??= new Mock<IMovieService>();
            return new MovieController(mockMovieService.Object, null, null, null);
        }

        [Fact]
        public void MovieList_SearchByName_ReturnsFilteredAndSortedMovies()
        {
            // Arrange
            var mockMovieService = new Mock<IMovieService>();
            var movies = new List<Movie>
            {
                new Movie { MovieNameEnglish = "Avatar" },
                new Movie { MovieNameEnglish = "Batman" },
                new Movie { MovieNameEnglish = "Avengers" }
            };
            mockMovieService.Setup(s => s.GetAll()).Returns(movies);
            var controller = CreateController(mockMovieService);

            // Act
            var result = controller.MovieList("Av");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Movie>>(viewResult.Model);
            var movieList = model.ToList();
            Assert.Equal(2, movieList.Count); // Avatar, Avengers
            Assert.Equal("Avatar", movieList[0].MovieNameEnglish);
            Assert.Equal("Avengers", movieList[1].MovieNameEnglish);
        }

        [Fact]
        public void MovieList_SearchNoResult_ReturnsEmptyList()
        {
            // Arrange
            var mockMovieService = new Mock<IMovieService>();
            var movies = new List<Movie>
            {
                new Movie { MovieNameEnglish = "Avatar" },
                new Movie { MovieNameEnglish = "Batman" }
            };
            mockMovieService.Setup(s => s.GetAll()).Returns(movies);
            var controller = CreateController(mockMovieService);

            // Act
            var result = controller.MovieList("Superman");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Movie>>(viewResult.Model);
            Assert.Empty(model); // View sẽ hiển thị "No movies found"
        }

        [Fact]
        public void MovieList_EmptySearch_ReturnsAllMoviesSorted()
        {
            // Arrange
            var mockMovieService = new Mock<IMovieService>();
            var movies = new List<Movie>
            {
                new Movie { MovieNameEnglish = "Batman" },
                new Movie { MovieNameEnglish = "Avatar" }
            };
            mockMovieService.Setup(s => s.GetAll()).Returns(movies);
            var controller = CreateController(mockMovieService);

            // Act
            var result = controller.MovieList("");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Movie>>(viewResult.Model);
            var movieList = model.ToList();
            Assert.Equal(2, movieList.Count);
            Assert.Equal("Avatar", movieList[0].MovieNameEnglish); // Đã sort A-Z
            Assert.Equal("Batman", movieList[1].MovieNameEnglish);
        }
    }
} 