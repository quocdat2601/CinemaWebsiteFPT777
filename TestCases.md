# Test Cases cho Path Security Changes

## Test Cases cần kiểm tra:

### 1. Movie Upload (Admin/Employee)
- [ ] Upload movie với LargeImageFile bình thường
- [ ] Upload movie với SmallImageFile bình thường  
- [ ] Upload movie với LogoFile bình thường
- [ ] Upload movie với tên file có ký tự đặc biệt
- [ ] Upload movie với tên file rỗng
- [ ] Upload movie với tên file có path traversal

### 2. Voucher Upload (Admin)
- [ ] Upload voucher image bình thường
- [ ] Upload voucher với tên file có ký tự đặc biệt
- [ ] Upload voucher với tên file rỗng
- [ ] Upload voucher với tên file có path traversal

### 3. Profile Image Upload (User)
- [ ] Upload avatar bình thường
- [ ] Upload avatar với tên file có ký tự đặc biệt
- [ ] Upload avatar với tên file rỗng
- [ ] Upload avatar với tên file có path traversal

## Expected Results:
- ✅ File upload thành công với tên file bình thường
- ✅ File upload thành công với tên file được sanitize
- ❌ File upload thất bại với path traversal attack
- ❌ File upload thất bại với tên file rỗng (sử dụng default)

## Rollback Plan:
Nếu có vấn đề, có thể rollback bằng cách:
1. Comment out các dòng sử dụng PathSecurityHelper
2. Sử dụng lại code cũ
 