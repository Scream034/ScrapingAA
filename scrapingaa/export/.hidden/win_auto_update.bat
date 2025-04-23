set projectName=%1
set fromPath=%2
set moveToPath=%3

echo Project Name: %projectName%
echo Move To Path: %moveToPath%

timeout /t 3 /nobreak > nul

move %fromPath%\data_%projectName%_windows_x86_64 %moveToPath%
move %fromPath%\release.console.exe %moveToPath%
move %fromPath%\release.exe %moveToPath%

call %moveToPath%\release.console.exe

pause