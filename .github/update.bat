@echo off
setlocal

:: 获取管理员权限
net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo Stopping OmenSuperHub...

taskkill /F /IM OmenSuperHub.exe >nul 2>&1

timeout /t 2 >nul

if exist OmenSuperHub.exe (
    ren OmenSuperHub.exe OmenSuperHub.exe.old
)

echo Downloading update...

powershell -Command ^
"$ProgressPreference='SilentlyContinue'; Invoke-WebRequest -Uri 'https://v6.gh-proxy.org/https://github.com/krisstibex/OmenSuperHub_github_ci_build/releases/latest/download/OmenSuperHub.exe' -OutFile 'OmenSuperHub.exe'"

if not exist OmenSuperHub.exe (
    echo Download failed
    pause
    exit /b 1
)

echo Starting new version...

start "" OmenSuperHub.exe

timeout /t 3 >nul

tasklist | find /i "OmenSuperHub.exe" >nul

if %errorlevel%==0 (
    echo Update successful
    del /f /q OmenSuperHub.exe.old >nul 2>&1
) else (
    echo Update failed, restoring old version

    del /f /q OmenSuperHub.exe >nul 2>&1

    if exist OmenSuperHub.exe.old (
        ren OmenSuperHub.exe.old OmenSuperHub.exe
    )

    start "" OmenSuperHub.exe
)

exit /b
