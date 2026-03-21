@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
call "%~dp0stop-web.bat"

timeout /t 2 /nobreak >nul

echo Starting backend...
start "Atlas.WebApi" cmd /k "cd /d ""%ROOT%"" && dotnet run --project src\backend\Atlas.WebApi"

echo Starting frontend...
start "Atlas.WebApp" cmd /k "cd /d ""%ROOT%\src\frontend\Atlas.WebApp"" && npm run dev"

echo.
echo Both services are starting in separate windows.
endlocal
