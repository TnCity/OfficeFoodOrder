@echo off
echo ===================================================
echo   OfficeBite - Starting Backend API (Port 5052)
echo ===================================================
echo Listening on http://0.0.0.0:5052 ...
dotnet run --project "%~dp0OfficeBite\OfficeBite.API\OfficeBite.API.csproj" --urls "http://0.0.0.0:5052"
pause
