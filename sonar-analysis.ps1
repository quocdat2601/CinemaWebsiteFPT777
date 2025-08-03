# SonarQube Analysis Script for PowerShell
Write-Host "Starting SonarQube Analysis with Coverage..." -ForegroundColor Green
Write-Host ""

# Check if SonarQube Scanner is installed
try {
    $null = Get-Command sonar-scanner -ErrorAction Stop
    Write-Host "✓ SonarQube Scanner found" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: SonarQube Scanner is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install SonarQube Scanner from: https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/" -ForegroundColor Yellow
    Write-Host "Or use: dotnet tool install --global dotnet-sonarscanner" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 1: Clean previous test results
Write-Host "Step 1: Cleaning previous test results..." -ForegroundColor Cyan
if (Test-Path "TestResults") {
    Remove-Item -Path "TestResults" -Recurse -Force
    Write-Host "✓ Cleaned TestResults directory" -ForegroundColor Green
}
if (Test-Path "MovieTheater.Tests\TestResults") {
    Remove-Item -Path "MovieTheater.Tests\TestResults" -Recurse -Force
    Write-Host "✓ Cleaned MovieTheater.Tests\TestResults directory" -ForegroundColor Green
}

# Step 2: Restore dependencies
Write-Host "Step 2: Restoring dependencies..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ERROR: Failed to restore dependencies" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "✓ Dependencies restored" -ForegroundColor Green

# Step 3: Build project
Write-Host "Step 3: Building project..." -ForegroundColor Cyan
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ERROR: Failed to build project" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "✓ Project built successfully" -ForegroundColor Green

# Step 4: Run tests with coverage
Write-Host "Step 4: Running tests with coverage..." -ForegroundColor Cyan
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory "TestResults" --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ERROR: Failed to run tests" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "✓ Tests completed with coverage" -ForegroundColor Green

# Step 5: Check coverage files
Write-Host "Step 5: Checking coverage files..." -ForegroundColor Cyan
$coverageFiles = Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.opencover.xml"
if ($coverageFiles.Count -gt 0) {
    Write-Host "✓ Coverage files found:" -ForegroundColor Green
    foreach ($file in $coverageFiles) {
        Write-Host "  - $($file.FullName)" -ForegroundColor Gray
    }
} else {
    Write-Host "⚠ WARNING: No OpenCover coverage files found" -ForegroundColor Yellow
    Write-Host "Checking for alternative coverage formats..." -ForegroundColor Cyan
    
    $coberturaFiles = Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.cobertura.xml"
    if ($coberturaFiles.Count -gt 0) {
        Write-Host "✓ Found Cobertura coverage files:" -ForegroundColor Green
        foreach ($file in $coberturaFiles) {
            Write-Host "  - $($file.FullName)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠ No coverage files found at all" -ForegroundColor Yellow
    }
}

# Step 6: Run SonarQube analysis
Write-Host "Step 6: Running SonarQube analysis..." -ForegroundColor Cyan
sonar-scanner
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ERROR: SonarQube analysis failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "✓ SUCCESS: SonarQube analysis completed with coverage!" -ForegroundColor Green
Write-Host "Check your SonarQube dashboard for results." -ForegroundColor Cyan
Read-Host "Press Enter to exit" 