@echo off
:: Batch script to copy files from specific folders to target directories

:: Get the current username
set "UserName=%UserName%"

:: Define source and target directories
set "X=DPredict"
set "Y=C:\Users\%UserName%\AppData\Roaming\Grasshopper\Libraries\DPredict"

:: Create target directories if they don't exist
if not exist "%Y%" mkdir "%Y%"

:: Copy files from X to Y
echo Copying files from "%X%" to "%Y%"...
xcopy "%X%\*" "%Y%\" /E /H /C /I
if %ERRORLEVEL% EQU 0 (
    echo Files successfully copied to "%Y%".
) else (
    echo Error occurred while copying files to "%Y%".
)


:: Done
echo All operations completed.
pause
