@echo off
setlocal EnableExtensions

REM ============================================================
REM  OBS Game Bar Widget - Build, Sign, Install & Launch
REM ============================================================

set "ROOT=%~dp0"
set "SLN=%ROOT%OBSGameBar.sln"
set "MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
set "PLATFORM=x64"
set "CONFIG=Debug"
set "PKGDIR=%ROOT%OBSGameBar\AppPackages\OBSGameBar_1.0.0.0_%PLATFORM%_%CONFIG%_Test"
set "MSIX=%PKGDIR%\OBSGameBar_1.0.0.0_%PLATFORM%_%CONFIG%.msix"

REM --- Locate signtool (newest Windows SDK) ---
set "SIGNTOOL="
for /f "delims=" %%D in ('dir /b /ad /o-n "C:\Program Files (x86)\Windows Kits\10\bin" 2^>nul') do (
    if not defined SIGNTOOL if exist "C:\Program Files (x86)\Windows Kits\10\bin\%%D\x64\signtool.exe" set "SIGNTOOL=C:\Program Files (x86)\Windows Kits\10\bin\%%D\x64\signtool.exe"
)

echo.
echo [1/4] Building solution (%CONFIG% ^| %PLATFORM%)...
if not exist "%MSBUILD%" (
    echo ERROR: MSBuild not found at "%MSBUILD%"
    pause & exit /b 1
)
"%MSBUILD%" "%SLN%" /p:Configuration=%CONFIG% /p:Platform=%PLATFORM% /m /v:m /nologo
if errorlevel 1 (
    echo ERROR: Build failed.
    pause & exit /b 1
)

echo.
echo [2/4] Signing package with the trusted OBS Game Bar developer certificate...
if not exist "%SIGNTOOL%" (
    echo ERROR: signtool.exe not found. Install the Windows SDK.
    pause & exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%sign.ps1" "%MSIX%" "%SIGNTOOL%"
if errorlevel 1 (
    echo ERROR: Signing failed. See sign.ps1 - a valid OBS Game Bar developer
    echo        certificate must be present in the current user's cert store.
    pause & exit /b 1
)

echo.
echo [3/4] Installing package...
REM Remove any previously installed OBSGameBarWidget package. We look the real package
REM up by name instead of using a hard-coded full name, so this keeps working even if
REM the publisher ID or version changes.
powershell -NoProfile -Command "Get-AppxPackage -Name 'OBSGameBarWidget' -ErrorAction SilentlyContinue | ForEach-Object { Remove-AppxPackage $_.PackageFullName -ErrorAction Continue }; Add-AppxPackage -Path '%MSIX%' -ErrorAction Stop; if ($?) { exit 0 }"
if errorlevel 1 (
    echo ERROR: Package installation failed.
    pause & exit /b 1
)

REM --- Look up the actual installed package full name (powershell runs stand-alone,
REM     so the cmd 'cert:' drive quirk does not affect this) ---
set "FULLNAME="
for /f "delims=" %%N in ('powershell -NoProfile -Command "Get-AppxPackage -Name 'OBSGameBarWidget' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty PackageFullName"') do set "FULLNAME=%%N"
if not defined FULLNAME (
    echo WARNING: Could not determine the installed package full name.
)

echo.
echo [4/4] Granting loopback exemption (needed to reach OBS on 127.0.0.1)...
net session >nul 2>&1
if %errorlevel%==0 (
    CheckNetIsolation LoopbackExempt -a -n="%FULLNAME%"
    echo Loopback exemption applied.
) else (
    echo WARNING: Not running as administrator - loopback exemption NOT applied.
    echo          The widget will NOT be able to connect to OBS. Run this once as admin:
    echo          CheckNetIsolation LoopbackExempt -a -n="%FULLNAME%"
)

echo.
echo Launching widget...
explorer.exe "shell:appsFolder\%FULLNAME%!App"

echo.
echo Done! Press Win+G and open the "OBS Studio" widget.
echo.
pause
