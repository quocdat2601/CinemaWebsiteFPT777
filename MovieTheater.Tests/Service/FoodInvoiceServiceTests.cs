using Xunit;
using Moq;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MovieTheater.Tests.Service
{
    public class FoodInvoiceServiceTests
    {
        private readonly Mock<IFoodInvoiceRepository> _mockRepo;
        private readonly FoodInvoiceService _service;

        public FoodInvoiceServiceTests()
        {
            _mockRepo = new Mock<IFoodInvoiceRepository>();
            _service = new FoodInvoiceService(_mockRepo.Object);
        }

        [Fact]
        public async Task GetFoodsByInvoiceIdAsync_ReturnsFoodViewModels()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            var foodInvoice = new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "INV1", FoodId = 1, Quantity = 2, Price = 50000, Food = food };
            _mockRepo.Setup(r => r.GetByInvoiceIdAsync("INV1")).ReturnsAsync(new List<FoodInvoice> { foodInvoice });
            var result = await _service.GetFoodsByInvoiceIdAsync("INV1");
            Assert.Single(result);
            Assert.Equal("Popcorn", result.First().Name);
            Assert.Equal(2, result.First().Quantity);
        }

        [Fact]
        public async Task SaveFoodOrderAsync_SavesFoodsAndReturnsTrue()
        {
            var foods = new List<FoodViewModel> { new FoodViewModel { FoodId = 1, Name = "Popcorn", Price = 50000, Quantity = 2 } };
            _mockRepo.Setup(r => r.CreateMultipleAsync(It.IsAny<IEnumerable<FoodInvoice>>())).ReturnsAsync(new List<FoodInvoice>());
            var result = await _service.SaveFoodOrderAsync("INV1", foods);
            Assert.True(result);
        }

        [Fact]
        public async Task SaveFoodOrderAsync_NullOrEmptyList_ReturnsTrue()
        {
            var result1 = await _service.SaveFoodOrderAsync("INV1", null);
            var result2 = await _service.SaveFoodOrderAsync("INV1", new List<FoodViewModel>());
            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task GetTotalFoodPriceByInvoiceIdAsync_ReturnsCorrectTotal()
        {
            var foodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "INV1", FoodId = 1, Quantity = 2, Price = 50000 },
                new FoodInvoice { FoodInvoiceId = 2, InvoiceId = "INV1", FoodId = 2, Quantity = 1, Price = 30000 }
            };
            _mockRepo.Setup(r => r.GetByInvoiceIdAsync("INV1")).ReturnsAsync(foodInvoices);
            var total = await _service.GetTotalFoodPriceByInvoiceIdAsync("INV1");
            Assert.Equal(130000, total);
        }
    }
} 