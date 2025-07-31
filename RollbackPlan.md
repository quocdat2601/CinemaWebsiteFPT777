# Rollback Plan cho Path Security Changes

## Nếu có vấn đề, có thể rollback bằng cách:

### Option 1: Comment out PathSecurityHelper
```csharp
// Thay vì:
string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.LargeImageFile.FileName);
string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);

// Sử dụng lại code cũ:
string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.LargeImageFile.FileName;
string filePath = Path.Combine(uploadsFolder, uniqueFileName);
```

### Option 2: Git Revert
```bash
git revert <commit-hash>
```

### Option 3: Temporary Disable
```csharp
// Tạm thời disable validation
string? secureFilePath = uploadsFolder + "/" + uniqueFileName; // Bỏ validation
```

## Files cần rollback:
- Controllers/MovieController.cs
- Controllers/VoucherController.cs  
- Service/AccountService.cs
- Helpers/PathSecurityHelper.cs (có thể xóa)

## Test sau rollback:
- Upload file bình thường
- Kiểm tra file được lưu đúng
- Kiểm tra database được cập nhật 