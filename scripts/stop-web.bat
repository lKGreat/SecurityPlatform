@echo off
setlocal EnableExtensions

taskkill /F /IM Atlas.WebApi.exe >nul 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "$processes = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -and ($_.CommandLine -match 'src\\frontend\\Atlas\.WebApp|node_modules\\vite\\bin\\vite\.js|npm run dev') }; if ($processes) { $processes | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }; Write-Host ('Stopped frontend process(es): ' + $processes.Count) } else { Write-Host 'No matching frontend processes found.' }"

endlocal
