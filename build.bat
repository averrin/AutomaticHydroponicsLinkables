@echo off
setlocal

set SRC=C:\Users\o\Documents\GitHub\AutomaticHydroponicsLinkables
set DST=D:\Games\SteamLibrary\steamapps\common\RimWorld\Mods\AutomaticHydroponicsLinkables
set EXE=D:\Games\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64.exe

rem 1) Build
powershell -NoProfile -ExecutionPolicy Bypass -Command "dotnet build '%SRC%\Source\AutomaticHydroponicsLinkables.csproj' -c Release" || goto :fail

rem 2) Mirror files into Mods (force overwrite)
robocopy "%SRC%" "%DST%" /MIR /NFL /NDL /NJH /NJS /NP
if %ERRORLEVEL% GEQ 8 goto :fail

rem 3) Launch RimWorld
start "" "%EXE%"
exit /b 0

:fail
echo Build or deploy failed.
exit /b 1