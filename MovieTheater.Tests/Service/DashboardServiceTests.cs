using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ISeatService> _mockSeatService;
        private readonly Mock<IMemberRepository> _mockMemberRepository;
        private readonly DashboardService _dashboardService;
        private readonly MovieTheaterContext _context;

        public DashboardServiceTests()
        {
            _mockInvoiceService = new Mock<IInvoiceService>();
            _mockSeatService = new Mock<ISeatService>();
            _mockMemberRepository = new Mock<IMemberRepository>();

            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new MovieTheaterContext(options);

            _dashboardService = new DashboardService(_mockInvoiceService.Object, _mockSeatService.Object, _mockMemberRepository.Object, _context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        /// <summary>
        /// Tests that the DashboardService can be instantiated without throwing exceptions.
        /// </summary>
        [Fact]
        public void DashboardService_CanBeInstantiated_WithoutException()
        {
            // Arrange & Act & Assert
            Assert.NotNull(_dashboardService);
        }

        /// <summary>
        /// Tests that the DashboardService handles null services gracefully.
        /// </summary>
        [Fact]
        public void DashboardService_WithNullServices_DoesNotThrowException()
        {
            // Arrange & Act & Assert
            // Note: The DashboardService constructor doesn't validate null parameters
            var dashboardService1 = new DashboardService(null, _mockSeatService.Object, _mockMemberRepository.Object, _context);
            var dashboardService2 = new DashboardService(_mockInvoiceService.Object, null, _mockMemberRepository.Object, _context);
            var dashboardService3 = new DashboardService(_mockInvoiceService.Object, _mockSeatService.Object, null, _context);

            Assert.NotNull(dashboardService1);
            Assert.NotNull(dashboardService2);
            Assert.NotNull(dashboardService3);
        }

        /// <summary>
        /// Tests that the DashboardService implements the correct interface.
        /// </summary>
        [Fact]
        public void DashboardService_ImplementsIDashboardService()
        {
            // Arrange & Act & Assert
            Assert.IsAssignableFrom<IDashboardService>(_dashboardService);
        }

        /// <summary>
        /// Tests that the DashboardService has the expected dependencies.
        /// </summary>
        [Fact]
        public void DashboardService_HasExpectedDependencies()
        {
            // Arrange & Act & Assert
            Assert.NotNull(_dashboardService);
            
            // Test that the service can be instantiated with all dependencies
            var testService = new DashboardService(
                _mockInvoiceService.Object, 
                _mockSeatService.Object, 
                _mockMemberRepository.Object, 
                _context);
            
            Assert.NotNull(testService);
        }
    }
} 