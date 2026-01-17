@echo off
echo ========================================
echo P&ID Standardization Application
echo Build and Deploy Script
echo ========================================
echo.

REM Clean previous builds
echo Step 1: Cleaning previous builds...
if exist "Published" rmdir /s /q "Published"
if exist "Deployment_Package\Installer_Output" rmdir /s /q "Deployment_Package\Installer_Output"

REM Build the solution
echo.
echo Step 2: Building solution in Release mode...
dotnet build PIDStandardization\PIDStandardization.sln -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

REM Publish the UI application
echo.
echo Step 3: Publishing UI application...
dotnet publish "PIDStandardization\PIDStandardization.UI\PIDStandardization.UI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "Published"
if %errorlevel% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

REM Copy to Deployment Package
echo.
echo Step 4: Copying files to Deployment_Package...
copy /Y "Published\PIDStandardization.UI.exe" "Deployment_Package\"
xcopy /Y /E /I "PIDStandardization\PIDStandardization.AutoCAD\bin\Release\net8.0-windows\win-x64\*" "Deployment_Package\AutoCAD_Plugin\"

REM Check if Inno Setup is installed
echo.
echo Step 5: Creating installer...
set INNO_PATH="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INNO_PATH% (
    echo Inno Setup found, compiling installer...
    %INNO_PATH% "Deployment_Package\PIDStandardization_Installer.iss"
    if %errorlevel% equ 0 (
        echo.
        echo ========================================
        echo SUCCESS! Deployment package created!
        echo ========================================
        echo.
        echo Installer location:
        echo Deployment_Package\Installer_Output\PIDStandardization_Setup_v1.0.0.exe
        echo.
        echo This file can be distributed to your team!
        echo.
    ) else (
        echo WARNING: Installer compilation failed!
        echo Check Deployment_Package\PIDStandardization_Installer.iss for errors
    )
) else (
    echo WARNING: Inno Setup not found at %INNO_PATH%
    echo.
    echo Deployment files are ready in Deployment_Package folder
    echo but installer was not created.
    echo.
    echo To create installer:
    echo 1. Install Inno Setup from https://jrsoftware.org/isdl.php
    echo 2. Open Deployment_Package\PIDStandardization_Installer.iss
    echo 3. Press F9 to compile
)

echo.
echo ========================================
echo Build and Deploy Complete!
echo ========================================
echo.
pause
