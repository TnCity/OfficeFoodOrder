@echo off
setlocal enabledelayedexpansion

set ADB="C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"

echo ===================================================
echo   OfficeBite - Deploy to Connected Phone
echo ===================================================

:: Check if ADB sees a device
%ADB% devices | findstr /R /C:"device$" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [!] No device detected. Attempting to connect over WiFi...
    set /p PHONE_PORT="Enter Wireless Debugging Port on phone (or press Enter for 35033): "
    if "!PHONE_PORT!"=="" set PHONE_PORT=35033
    %ADB% connect 192.168.0.44:!PHONE_PORT!
)

:: Get the first online device ID to avoid "more than one device/emulator" error
set DEVICE_ID=
for /f "tokens=1" %%d in ('%ADB% devices ^| findstr /R /C:"device$"') do (
    if "!DEVICE_ID!"=="" set DEVICE_ID=%%d
)

if "!DEVICE_ID!"=="" (
    echo.
    echo [ERROR] Phone is not connected.
    echo Please make sure:
    echo  1. Phone screen is unlocked.
    echo  2. Wireless Debugging is turned ON in Developer Options.
    echo.
    pause
    exit /b 1
)

echo Target Device: !DEVICE_ID!

echo.
echo [1/3] Building Android APK...
dotnet build "%~dp0OfficeBite.Mobile\OfficeBite.Mobile.csproj" -f net9.0-android
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed! Check errors above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Installing APK to phone (!DEVICE_ID!)...
%ADB% -s !DEVICE_ID! install -r "%~dp0OfficeBite.Mobile\bin\Debug\net9.0-android\com.companyname.officebite.mobile-Signed.apk"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Installation failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Launching App on phone...
%ADB% -s !DEVICE_ID! shell monkey -p com.companyname.officebite.mobile -c android.intent.category.LAUNCHER 1 >nul 2>&1

echo.
echo ===================================================
echo   SUCCESS! App is updated and opened on your phone.
echo ===================================================
pause

