@echo off
echo ========================================
echo P&ID Standardization Application
echo Installation Script
echo ========================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click INSTALL.bat and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

echo Step 1: Installing Main Application...
echo.

REM Create installation directory
set INSTALL_DIR=C:\Program Files\PIDStandardization
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

REM Copy main application
echo Copying PIDStandardization.UI.exe to %INSTALL_DIR%...
copy /Y "PIDStandardization.UI.exe" "%INSTALL_DIR%\"

REM Copy AutoCAD Plugin
set PLUGIN_DIR=%INSTALL_DIR%\AutoCAD_Plugin
if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"
echo Copying AutoCAD Plugin files...
xcopy /Y /E /I "AutoCAD_Plugin\*" "%PLUGIN_DIR%\"

echo.
echo Step 2: Setting up AutoCAD 2026 Auto-load...
echo.

REM Create AutoCAD support directory path
set AUTOCAD_SUPPORT=%APPDATA%\Autodesk\AutoCAD 2026\R24.1\enu\Support

REM Check if AutoCAD support directory exists
if not exist "%AUTOCAD_SUPPORT%" (
    echo WARNING: AutoCAD 2026 support directory not found.
    echo The plugin will need to be loaded manually using NETLOAD.
    echo Location: %AUTOCAD_SUPPORT%
    echo.
    goto :CREATE_SHORTCUT
)

REM Create acad2026.lsp file for auto-loading
set LSP_FILE=%AUTOCAD_SUPPORT%\acad2026.lsp

echo Creating AutoCAD startup file: %LSP_FILE%
(
echo ; P^&ID Standardization Plugin Auto-load
echo ; This file automatically loads the PIDStandardization plugin when AutoCAD starts
echo ^(defun S::STARTUP ^(^)
echo   ^(command "._NETLOAD" "C:\\Program Files\\PIDStandardization\\AutoCAD_Plugin\\PIDStandardization.AutoCAD.dll"^)
echo   ^(princ "\nP^&ID Standardization plugin loaded successfully! Type PIDINFO for info.\n"^)
echo   ^(princ^)
echo ^)
) > "%LSP_FILE%"

echo AutoCAD auto-load configured successfully!
echo.

:CREATE_SHORTCUT
echo Step 3: Creating Desktop Shortcut...
echo.

REM Create desktop shortcut using PowerShell
powershell -Command "$WScriptShell = New-Object -ComObject WScript.Shell; $Shortcut = $WScriptShell.CreateShortcut('%USERPROFILE%\Desktop\P&ID Standardization.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\PIDStandardization.UI.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Description = 'P&ID Standardization Application'; $Shortcut.Save()"

echo Desktop shortcut created!
echo.

echo ========================================
echo Installation Complete!
echo ========================================
echo.
echo Main Application installed to: %INSTALL_DIR%
echo Desktop shortcut created: P^&ID Standardization
echo.
echo AutoCAD Plugin:
if exist "%LSP_FILE%" (
    echo   [OK] Will auto-load when AutoCAD 2026 starts
) else (
    echo   [MANUAL] Load using NETLOAD command in AutoCAD
    echo   Path: %PLUGIN_DIR%\PIDStandardization.AutoCAD.dll
)
echo.
echo Next Steps:
echo 1. Double-click "P^&ID Standardization" on your desktop to launch the application
echo 2. Create your first project
echo 3. Import P^&ID drawings
echo 4. Open AutoCAD 2026 - the plugin will load automatically
echo 5. Use PIDEXTRACTDB command to extract equipment
echo.
echo For help, refer to README.txt
echo.
pause
