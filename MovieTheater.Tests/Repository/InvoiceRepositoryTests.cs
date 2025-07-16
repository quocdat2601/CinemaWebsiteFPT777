using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class InvoiceRepositoryTests
    {
        private MovieTheaterContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "InvoiceRepoTestDb" + Guid.NewGuid())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public void GetAll_ShouldReturnAllInvoicesWithIncludes()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1 };
            context.CinemaRooms.Add(cinemaRoom);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            var version = new MovieTheater.Models.Version { VersionId = 1 };
            context.Versions.Add(version);
            context.SaveChanges();
            var movieShow = new MovieShow
            {
                MovieShowId = 1,
                MovieId = movie.MovieId,
                Movie = movie,
                CinemaRoomId = cinemaRoom.CinemaRoomId,
                CinemaRoom = cinemaRoom,
                ScheduleId = schedule.ScheduleId,
                Schedule = schedule,
                VersionId = version.VersionId,
                Version = version
            };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var invoice = new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, Account = account, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow };
            context.Invoices.Add(invoice);
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = repo.GetAll().ToList();
            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].Account);
            Assert.NotNull(result[0].MovieShow);
        }

        [Fact]
        public void GetById_ShouldReturnInvoiceWithIncludes_WhenFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1", Members = new List<Member>() };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1 };
            context.CinemaRooms.Add(cinemaRoom);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            var version = new MovieTheater.Models.Version { VersionId = 1 };
            context.Versions.Add(version);
            context.SaveChanges();
            var movieShow = new MovieShow
            {
                MovieShowId = 1,
                MovieId = movie.MovieId,
                Movie = movie,
                CinemaRoomId = cinemaRoom.CinemaRoomId,
                CinemaRoom = cinemaRoom,
                ScheduleId = schedule.ScheduleId,
                Schedule = schedule,
                VersionId = version.VersionId,
                Version = version
            };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            var invoice = new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, Account = account, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow };
            context.Invoices.Add(invoice);
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = repo.GetById("inv1");
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Account);
            Assert.NotNull(result.MovieShow);
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new InvoiceRepository(context);
            // Act
            var result = repo.GetById("notfound");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByAccountIdAsync_ShouldReturnInvoicesForAccount()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.SaveChanges();
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = movie.MovieId, Movie = movie, ScheduleId = schedule.ScheduleId, Schedule = schedule };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            context.Invoices.Add(new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, Status = InvoiceStatus.Completed, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.Invoices.Add(new Invoice { InvoiceId = "inv2", AccountId = account.AccountId, Status = InvoiceStatus.Incomplete, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = (await repo.GetByAccountIdAsync(account.AccountId)).ToList();
            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByAccountIdAsync_ShouldFilterByStatus()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.SaveChanges();
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = movie.MovieId, Movie = movie, ScheduleId = schedule.ScheduleId, Schedule = schedule };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            context.Invoices.Add(new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, Status = InvoiceStatus.Completed, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.Invoices.Add(new Invoice { InvoiceId = "inv2", AccountId = account.AccountId, Status = InvoiceStatus.Incomplete, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = (await repo.GetByAccountIdAsync(account.AccountId, InvoiceStatus.Completed)).ToList();
            // Assert
            Assert.Single(result);
            Assert.Equal(InvoiceStatus.Completed, result[0].Status);
        }

        [Fact]
        public async Task GetByDateRangeAsync_ShouldReturnInvoicesInRange()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.SaveChanges();
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = movie.MovieId, Movie = movie, ScheduleId = schedule.ScheduleId, Schedule = schedule };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            context.Invoices.Add(new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, BookingDate = new DateTime(2024, 1, 1), MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.Invoices.Add(new Invoice { InvoiceId = "inv2", AccountId = account.AccountId, BookingDate = new DateTime(2024, 2, 1), MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = (await repo.GetByDateRangeAsync(account.AccountId, new DateTime(2024, 1, 15), new DateTime(2024, 2, 15))).ToList();
            // Assert
            Assert.Single(result);
            Assert.Equal("inv2", result[0].InvoiceId);
        }

        [Fact]
        public async Task GetByDateRangeAsync_ShouldReturnAll_WhenNoDates()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.SaveChanges();
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = movie.MovieId, Movie = movie, ScheduleId = schedule.ScheduleId, Schedule = schedule };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            context.Invoices.Add(new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, BookingDate = new DateTime(2024, 1, 1), MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.Invoices.Add(new Invoice { InvoiceId = "inv2", AccountId = account.AccountId, BookingDate = new DateTime(2024, 2, 1), MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = (await repo.GetByDateRangeAsync(account.AccountId, null, null)).ToList();
            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetDetailsAsync_ShouldReturnInvoice_WhenFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1 };
            context.CinemaRooms.Add(cinemaRoom);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            var version = new MovieTheater.Models.Version { VersionId = 1 };
            context.Versions.Add(version);
            context.SaveChanges();
            var movieShow = new MovieShow
            {
                MovieShowId = 1,
                MovieId = movie.MovieId,
                Movie = movie,
                CinemaRoomId = cinemaRoom.CinemaRoomId,
                CinemaRoom = cinemaRoom,
                ScheduleId = schedule.ScheduleId,
                Schedule = schedule,
                VersionId = version.VersionId,
                Version = version
            };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            context.Invoices.Add(new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = await repo.GetDetailsAsync("inv1", account.AccountId);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        [Fact]
        public async Task GetDetailsAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new InvoiceRepository(context);
            // Act
            var result = await repo.GetDetailsAsync("notfound", "acc1");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetForCancelAsync_ShouldReturnInvoice_WhenFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var account = new Account { AccountId = "acc1" };
            context.Accounts.Add(account);
            var movie = new Movie { MovieId = "1" };
            context.Movies.Add(movie);
            var schedule = new Schedule { ScheduleId = 1 };
            context.Schedules.Add(schedule);
            context.SaveChanges();
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = movie.MovieId, Movie = movie, ScheduleId = schedule.ScheduleId, Schedule = schedule };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();
            context.Invoices.Add(new Invoice { InvoiceId = "inv1", AccountId = account.AccountId, MovieShowId = movieShow.MovieShowId, MovieShow = movieShow });
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            var result = await repo.GetForCancelAsync("inv1", account.AccountId);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("inv1", result.InvoiceId);
        }

        [Fact]
        public async Task GetForCancelAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new InvoiceRepository(context);
            // Act
            var result = await repo.GetForCancelAsync("notfound", "acc1");
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateInvoice()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var invoice = new Invoice { InvoiceId = "inv1", AccountId = "acc1" };
            context.Invoices.Add(invoice);
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            invoice.Status = InvoiceStatus.Completed;
            // Act
            await repo.UpdateAsync(invoice);
            // Assert
            var updated = context.Invoices.First(i => i.InvoiceId == "inv1");
            Assert.Equal(InvoiceStatus.Completed, updated.Status);
        }

        [Fact]
        public void Update_ShouldSetEntityStateToModified()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var invoice = new Invoice { InvoiceId = "inv1", AccountId = "acc1" };
            context.Invoices.Add(invoice);
            context.SaveChanges();
            var repo = new InvoiceRepository(context);
            // Act
            repo.Update(invoice);
            // Assert
            Assert.Equal(EntityState.Modified, context.Entry(invoice).State);
        }

        [Fact]
        public void Save_ShouldCallSaveChanges()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var repo = new InvoiceRepository(context);
            // Act
            repo.Save();
            // Assert
            Assert.True(true); // No exception means success
        }
    }
} 