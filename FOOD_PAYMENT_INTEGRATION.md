# Food Payment Integration

## Tổng quan
Tính năng này cho phép người dùng đặt đồ ăn và thức uống cùng với vé xem phim, và thanh toán tất cả trong một lần.

## Các thay đổi đã thực hiện

### 1. Database
- Tạo bảng `FoodInvoice` để lưu trữ thông tin đồ ăn đã đặt
- Chạy script `FoodInvoice_Table.sql` để tạo bảng

### 2. Models
- `FoodInvoice.cs`: Model cho bảng FoodInvoice
- Cập nhật `MovieTheaterContext.cs` để thêm DbSet cho FoodInvoice

### 3. Repository & Service
- `IFoodInvoiceRepository.cs` & `FoodInvoiceRepository.cs`: Repository pattern cho FoodInvoice
- `IFoodInvoiceService.cs` & `FoodInvoiceService.cs`: Service layer cho FoodInvoice

### 4. ViewModels
- Cập nhật `PaymentViewModel.cs` để bao gồm thông tin food:
  - `SelectedFoods`: Danh sách đồ ăn đã chọn
  - `TotalFoodPrice`: Tổng tiền đồ ăn
  - `TotalSeatPrice`: Tổng tiền ghế

### 5. Controllers
- Cập nhật `BookingController.cs`:
  - Method `Confirm`: Lưu food vào session và tính toán tổng tiền bao gồm food
  - Method `Payment`: Hiển thị thông tin food trong trang payment
- Cập nhật `PaymentController.cs`:
  - Method `VNPayReturn`: Lưu food orders vào database khi thanh toán thành công

### 6. Views
- Cập nhật `Payment.cshtml` để hiển thị:
  - Bảng thông tin đồ ăn đã chọn
  - Tách biệt giá ghế và giá đồ ăn
  - Tổng tiền cuối cùng

### 7. Dependency Injection
- Đăng ký `IFoodInvoiceRepository` và `IFoodInvoiceService` trong `Program.cs`

## Cách hoạt động

### 1. Chọn đồ ăn
- Người dùng chọn đồ ăn trong trang chọn ghế (`Seat/View.cshtml`)
- Thông tin đồ ăn được lưu trong session

### 2. Xác nhận đặt vé
- Trong trang xác nhận (`ConfirmBooking.cshtml`), hiển thị danh sách đồ ăn đã chọn
- Tính toán tổng tiền bao gồm cả ghế và đồ ăn
- Lưu thông tin đồ ăn vào session với key `SelectedFoods_{invoiceId}`

### 3. Thanh toán
- Trang payment hiển thị chi tiết:
  - Thông tin ghế và giá ghế
  - Danh sách đồ ăn và giá đồ ăn
  - Tổng tiền cuối cùng
- Khi thanh toán thành công, lưu thông tin đồ ăn vào bảng `FoodInvoice`

### 4. Lưu trữ
- Mỗi record trong `FoodInvoice` chứa:
  - `Invoice_ID`: Liên kết với hóa đơn
  - `Food_ID`: ID của món ăn
  - `Quantity`: Số lượng
  - `Price`: Giá tại thời điểm đặt (để tránh thay đổi giá sau này)

## Cấu trúc dữ liệu

### FoodInvoice Table
```sql
CREATE TABLE FoodInvoice (
    FoodInvoice_ID INT IDENTITY(1,1) PRIMARY KEY,
    Invoice_ID NVARCHAR(10) NOT NULL,
    Food_ID INT NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_FoodInvoice_Invoice FOREIGN KEY (Invoice_ID) REFERENCES Invoice(Invoice_ID),
    CONSTRAINT FK_FoodInvoice_Food FOREIGN KEY (Food_ID) REFERENCES Food(FoodId)
);
```

### PaymentViewModel
```csharp
public class PaymentViewModel
{
    // Existing properties...
    public List<FoodViewModel> SelectedFoods { get; set; }
    public decimal TotalFoodPrice { get; set; }
    public decimal TotalSeatPrice { get; set; }
}
```

## Lưu ý quan trọng

1. **Tính toán điểm**: Điểm chỉ được tính dựa trên giá ghế, không bao gồm giá đồ ăn
2. **Lưu giá**: Giá đồ ăn được lưu tại thời điểm đặt để tránh thay đổi giá sau này
3. **Session**: Thông tin đồ ăn được lưu trong session để duy trì qua các bước thanh toán
4. **Error handling**: Có xử lý lỗi khi lưu thông tin đồ ăn trong PaymentController

## Testing

1. Chọn ghế và đồ ăn trong trang chọn ghế
2. Xác nhận thông tin trong trang xác nhận
3. Kiểm tra thông tin đồ ăn trong trang payment
4. Thực hiện thanh toán
5. Kiểm tra dữ liệu trong bảng `FoodInvoice` sau khi thanh toán thành công 