# Hướng dẫn tích hợp Unit Test Coverage vào SonarQube

## Tổng quan
Dự án này đã được cấu hình để tích hợp unit test coverage vào SonarQube. Coverage sẽ được tạo ra bằng Coverlet và được gửi đến SonarQube để phân tích.

## Các file cấu hình đã tạo

### 1. `sonar-project.properties`
File cấu hình chính cho SonarQube analysis:
- Định nghĩa project key và tên
- Cấu hình đường dẫn source code và test
- Thiết lập coverage exclusions
- Cấu hình quality gate

### 2. `sonar-analysis.bat`
Script để chạy SonarQube analysis locally:
- Kiểm tra SonarQube Scanner
- Chạy tests với coverage
- Thực hiện SonarQube analysis

### 3. `.gitlab-ci.yml` (đã cập nhật)
Pipeline CI/CD với các stage:
- `build`: Build project
- `test`: Chạy tests với coverage
- `sonarqube`: Phân tích SonarQube
- `deploy`: Deploy ứng dụng

## Cách sử dụng

### Chạy locally

1. **Cài đặt SonarQube Scanner:**
   ```bash
   # Windows
   # Tải từ: https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/
   
   # Hoặc sử dụng dotnet tool
   dotnet tool install --global dotnet-sonarscanner
   ```

2. **Chạy analysis:**
   ```bash
   # Sử dụng script batch
   sonar-analysis.bat
   
   # Hoặc chạy thủ công
   dotnet test --collect:"XPlat Code Coverage" --results-directory "TestResults"
   sonar-scanner
   ```

### Chạy trên GitLab CI/CD

1. **Thiết lập biến môi trường trong GitLab:**
   - `SONAR_HOST_URL`: URL của SonarQube server
   - `SONAR_TOKEN`: Token để authenticate với SonarQube

2. **Pipeline sẽ tự động:**
   - Chạy tests với coverage
   - Gửi kết quả đến SonarQube
   - Hiển thị coverage trong SonarQube dashboard

## Cấu hình Coverage

### Coverage Format
- **OpenCover**: Được sử dụng chính thức với SonarQube
- **Cobertura**: Format thay thế (có thể chuyển đổi)

### Exclusions
Các file sau sẽ không được tính vào coverage:
- Views (.cshtml)
- Static files (CSS, JS, images)
- Configuration files
- Documentation files

### Coverage Paths
- Source code: `Controllers`, `Models`, `Service`, `Repository`, `Helpers`, `Middleware`, `Hubs`
- Tests: `MovieTheater.Tests`

## Troubleshooting

### Lỗi thường gặp

1. **SonarQube Scanner không tìm thấy:**
   ```bash
   # Kiểm tra PATH
   where sonar-scanner
   
   # Hoặc sử dụng dotnet tool
   dotnet tool list --global
   ```

2. **Coverage files không được tạo:**
   ```bash
   # Kiểm tra coverlet.collector đã được cài đặt
   dotnet list MovieTheater.Tests package
   ```

3. **SonarQube analysis thất bại:**
   - Kiểm tra `SONAR_HOST_URL` và `SONAR_TOKEN`
   - Kiểm tra kết nối mạng đến SonarQube server

### Kiểm tra coverage locally

```bash
# Chạy tests với coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory "TestResults"

# Tạo HTML report (cần ReportGenerator)
reportgenerator -reports:"TestResults/**/coverage.opencover.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## Quality Gate

SonarQube sẽ kiểm tra:
- Code coverage percentage
- Code smells
- Security hotspots
- Bugs
- Technical debt

## Lưu ý

1. **Performance**: Coverage analysis có thể làm chậm build process
2. **Storage**: Coverage files có thể lớn, nên cleanup định kỳ
3. **Security**: Không commit `SONAR_TOKEN` vào repository
4. **Maintenance**: Cập nhật SonarQube Scanner định kỳ

## Liên kết hữu ích

- [SonarQube Documentation](https://docs.sonarqube.org/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [.NET SonarQube Scanner](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-msbuild/) 