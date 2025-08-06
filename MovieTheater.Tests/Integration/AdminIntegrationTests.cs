using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MovieTheater.Tests.Integration
{
    public class AdminIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public AdminIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        #region MainPage Tests
        [Fact]
        public async Task AdminMainPage_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage");    

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminMainPage_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminMainPage_ContainsAdminElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("admin", content.ToLower());
        }

        [Fact]
        public async Task AdminMainPage_WithTabParameter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminMainPage_WithRangeParameter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?range=monthly");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region EditMember Tests
        [Fact]
        public async Task AdminEditMember_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/Edit/test-id");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminEditMember_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/Edit/test-id");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminEditMember_ContainsEditMemberElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/Edit/test-id");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("form", content.ToLower());
        }

        [Fact]
        public async Task AdminEditMember_WithInvalidId_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/Edit/invalid-id");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region EditVoucher Tests
        [Fact]
        public async Task AdminEditVoucher_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Voucher/AdminEdit/test-id");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminEditVoucher_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Voucher/AdminEdit/test-id");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminEditVoucher_ContainsEditVoucherElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Voucher/AdminEdit/test-id");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("form", content.ToLower());
        }

        [Fact]
        public async Task AdminEditVoucher_WithInvalidId_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Voucher/AdminEdit/invalid-id");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region ShowtimeMg Tests
        [Fact]
        public async Task AdminShowtimeMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminShowtimeMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminShowtimeMg_ContainsShowtimeElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task AdminShowtimeMg_WithDateParameter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg?date=15/06/2024");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminShowtimeMg_WithInvalidDate_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg?date=invalid-date");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Dashboard Tab Tests
        [Fact]
        public async Task AdminDashboard_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminDashboard_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminDashboard_ContainsDashboardElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task AdminDashboard_WithMonthlyRange_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard&range=monthly");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region MemberMg Tab Tests
        [Fact]
        public async Task AdminMemberMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=MemberMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminMemberMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=MemberMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminMemberMg_ContainsMemberManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=MemberMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region EmployeeMg Tab Tests
        [Fact]
        public async Task AdminEmployeeMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=EmployeeMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminEmployeeMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=EmployeeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminEmployeeMg_ContainsEmployeeManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=EmployeeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task AdminEmployeeMg_WithKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=EmployeeMg&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region MovieMg Tab Tests
        [Fact]
        public async Task AdminMovieMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=MovieMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminMovieMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=MovieMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminMovieMg_ContainsMovieManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=MovieMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region ShowroomMg Tab Tests
        [Fact]
        public async Task AdminShowroomMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=ShowroomMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminShowroomMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=ShowroomMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminShowroomMg_ContainsShowroomManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=ShowroomMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region VersionMg Tab Tests
        [Fact]
        public async Task AdminVersionMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VersionMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminVersionMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VersionMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminVersionMg_ContainsVersionManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VersionMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region PromotionMg Tab Tests
        [Fact]
        public async Task AdminPromotionMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=PromotionMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminPromotionMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=PromotionMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminPromotionMg_ContainsPromotionManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=PromotionMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region BookingMg Tab Tests
        [Fact]
        public async Task AdminBookingMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=BookingMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminBookingMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=BookingMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminBookingMg_ContainsBookingManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=BookingMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task AdminBookingMg_WithKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=BookingMg&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminBookingMg_WithStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=BookingMg&statusFilter=completed");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region FoodMg Tab Tests
        [Fact]
        public async Task AdminFoodMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=FoodMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminFoodMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=FoodMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminFoodMg_ContainsFoodManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=FoodMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task AdminFoodMg_WithKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=FoodMg&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminFoodMg_WithCategoryFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=FoodMg&categoryFilter=drinks");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region VoucherMg Tab Tests
        [Fact]
        public async Task AdminVoucherMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VoucherMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminVoucherMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VoucherMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminVoucherMg_ContainsVoucherManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VoucherMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task AdminVoucherMg_WithKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VoucherMg&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminVoucherMg_WithStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=VoucherMg&statusFilter=active");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region RankMg Tab Tests
        [Fact]
        public async Task AdminRankMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=RankMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminRankMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=RankMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminRankMg_ContainsRankManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=RankMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region CastMg Tab Tests
        [Fact]
        public async Task AdminCastMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=CastMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminCastMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=CastMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task AdminCastMg_ContainsCastManagementElements()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=CastMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region LoadTab Tests for All Views
        [Theory]
        [InlineData("Dashboard")]
        [InlineData("MemberMg")]
        [InlineData("EmployeeMg")]
        [InlineData("MovieMg")]
        [InlineData("ShowroomMg")]
        [InlineData("VersionMg")]
        [InlineData("PromotionMg")]
        [InlineData("BookingMg")]
        [InlineData("FoodMg")]
        [InlineData("VoucherMg")]
        [InlineData("RankMg")]
        [InlineData("CastMg")]
        public async Task LoadTab_ReturnsSuccessStatusCode(string tab)
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync($"/Admin/LoadTab?tab={tab}");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Theory]
        [InlineData("Dashboard")]
        [InlineData("MemberMg")]
        [InlineData("EmployeeMg")]
        [InlineData("MovieMg")]
        [InlineData("ShowroomMg")]
        [InlineData("VersionMg")]
        [InlineData("PromotionMg")]
        [InlineData("BookingMg")]
        [InlineData("FoodMg")]
        [InlineData("VoucherMg")]
        [InlineData("RankMg")]
        [InlineData("CastMg")]
        public async Task LoadTab_ReturnsHtmlContent(string tab)
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync($"/Admin/LoadTab?tab={tab}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region Partial View Tests
        [Fact]
        public async Task BookingMgPartial_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/BookingMgPartial");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task BookingMgPartial_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/BookingMgPartial");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }

        [Fact]
        public async Task VoucherMgPartial_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/VoucherMgPartial");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task VoucherMgPartial_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/VoucherMgPartial");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("div", content.ToLower());
        }
        #endregion

        #region BookingMg Status Filter Tests for 100% Coverage
        [Fact]
        public async Task LoadTab_BookingMg_WithPaidStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=paid");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithCancelledStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=cancelled");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithUnpaidStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=unpaid");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithEmptyStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithPaidStatusFilterAndKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=paid&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithCancelledStatusFilterAndKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=cancelled&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithUnpaidStatusFilterAndKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=unpaid&keyword=test");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithPaidStatusFilterAndBookingType_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=paid&bookingTypeFilter=normal");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithCancelledStatusFilterAndBookingType_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=cancelled&bookingTypeFilter=employee");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithUnpaidStatusFilterAndBookingType_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=BookingMg&statusFilter=unpaid&bookingTypeFilter=all");

            // Assert
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region EditMember Page Tests
        [Fact]
        public async Task EditMember_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/Edit/test-id");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task EditMember_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/Edit/test-id");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
            Assert.Contains("<!DOCTYPE html>", content);
        }
        #endregion

        #region EditVoucher Page Tests
        [Fact]
        public async Task EditVoucher_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Voucher/AdminEdit/VOUCHER001");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task EditVoucher_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Voucher/AdminEdit/VOUCHER001");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
            Assert.Contains("<!DOCTYPE html>", content);
        }
        #endregion

        #region ShowtimeMg Page Tests
        [Fact]
        public async Task ShowtimeMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task ShowtimeMg_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task ShowtimeMg_WithDateParameter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/ShowtimeMg?date=01/01/2024");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }
        #endregion

        #region Dashboard Page Tests
        [Fact]
        public async Task Dashboard_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task Dashboard_ReturnsHtmlContent()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
            Assert.Contains("<!DOCTYPE html>", content);
        }

        [Fact]
        public async Task Dashboard_WithWeeklyRange_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard&range=weekly");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task Dashboard_WithMonthlyRange_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/MainPage?tab=Dashboard&range=monthly");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }
        #endregion

        #region LoadTab Tests
        [Fact]
        public async Task LoadTab_Dashboard_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=Dashboard");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task LoadTab_MemberMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=MemberMg");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=EmployeeMg");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_WithKeyword_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=EmployeeMg&keyword=test");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_WithStatusFilter_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateAuthenticatedClient("Admin");

            // Act
            var response = await client.GetAsync("/Admin/LoadTab?tab=EmployeeMg&statusFilter=active");

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Response status: {response.StatusCode}");
        }
        #endregion
    }
} 