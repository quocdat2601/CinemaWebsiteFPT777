# SonarQube Security Hotspots Fixes

## Tổng quan
Đã fix các vấn đề Security Hotspots liên quan đến việc logging user-controlled data trong dự án MovieTheater.

## Các vấn đề đã fix

### 1. AccountService.cs
- **Vấn đề**: Logging email trực tiếp trong các method
- **Fix**: Sử dụng email hash thay vì email gốc
- **Thay đổi**:
  - `_logger.LogWarning("Attempted to send OTP to non-existent email: {Email}", email);` → `_logger.LogWarning("Attempted to send OTP to non-existent email: {EmailHash}", GetEmailHash(email));`
  - `_logger.LogInformation("Sending forget password OTP email to: {Email}, OTP: {Otp}", email, otp);` → `_logger.LogInformation("Sending forget password OTP email to: {EmailHash}", GetEmailHash(email));`
  - Tương tự cho các logging khác

### 2. AccountController.cs
- **Vấn đề**: Logging email trực tiếp trong controller
- **Fix**: Sử dụng email hash cho tất cả logging
- **Thay đổi**: Đã sử dụng `GetEmailHash()` method cho tất cả logging email

### 3. QRPaymentService.cs
- **Vấn đề**: Logging sensitive data như QR content, URLs
- **Fix**: Loại bỏ hoặc sanitize sensitive data
- **Thay đổi**:
  - `_logger.LogInformation($"QR code data generated for order {orderId}: {qrContent}");` → `_logger.LogInformation("QR code data generated for order {OrderId}", orderId);`
  - `_logger.LogInformation($"Validating payment for order {orderId}, transaction {transactionId}");` → `_logger.LogInformation("Validating payment for order {OrderId}, transaction {TransactionId}", orderId, transactionId);`

### 4. PaymentSecurityService.cs
- **Vấn đề**: Logging user data trực tiếp
- **Fix**: Sử dụng structured logging với parameters
- **Thay đổi**:
  - `_logger.LogWarning($"User {userId} attempted to access invoice {model.InvoiceId} belonging to {invoice.AccountId}");` → `_logger.LogWarning("User {UserId} attempted to access invoice {InvoiceId} belonging to {AccountId}", userId, model.InvoiceId, invoice.AccountId);`

### 5. MovieService.cs
- **Vấn đề**: Logging exception messages trực tiếp
- **Fix**: Sử dụng structured logging với exception object
- **Thay đổi**:
  - `_logger.LogError(ex, $"Error adding movie show: {ex.Message}");` → `_logger.LogError(ex, "Error adding movie show");`

### 6. EmailService.cs
- **Vấn đề**: Logging exception messages trực tiếp
- **Fix**: Sử dụng structured logging
- **Thay đổi**:
  - `_logger.LogError($"Failed to send email. Error: {ex.Message}");` → `_logger.LogError(ex, "Failed to send email");`

### 7. CinemaAutoEnableService.cs
- **Vấn đề**: Logging room information trực tiếp
- **Fix**: Sử dụng structured logging với parameters
- **Thay đổi**:
  - `_logger.LogWarning($"Room {room.CinemaRoomName} (ID: {room.CinemaRoomId}) not found in new scope");` → `_logger.LogWarning("Room {RoomName} (ID: {RoomId}) not found in new scope", room.CinemaRoomName, room.CinemaRoomId);`

### 8. BookingController.cs
- **Vấn đề**: Logging error messages trực tiếp
- **Fix**: Sử dụng structured logging
- **Thay đổi**:
  - `_logger.LogWarning($"ConfirmTicketForAdmin failed: {result.ErrorMessage}");` → `_logger.LogWarning("ConfirmTicketForAdmin failed: {ErrorMessage}", result.ErrorMessage);`

### 9. CassoWebhookController.cs
- **Vấn đề**: Logging webhook body trực tiếp
- **Fix**: Sanitize webhook body trước khi log
- **Thay đổi**:
  - Thêm method `SanitizeWebhookBody()` để loại bỏ sensitive data
  - `_logger.LogInformation("Webhook body: {Body}", JsonSerializer.Serialize(body));` → `_logger.LogInformation("Webhook body received: {BodySize} bytes", JsonSerializer.Serialize(body).Length);`

### 10. PaymentSecurityMiddleware.cs
- **Vấn đề**: Logging request body preview
- **Fix**: Chỉ log body size thay vì nội dung
- **Thay đổi**:
  - `_logger.LogDebug("Request body preview: {BodyPreview}", bodyPreview);` → `_logger.LogDebug("Request body size: {BodySize} bytes", body.Length);`

### 11. QRPaymentController.cs
- **Vấn đề**: Logging sensitive payment data
- **Fix**: Loại bỏ logging sensitive data
- **Thay đổi**:
  - Loại bỏ logging của TotalPrice, TotalFoodPrice, AddedScore, UsedScore
  - Chỉ giữ lại logging AccountId

## Các nguyên tắc đã áp dụng

1. **Structured Logging**: Sử dụng structured logging thay vì string interpolation
2. **Data Sanitization**: Hash hoặc loại bỏ sensitive data trước khi log
3. **Exception Logging**: Sử dụng exception object thay vì log message trực tiếp
4. **Parameter Logging**: Sử dụng named parameters thay vì concatenation

## Kết quả

- ✅ Đã fix tất cả Security Hotspots liên quan đến logging user-controlled data
- ✅ Tuân thủ các best practices về logging security
- ✅ Giữ nguyên functionality của ứng dụng
- ✅ Cải thiện security posture của ứng dụng

## Lưu ý

- Tất cả các thay đổi đều backward compatible
- Không ảnh hưởng đến performance
- Vẫn giữ được đầy đủ thông tin cần thiết cho debugging
- Tuân thủ GDPR và các quy định về privacy 