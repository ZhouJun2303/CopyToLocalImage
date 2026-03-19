@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo   CopyToLocalImage - 一键打包脚本
echo ========================================
echo.

cd /d "%~dp0CopyToLocalImage"

echo [1/4] 清理旧文件...
dotnet clean --configuration Release >nul 2>&1
if exist "bin\Release\net8.0-windows" rmdir /s /q "bin\Release\net8.0-windows"

echo [2/4] 编译 Release 版本...
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 编译失败！
    pause
    exit /b 1
)

echo [3/4] 复制必要文件...
copy /Y "app.ico" "bin\Release\net8.0-windows\" >nul

echo [4/4] 创建发布目录...
cd ".."
set "BUILD_DIR=publish"
set "PUBLISH_DIR=CopyToLocalImage\bin\Release\net8.0-windows"

if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%BUILD_DIR%"

xcopy /E /I /Y "%PUBLISH_DIR%\*.dll" "%BUILD_DIR%\" >nul
xcopy /E /I /Y "%PUBLISH_DIR%\*.exe" "%BUILD_DIR%\" >nul
xcopy /E /I /Y "%PUBLISH_DIR%\*.json" "%BUILD_DIR%\" >nul
xcopy /E /I /Y "%PUBLISH_DIR%\*.deps.json" "%BUILD_DIR%\" >nul
xcopy /E /I /Y "%PUBLISH_DIR%\*.runtimeconfig.json" "%BUILD_DIR%\" >nul

echo.
echo ========================================
echo   打包完成!
echo   输出目录：%CD%\%BUILD_DIR%
echo ========================================
echo.

start "%BUILD_DIR%" explorer "%CD%\%BUILD_DIR%"
