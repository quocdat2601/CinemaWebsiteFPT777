@echo off
REM Change to the directory where this script is located
cd /d %~dp0

dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**\TestResults\**\coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html;TextSummary

IF EXIST coverage-report\index.html (
    start coverage-report\index.html
) ELSE (
    echo Coverage report not found. Please check for errors above.
    pause
)