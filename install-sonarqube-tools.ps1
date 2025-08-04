# Script cài đặt tools cần thiết cho SonarQube Analysis
Write-Host "Installing SonarQube Analysis Tools..." -ForegroundColor Green
Write-Host ""

# Kiểm tra .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: .NET SDK not found" -ForegroundColor Red
    Write-Host "Please install .NET SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Cài đặt SonarQube Scanner
Write-Host "Installing SonarQube Scanner..." -ForegroundColor Cyan
try {
    dotnet tool install --global dotnet-sonarscanner
    Write-Host "✓ SonarQube Scanner installed successfully" -ForegroundColor Green
} catch {
    Write-Host "⚠ WARNING: Failed to install SonarQube Scanner via dotnet tool" -ForegroundColor Yellow
    Write-Host "Please install manually from: https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/" -ForegroundColor Yellow
}

# Cài đặt ReportGenerator
Write-Host "Installing ReportGenerator..." -ForegroundColor Cyan
try {
    dotnet tool install --global dotnet-reportgenerator-globaltool
    Write-Host "✓ ReportGenerator installed successfully" -ForegroundColor Green
} catch {
    Write-Host "⚠ WARNING: Failed to install ReportGenerator" -ForegroundColor Yellow
}

# Kiểm tra coverlet.collector trong test project
Write-Host "Checking coverlet.collector in test project..." -ForegroundColor Cyan
try {
    $coverletInstalled = dotnet list MovieTheater.Tests package | Select-String "coverlet.collector"
    if ($coverletInstalled) {
        Write-Host "✓ coverlet.collector is already installed" -ForegroundColor Green
    } else {
        Write-Host "⚠ WARNING: coverlet.collector not found in test project" -ForegroundColor Yellow
        Write-Host "Please add it to MovieTheater.Tests.csproj:" -ForegroundColor Yellow
        Write-Host '<PackageReference Include="coverlet.collector" Version="6.0.4">' -ForegroundColor Gray
        Write-Host '  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>' -ForegroundColor Gray
        Write-Host '  <PrivateAssets>all</PrivateAssets>' -ForegroundColor Gray
        Write-Host '</PackageReference>' -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠ WARNING: Could not check coverlet.collector" -ForegroundColor Yellow
}

# Kiểm tra SonarQube Scanner
Write-Host "Checking SonarQube Scanner..." -ForegroundColor Cyan
try {
    $sonarScanner = Get-Command sonar-scanner -ErrorAction Stop
    Write-Host "✓ SonarQube Scanner found at: $($sonarScanner.Source)" -ForegroundColor Green
} catch {
    Write-Host "⚠ WARNING: SonarQube Scanner not found in PATH" -ForegroundColor Yellow
    Write-Host "You may need to add it to your PATH or use dotnet sonarscanner instead" -ForegroundColor Yellow
}

# Kiểm tra ReportGenerator
Write-Host "Checking ReportGenerator..." -ForegroundColor Cyan
try {
    $reportGenerator = Get-Command reportgenerator -ErrorAction Stop
    Write-Host "✓ ReportGenerator found at: $($reportGenerator.Source)" -ForegroundColor Green
} catch {
    Write-Host "⚠ WARNING: ReportGenerator not found in PATH" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installation Summary:" -ForegroundColor Green
Write-Host "====================" -ForegroundColor Green

# Hiển thị danh sách tools đã cài đặt
Write-Host "Installed tools:" -ForegroundColor Cyan
dotnet tool list --global

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Green
Write-Host "1. Set up SonarQube server and get your token" -ForegroundColor Cyan
Write-Host "2. Configure SONAR_HOST_URL and SONAR_TOKEN in GitLab CI/CD variables" -ForegroundColor Cyan
Write-Host "3. Run 'sonar-analysis.ps1' to test locally" -ForegroundColor Cyan
Write-Host "4. Push changes to trigger GitLab CI/CD pipeline" -ForegroundColor Cyan

Write-Host ""
Write-Host "✓ Installation completed!" -ForegroundColor Green
Read-Host "Press Enter to exit" 