using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class PersonRepositoryTests
    {
        private readonly MovieTheaterContext _context;
        private readonly PersonRepository _repository;

        public PersonRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);
            _repository = new PersonRepository(_context);
        }

        [Fact]
        public void GetAll_ReturnsAllPeople()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { PersonId = 1, Name = "John Doe", IsDirector = true },
                new Person { PersonId = 2, Name = "Jane Smith", IsDirector = false },
                new Person { PersonId = 3, Name = "Bob Johnson", IsDirector = true }
            };
            _context.People.AddRange(people);
            _context.SaveChanges();

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void GetAll_WhenNoPeople_ReturnsEmptyList()
        {
            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetById_WhenPersonExists_ReturnsPerson()
        {
            // Arrange
            var person = new Person { PersonId = 1, Name = "John Doe", IsDirector = true };
            _context.People.Add(person);
            _context.SaveChanges();

            // Act
            var result = _repository.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Doe", result.Name);
            Assert.True(result.IsDirector);
        }

        [Fact]
        public void GetById_WhenPersonDoesNotExist_ReturnsNull()
        {
            // Act
            var result = _repository.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetMovieByPerson_WhenPersonHasMovies_ReturnsMovies()
        {
            // Arrange
            var person = new Person { PersonId = 1, Name = "John Doe", IsDirector = true };
            var movie = new Movie { MovieId = "1", Content = "Test Movie" };
            person.Movies.Add(movie);
            
            _context.People.Add(person);
            _context.Movies.Add(movie);
            _context.SaveChanges();

            // Act
            var result = _repository.GetMovieByPerson(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Test Movie", result.First().Content);
        }

        [Fact]
        public void GetMovieByPerson_WhenPersonHasNoMovies_ReturnsEmptyList()
        {
            // Arrange
            var person = new Person { PersonId = 1, Name = "John Doe", IsDirector = true };
            _context.People.Add(person);
            _context.SaveChanges();

            // Act
            var result = _repository.GetMovieByPerson(1);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetDirectors_ReturnsOnlyDirectors()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { PersonId = 1, Name = "John Doe", IsDirector = true },
                new Person { PersonId = 2, Name = "Jane Smith", IsDirector = false },
                new Person { PersonId = 3, Name = "Bob Johnson", IsDirector = true }
            };
            _context.People.AddRange(people);
            _context.SaveChanges();

            // Act
            var result = _repository.GetDirectors();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, person => Assert.True(person.IsDirector));
        }

        [Fact]
        public void GetActors_ReturnsOnlyActors()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { PersonId = 1, Name = "John Doe", IsDirector = true },
                new Person { PersonId = 2, Name = "Jane Smith", IsDirector = false },
                new Person { PersonId = 3, Name = "Bob Johnson", IsDirector = true },
                new Person { PersonId = 4, Name = "Alice Brown", IsDirector = false }
            };
            _context.People.AddRange(people);
            _context.SaveChanges();

            // Act
            var result = _repository.GetActors();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, person => Assert.False(person.IsDirector));
        }

        [Fact]
        public void Add_AddsPersonToDatabase()
        {
            // Arrange
            var person = new Person { PersonId = 1, Name = "John Doe", IsDirector = true };

            // Act
            _repository.Add(person);
            _repository.Save();

            // Assert
            var savedPerson = _context.People.Find(1);
            Assert.NotNull(savedPerson);
            Assert.Equal("John Doe", savedPerson.Name);
        }

        [Fact]
        public void Update_WhenPersonExists_UpdatesPerson()
        {
            // Arrange
            var person = new Person { PersonId = 1, Name = "John Doe", IsDirector = true };
            _context.People.Add(person);
            _context.SaveChanges();

            var updatedPerson = new Person { PersonId = 1, Name = "John Updated", IsDirector = false };

            // Act
            _repository.Update(updatedPerson);
            _repository.Save();

            // Assert
            var result = _context.People.Find(1);
            Assert.NotNull(result);
            Assert.Equal("John Updated", result.Name);
            Assert.False(result.IsDirector);
        }

        [Fact]
        public void Update_WhenPersonDoesNotExist_DoesNothing()
        {
            // Arrange
            var person = new Person { PersonId = 999, Name = "Non-existent", IsDirector = true };

            // Act & Assert - Should not throw exception
            var exception = Record.Exception(() => _repository.Update(person));
            Assert.Null(exception);
        }

        [Fact]
        public void Delete_WhenPersonExists_RemovesPerson()
        {
            // Arrange
            var person = new Person { PersonId = 1, Name = "John Doe", IsDirector = true };
            _context.People.Add(person);
            _context.SaveChanges();

            // Act
            _repository.Delete(1);
            _repository.Save();

            // Assert
            var result = _context.People.Find(1);
            Assert.Null(result);
        }

        [Fact]
        public void Delete_WhenPersonDoesNotExist_DoesNothing()
        {
            // Act & Assert - Should not throw exception
            var exception = Record.Exception(() => _repository.Delete(999));
            Assert.Null(exception);
        }
    }
} 