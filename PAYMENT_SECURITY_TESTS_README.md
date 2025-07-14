# 🧪 Payment Security Unit Tests

## 📋 Tổng Quan

Unit tests được tạo để kiểm tra tính đúng đắn của các security features trong payment processing:

1. **PaymentSecurityMiddlewareTests** - Test middleware behavior
2. **PaymentSecurityServiceTests** - Test validation logic

## 🚀 Cách Chạy Tests

### 1. **Chạy Tất Cả Tests**
```bash
dotnet test
```

### 2. **Chạy Riêng Payment Security Tests**
```bash
dotnet test --filter "FullyQualifiedName~PaymentSecurity"
```

### 3. **Chạy Middleware Tests**
```bash
dotnet test --filter "FullyQualifiedName~PaymentSecurityMiddleware"
```

### 4. **Chạy Service Tests**
```bash
dotnet test --filter "FullyQualifiedName~PaymentSecurityService"
```

## 📊 Test Cases Đã Cover

### 🔍 **PaymentSecurityMiddlewareTests**

#### ✅ **Request Detection Tests**
- `InvokeAsync_NonPaymentRequest_ShouldNotLog` - Kiểm tra không log request không phải payment
- `InvokeAsync_PaymentApiRequest_ShouldLogAndValidate` - Kiểm tra log và validate payment API request

#### ✅ **Authentication Tests**
- `InvokeAsync_UnauthenticatedUser_ShouldReturn401` - Kiểm tra user chưa đăng nhập
- `InvokeAsync_ServiceNotResolved_ShouldContinue` - Kiểm tra service không resolve được

#### ✅ **Validation Tests**
- `InvokeAsync_InvalidPaymentData_ShouldReturn400` - Kiểm tra data không hợp lệ
- `InvokeAsync_EmptyJsonBody_ShouldContinue` - Kiểm tra body rỗng
- `InvokeAsync_InvalidJsonBody_ShouldContinue` - Kiểm tra JSON không hợp lệ

#### ✅ **VNPay Integration Tests**
- `InvokeAsync_VnPayReturnRequest_ShouldOnlyLog` - Kiểm tra VNPay return request

### 🔍 **PaymentSecurityServiceTests**

#### ✅ **Payment Data Validation Tests**
- `ValidatePaymentData_WithNullInvoiceId_ShouldReturnError` - Invoice ID null
- `ValidatePaymentData_WithEmptyInvoiceId_ShouldReturnError` - Invoice ID rỗng
- `ValidatePaymentData_WithInvalidAmount_ShouldReturnError` - Amount âm
- `ValidatePaymentData_WithZeroAmount_ShouldReturnError` - Amount = 0
- `ValidatePaymentData_WithNonExistentInvoice_ShouldReturnError` - Invoice không tồn tại

#### ✅ **Authorization Tests**
- `ValidatePaymentData_WithInvoiceBelongingToDifferentUser_ShouldReturnError` - User khác truy cập
- `ValidatePaymentData_WithCompletedInvoice_ShouldReturnError` - Invoice đã hoàn thành
- `ValidatePaymentData_WithValidData_ShouldReturnSuccess` - Data hợp lệ

#### ✅ **Amount Validation Tests**
- `ValidateAmount_WithMatchingAmount_ShouldReturnSuccess` - Amount khớp
- `ValidateAmount_WithNonMatchingAmount_ShouldReturnError` - Amount không khớp

#### ✅ **VNPay Response Validation Tests**
- `ValidatePaymentResponse_WithValidSignature_ShouldReturnSuccess` - Signature hợp lệ
- `ValidatePaymentResponse_WithInvalidSignature_ShouldReturnError` - Signature không hợp lệ
- `ValidatePaymentResponse_WithFailedResponseCode_ShouldReturnError` - Response code lỗi
- `ValidatePaymentResponse_WithMissingRequiredFields_ShouldReturnError` - Thiếu field bắt buộc
- `ValidatePaymentResponse_WithNonExistentInvoice_ShouldReturnError` - Invoice không tồn tại

## 🛠️ Test Setup

