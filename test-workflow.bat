@echo off
echo Testing CI/CD Workflow Locally...
echo.

echo Step 1: Restore dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore dependencies
    exit /b 1
)

echo Step 2: Build project...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to build project
    exit /b 1
)

echo Step 3: Run tests with coverage...
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory "TestResults" --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to run tests
    exit /b 1
)

echo Step 4: Check if coverage files exist...
if exist "TestResults\**\coverage.opencover.xml" (
    echo SUCCESS: Coverage files found
) else (
    echo WARNING: No coverage files found
)

echo.
echo Workflow test completed successfully!
pause 