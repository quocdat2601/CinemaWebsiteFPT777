# Chức Năng Quên Mật Khẩu - FPT 777 Cinema

## Tổng Quan

Chức năng quên mật khẩu cho phép người dùng đặt lại mật khẩu thông qua xác thực OTP qua email. Hệ thống sẽ gửi mã OTP 6 số đến email đã đăng ký và cho phép người dùng tạo mật khẩu mới.

## Luồng Hoạt Động

### 1. Người dùng yêu cầu quên mật khẩu
- Truy cập trang đăng nhập
- Click vào link "Forgot password?"
- Nhập email đã đăng ký trong hệ thống
- Hệ thống kiểm tra email có tồn tại không
- Nếu email hợp lệ, gửi mã OTP qua email

### 2. Xác thực OTP và đặt lại mật khẩu
- Người dùng nhập mã OTP 6 số
- Nhập mật khẩu mới (8-16 ký tự)
- Xác nhận mật khẩu mới
- Hệ thống kiểm tra OTP và cập nhật mật khẩu

## Các File Đã Tạo/Cập Nhật

### ViewModels
- `ViewModels/ForgetPasswordViewModel.cs` - Model cho form quên mật khẩu
- `ViewModels/ResetPasswordViewModel.cs` - Model cho form đặt lại mật khẩu

### Service
- `Service/IAccountService.cs` - Thêm interface methods
- `Service/AccountService.cs` - Thêm implementation cho chức năng quên mật khẩu

### Controller
- `Controllers/AccountController.cs` - Thêm actions cho quên mật khẩu

### Views
- `Views/Account/ForgetPassword.cshtml` - Trang nhập email
- `Views/Account/ResetPassword.cshtml` - Trang đặt lại mật khẩu

### Tests
- `MovieTheater.Tests/Service/ForgetPasswordTests.cs` - Unit tests cho chức năng

## Cấu Hình Email

Chức năng sử dụng cấu hình email trong `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Movie Theater"
  }
}
```

## Tính Năng Bảo Mật

### OTP Security
- Mã OTP 6 số ngẫu nhiên
- Thời gian hết hạn: 10 phút
- Lưu trữ trong memory (ConcurrentDictionary)
- Tự động xóa sau khi sử dụng

### Password Requirements
- Độ dài: 8-16 ký tự
- Validation real-time trên frontend
- Hash password trước khi lưu vào database

### Email Template
- Template HTML đẹp mắt
- Thông tin rõ ràng về OTP
- Cảnh báo về thời gian hết hạn
- Responsive design

## API Endpoints

### GET /Account/ForgetPassword
- Hiển thị form nhập email

### POST /Account/ForgetPassword
- Nhận email và gửi OTP
- Redirect đến trang đặt lại mật khẩu

### GET /Account/ResetPassword
- Hiển thị form nhập OTP và mật khẩu mới

### POST /Account/ResetPassword
- Xác thực OTP và cập nhật mật khẩu
- Redirect về trang đăng nhập

## Validation Rules

### Email Validation
- Email phải hợp lệ format
- Email phải tồn tại trong hệ thống

### OTP Validation
- OTP phải có đúng 6 ký tự số
- OTP phải còn hiệu lực (chưa hết hạn)
- OTP phải khớp với email

### Password Validation
- Độ dài: 8-16 ký tự
- Xác nhận mật khẩu phải khớp
- Validation real-time trên frontend

## Error Handling

### Common Errors
- Email không tồn tại
- OTP không đúng hoặc hết hạn
- Mật khẩu không đúng format
- Mật khẩu xác nhận không khớp

### User Feedback
- Thông báo lỗi rõ ràng
- Loading states
- Success messages
- Redirect tự động

## Testing

### Unit Tests
- Test gửi OTP với email hợp lệ
- Test gửi OTP với email không tồn tại
- Test xác thực OTP
- Test đặt lại mật khẩu
- Test validation

### Manual Testing
1. Truy cập trang đăng nhập
2. Click "Forgot password?"
3. Nhập email hợp lệ
4. Kiểm tra email nhận OTP
5. Nhập OTP và mật khẩu mới
6. Đăng nhập với mật khẩu mới

## Security Considerations

### Best Practices
- Sử dụng HTTPS
- Rate limiting cho OTP requests
- Logging cho security events
- Xóa OTP sau khi sử dụng
- Hash password trước khi lưu

### Potential Improvements
- Thêm CAPTCHA cho form
- Rate limiting chi tiết hơn
- Audit logging
- Email verification trước khi gửi OTP

## Troubleshooting

### Common Issues
1. **Email không nhận được OTP**
   - Kiểm tra cấu hình SMTP
   - Kiểm tra thư mục spam
   - Kiểm tra app password Gmail

2. **OTP không đúng**
   - Đảm bảo nhập đúng 6 số
   - Kiểm tra thời gian hết hạn
   - Thử gửi lại OTP

3. **Mật khẩu không cập nhật**
   - Kiểm tra validation rules
   - Kiểm tra database connection
   - Kiểm tra logs

## Deployment Notes

### Environment Variables
- Cấu hình email settings
- Database connection string
- Logging configuration

### Dependencies
- ASP.NET Core 6.0+
- Entity Framework Core
- System.Net.Mail
- Font Awesome (CSS)

## Support

Nếu gặp vấn đề, vui lòng:
1. Kiểm tra logs trong thư mục `Logs/`
2. Kiểm tra cấu hình email
3. Kiểm tra database connection
4. Liên hệ team development 