### **Dependencies**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="7.0.0" />
<PackageReference Include="Moq" Version="4.18.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.7.1" />
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
```

### **Test Data Setup**
```csharp
// In-memory database cho testing
var options = new DbContextOptionsBuilder<MovieTheaterContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

// Mock services
var mockLogger = new Mock<ILogger<PaymentSecurityService>>();
var mockVnPayService = new Mock<VNPayService>(null);
```

## 📈 Test Coverage

### **Middleware Coverage**
- ✅ Request detection (payment vs non-payment)
- ✅ Authentication check
- ✅ JSON body parsing
- ✅ Service resolution
- ✅ Validation integration
- ✅ Error handling

### **Service Coverage**
- ✅ Input validation (null, empty, invalid)
- ✅ Business rule validation
- ✅ Authorization checks
- ✅ Amount calculation validation
- ✅ VNPay response validation
- ✅ Error code mapping

## 🔧 Custom Test Scenarios

### **Thêm Test Case Mới**

#### 1. **Middleware Test**
```csharp
[Fact]
public async Task InvokeAsync_CustomScenario_ShouldBehaveCorrectly()
{
    // Arrange
    var context = CreateHttpContext("/custom/path", "POST", "custom body");
    
    // Act
    await _middleware.InvokeAsync(context);
    
    // Assert
    // Your assertions here
}
```

#### 2. **Service Test**
```csharp
[Fact]
public void ValidatePaymentData_CustomScenario_ShouldReturnExpectedResult()
{
    // Arrange
    var model = new PaymentViewModel { /* custom data */ };
    
    // Act
    var result = _service.ValidatePaymentData(model, "user123");
    
    // Assert
    Assert.True(result.IsValid); // or False based on scenario
    Assert.Equal("EXPECTED_CODE", result.ErrorCode);
}
```

## 🚨 Troubleshooting

### **Common Issues**

#### 1. **Test Database Issues**
```bash
# Clear test database cache
dotnet test --logger "console;verbosity=detailed"
```

#### 2. **Mock Setup Issues**
```csharp
// Ensure proper mock setup
_mockVnPayService.Setup(x => x.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>()))
    .Returns(true);
```

#### 3. **Service Resolution Issues**
```csharp
// Ensure service is registered in test
var serviceProvider = new Mock<IServiceProvider>();
serviceProvider.Setup(x => x.GetService(typeof(IPaymentSecurityService)))
    .Returns(_mockPaymentSecurityService.Object);
```

## 📊 Performance Testing

### **Load Testing Scenarios**
```csharp
[Fact]
public async Task InvokeAsync_ConcurrentRequests_ShouldHandleCorrectly()
{
    // Test concurrent payment requests
    var tasks = Enumerable.Range(0, 100).Select(async i =>
    {
        var context = CreateHttpContext("/api/payment/create-payment", "POST", "{}");
        await _middleware.InvokeAsync(context);
    });
    
    await Task.WhenAll(tasks);
}
```

## 🎯 Best Practices

### **1. Test Isolation**
- Mỗi test sử dụng database riêng biệt
- Cleanup sau mỗi test
- Không phụ thuộc vào test khác

### **2. Mock Strategy**
- Mock external dependencies (VNPayService)
- Use in-memory database cho Entity Framework
- Mock ILogger để kiểm tra log messages

### **3. Assertion Strategy**
- Test cả success và failure cases
- Verify error codes và messages
- Check log entries khi cần

### **4. Test Data Management**
- Tạo test data trong mỗi test
- Sử dụng meaningful test data
- Cleanup sau mỗi test

## 📈 Monitoring Test Results

### **Test Report**
```bash
# Generate test report
dotnet test --logger "trx;LogFileName=PaymentSecurityTests.trx"
```

### **Coverage Report**
```bash
# Generate coverage report (if using coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🎯 Kết Luận

Unit tests này đảm bảo:
- ✅ **Security validation** hoạt động đúng
- ✅ **Error handling** xử lý đúng các trường hợp
- ✅ **Business rules** được enforce
- ✅ **Integration points** (VNPay) được test
- ✅ **Edge cases** được cover

Chạy tests thường xuyên để đảm bảo payment security luôn hoạt động đúng! 🚀 