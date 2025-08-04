@echo off
echo Starting SonarQube Analysis with Coverage...
echo.

REM Check if SonarQube Scanner is installed
where sonar-scanner >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: SonarQube Scanner is not installed or not in PATH
    echo Please install SonarQube Scanner from: https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/
    pause
    exit /b 1
)

echo Step 1: Clean previous test results...
if exist "TestResults" rmdir /s /q "TestResults"
if exist "MovieTheater.Tests\TestResults" rmdir /s /q "MovieTheater.Tests\TestResults"

echo Step 2: Restore dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore dependencies
    pause
    exit /b 1
)

echo Step 3: Build project...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to build project
    pause
    exit /b 1
)

echo Step 4: Run tests with coverage...
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory "TestResults" --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to run tests
    pause
    exit /b 1
)

echo Step 5: Check coverage files...
if exist "TestResults\**\coverage.opencover.xml" (
    echo SUCCESS: Coverage files found
    for /r "TestResults" %%f in (coverage.opencover.xml) do (
        echo Found coverage file: %%f
    )
) else (
    echo WARNING: No coverage files found
    echo Checking for alternative coverage formats...
    if exist "TestResults\**\coverage.cobertura.xml" (
        echo Found Cobertura coverage file
    )
)

echo Step 6: Run SonarQube analysis...
sonar-scanner
if %errorlevel% neq 0 (
    echo ERROR: SonarQube analysis failed
    pause
    exit /b 1
)

echo.
echo SUCCESS: SonarQube analysis completed with coverage!
echo Check your SonarQube dashboard for results.
pause 