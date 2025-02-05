@echo off
:: Batch script to copy files from specific folders to target directories

"C:\Program Files\Rhino 7\System\Yak.exe" install .\sunfish-0.7.0-beta-rh7_20-win.yak
"C:\Program Files\Rhino 7\System\Yak.exe" install .\dpredict-1.5.0-alpha-rh7_32-win.yak

if %ERRORLEVEL% EQU 0 (
    echo 
) else (
    echo An error occurred.
)

:: Done
echo All operations completed.
pause
