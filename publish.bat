@echo off
setlocal
echo ========================================
echo    Steam Workshop Mod Publisher
echo ========================================
echo.
set CURRENT_DIR=%CD%

if not exist "workshop.vdf" (
    echo ERROR: workshop.vdf descriptor file not found
    exit /b 1
)

set STEAMCMD_PATH=C:\Users\o\AppData\Local\RimSort\instances\Default\steamcmd\steamcmd.exe
set STEAM_USERNAME=averrinthemelas
if "%STEAMCMD_PATH%"=="" (
    set STEAMCMD_PATH=%CURRENT_DIR%\tools\steamcmd\steamcmd.exe
)

if not exist "%STEAMCMD_PATH%" (
    echo ERROR: SteamCMD not found. Set STEAMCMD_PATH env var or place steamcmd.exe at %%CD%%\tools\steamcmd\steamcmd.exe
    exit /b 1
)

(> steam_upload.txt (
    echo login %STEAM_USERNAME%
    echo workshop_build_item "%CURRENT_DIR%\workshop.vdf"
    echo quit
))

"%STEAMCMD_PATH%" +runscript "%CURRENT_DIR%\steam_upload.txt"
echo Upload complete.
endlocal
