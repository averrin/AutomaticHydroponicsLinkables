@echo off
echo ========================================
echo    Building Distribution Folder
echo ========================================
echo.

set CURRENT_DIR=%CD%
for /f "delims=" %%a in ('powershell -NoProfile -Command "([xml](Get-Content '%CURRENT_DIR%\About\About.xml')).ModMetaData.name"') do set MOD_NAME=%%a
for /f "delims=" %%a in ('powershell -NoProfile -Command "([xml](Get-Content '%CURRENT_DIR%\About\About.xml')).ModMetaData.packageId"') do set PACKAGE_ID=%%a
set DLL_NAME=%PACKAGE_ID:.=_%
set DIST_DIR=%CURRENT_DIR%\dist

if not exist "mod-description.txt" (
    echo ERROR: mod-description.txt not found!
    echo Please create this file with your mod description.
    exit /b 1
)

if exist "%DIST_DIR%" (
    echo Removing existing dist folder...
    rmdir /s /q "%DIST_DIR%"
)

echo Creating dist folder...
mkdir "%DIST_DIR%"

echo Copying About folder...
xcopy "About" "%DIST_DIR%\About" /E /I /Y >nul

echo Building Release...
for /f "delims=" %%a in ('dir /b "%CURRENT_DIR%\Source\*.csproj"') do set CSPROJ=%%a
powershell -NoProfile -ExecutionPolicy Bypass -Command "dotnet build '%CURRENT_DIR%\Source\%CSPROJ%' -c Release" || exit /b 1

if exist "1.6" (
    echo Copying 1.6 folder...
    xcopy "1.6" "%DIST_DIR%\1.6" /E /I /Y >nul
    if exist "Source\bin\Release\net48\%DLL_NAME%.dll" (
        echo Copying Release DLL...
        copy "Source\bin\Release\net48\%DLL_NAME%.dll" "%DIST_DIR%\1.6\Assemblies\%DLL_NAME%.dll" >nul
    )
)

rem Optional content folders copied if present
for %%F in (Assemblies Defs Patches Source Textures) do (
    if exist "%%F" (
        echo Copying %%F folder...
        xcopy "%%F" "%DIST_DIR%\%%F" /E /I /Y >nul
    )
)

rem Clean non-image files from dist/Textures (keep only .png and .dds)
if exist "%DIST_DIR%\Textures" (
    echo Cleaning non-image files from dist/Textures...
    for /R "%DIST_DIR%\Textures" %%G in (*) do (
        for %%H in ("%%~xG") do (
            if /I not "%%~H"==".png" if /I not "%%~H"==".dds" del /q "%%G"
        )
    )
)

if exist "README.md" copy "README.md" "%DIST_DIR%\README.md" >nul

echo.
echo ========================================
echo    Enter Changelog Note
echo ========================================
set /p CHANGENOTE="Changelog note: "

set WORKSHOP_ID=
if exist "About\PublishedFileId.txt" (
    for /f "delims=" %%i in (About\PublishedFileId.txt) do set WORKSHOP_ID=%%i
)

echo Updating workshop.vdf...

REM Validate preview image exists to avoid PS crash
if not exist "%CURRENT_DIR%\About\Preview.png" (
    echo ERROR: About\Preview.png not found. Please add a preview image before publishing.
    exit /b 1
)

REM Use the proven updater and also force absolute content/preview paths
powershell -NoProfile -ExecutionPolicy Bypass -Command "$content = Get-Content 'workshop.vdf' -Raw; $desc = Get-Content 'mod-description.txt' -Raw; $cf = (Resolve-Path '%DIST_DIR%').Path; $pf = (Resolve-Path '%DIST_DIR%\About\Preview.png').Path; $content = $content -replace '\"changenote\"\s+\".*\"', '\"changenote\"  \"%CHANGENOTE%\"'; $content = $content -replace '\"description\"\s+\".*\"', ('\"description\"  \"' + $desc.Trim() + '\"'); $content = $content -replace '\"contentfolder\"\s+\".*\"', ('\"contentfolder\"  \"' + $cf + '\"'); $content = $content -replace '\"previewfile\"\s+\".*\"', ('\"previewfile\"  \"' + $pf + '\"'); if ('%WORKSHOP_ID%' -ne '') { $content = $content -replace '\"publishedfileid\"\s+\".*\"', '\"publishedfileid\"  \"%WORKSHOP_ID%\"' }; Set-Content 'workshop.vdf' $content -Encoding UTF8"

echo Done. Dist at %DIST_DIR%

