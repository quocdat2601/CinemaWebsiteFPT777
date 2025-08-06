@echo off
REM Change to the root of the repo (this script should be placed at team03 level)
cd /d %~dp0

REM Run tests with coverage from the test project
dotnet test MovieTheater.Tests\MovieTheater.Tests.csproj --collect:"XPlat Code Coverage"

REM Use specific path to look for coverage result in the test project only
reportgenerator -reports:"MovieTheater.Tests\TestResults\**\coverage.cobertura.xml" -targetdir:"MovieTheater.Tests\coverage-report" -reporttypes:Html;TextSummary

REM Open the report if it was generated
IF EXIST MovieTheater.Tests\coverage-report\index.html (
    start MovieTheater.Tests\coverage-report\index.html
) ELSE (
    echo Coverage report not found. Please check for errors above.
    pause
)