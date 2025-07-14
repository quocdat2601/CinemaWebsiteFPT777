# Cấu hình SonarCloud cho dự án MovieTheater

## Tổng quan

Dự án này đã được cấu hình để sử dụng SonarCloud với các loại trừ (exclusions) tối ưu để giảm số lượng code được phân tích, giúp tiết kiệm quota của gói miễn phí.

## Các file cấu hình

### 1. `sonar-project.properties`
File cấu hình chính cho SonarCloud với các thiết lập:
- Loại trừ các thư mục build và generated code
- Loại trừ test files (chỉ cần coverage, không cần phân tích chất lượng)
- Loại trừ static assets và third-party libraries
- Loại trừ documentation và configuration files

### 2. `.github/workflows/build.yml`
GitHub Actions workflow để tự động phân tích code khi push/PR.

## Các loại trừ (Exclusions)

### Build artifacts và generated code
- `**/obj/**` - Thư mục build objects
- `**/bin/**` - Thư mục build binaries  
- `**/.vs/**` - Visual Studio files
- `**/Properties/**` - Project properties

### Test files
- `**/MovieTheater.Tests/**` - Toàn bộ thư mục test

### Static web assets
- `**/wwwroot/lib/**` - Third-party libraries
- `**/wwwroot/webfonts/**` - Font files
- `**/wwwroot/images/**` - Image files
- `**/wwwroot/css/**` - CSS files
- `**/wwwroot/js/**` - JavaScript files
- `**/wwwroot/favicon.ico` - Favicon

### Documentation và configuration
- `**/README.md` - Documentation files
- `**/CI_CD_README.md` - CI/CD documentation
- `**/PAYMENT_SECURITY_README.md` - Security documentation
- `**/PAYMENT_SECURITY_TESTS_README.md` - Test documentation

### Database và deployment files
- `**/scripst.sql` - Database scripts
- `**/Dockerfile` - Docker configuration
- `**/docker-compose.yml` - Docker compose
- `**/config.toml` - Configuration files

### Build scripts
- `**/generate-coverage.bat` - Coverage generation script

### Git và Docker files
- `**/.gitignore` - Git ignore file
- `**/.gitattributes` - Git attributes
- `**/.dockerignore` - Docker ignore file

### Coverage reports
- `**/coverage-report/**` - Coverage report files

## Lợi ích của cấu hình này

1. **Giảm số lượng code được phân tích**: Chỉ phân tích code business logic thực sự
2. **Tiết kiệm quota**: Tối ưu hóa cho gói miễn phí SonarCloud
3. **Tập trung vào chất lượng code**: Chỉ phân tích các file C# chính
4. **Tăng tốc độ phân tích**: Ít file hơn = phân tích nhanh hơn

## Cách thêm loại trừ mới

Để thêm loại trừ mới, chỉnh sửa file `sonar-project.properties`:

```properties
# Thêm loại trừ mới
sonar.exclusions+=**/path/to/exclude/**
```

## Monitoring

Kiểm tra SonarCloud dashboard để theo dõi:
- Số lượng lines of code được phân tích
- Coverage percentage
- Code quality metrics
- Security vulnerabilities

## Troubleshooting

Nếu gặp vấn đề với cấu hình:
1. Kiểm tra file `sonar-project.properties`
2. Xem logs trong GitHub Actions
3. Kiểm tra SonarCloud dashboard
4. Đảm bảo SONAR_TOKEN được cấu hình đúng 