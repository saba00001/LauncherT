@echo off
echo ===================================
echo HRP Launcher Build Script v2.0
echo ===================================
echo.

echo [1/5] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo ✓ Packages restored successfully
echo.

echo [2/5] Building Debug version...
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo ERROR: Debug build failed
    pause
    exit /b 1
)
echo ✓ Debug build completed
echo.

echo [3/5] Building Release version...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Release build failed
    pause
    exit /b 1
)
echo ✓ Release build completed
echo.

echo [4/5] Creating single-file executable...
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o "./dist"
if %errorlevel% neq 0 (
    echo ERROR: Publishing failed
    pause
    exit /b 1
)
echo ✓ Single-file executable created in ./dist folder
echo.

echo [5/5] Creating installer package...
if not exist "installer" mkdir installer
copy "dist\HRP.exe" "installer\"
copy "README.md" "installer\"
echo ✓ Installer package created in ./installer folder
echo.

echo ===================================
echo Build completed successfully!
echo ===================================
echo.
echo Files created:
echo - ./dist/HRP.exe (Single-file executable)
echo - ./installer/ (Distribution package)
echo.
echo You can now distribute the HRP.exe file.
echo.
pause