@echo off
setlocal

:: --------------------------------------------------------
:: CONFIGURATION
:: --------------------------------------------------------
:: The name of the folder as it will appear in RimWorld
set MOD_NAME=VenomTweaks

:: Your local RimWorld Mods directory
set TARGET_DIR=D:\SteamLibrary\steamapps\common\RimWorld\Mods\%MOD_NAME%

:: --------------------------------------------------------
:: DEPLOYMENT
:: --------------------------------------------------------
echo Deploying %MOD_NAME% to local RimWorld installation...

:: Create the target directory if it doesn't exist
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

:: Robocopy About folder
robocopy "%~dp0About" "%TARGET_DIR%\About" /E /NFL /NDL /NJH /NJS /nc /ns /np

:: Robocopy Assemblies folder (excludes debug .pdb files)
robocopy "%~dp0Assemblies" "%TARGET_DIR%\Assemblies" /E /XF *.pdb /NFL /NDL /NJH /NJS /nc /ns /np

echo Copy complete!

:: --------------------------------------------------------
:: ZIPPING FOR DISTRIBUTION
:: --------------------------------------------------------
echo Zipping mod for distribution...

:: The zip file will be created in your root project folder
set ZIP_PATH=%~dp0%MOD_NAME%.zip

:: Delete old zip if it exists to prevent errors
if exist "%ZIP_PATH%" del "%ZIP_PATH%"

:: Call powershell to compress the target directory
powershell.exe -NoProfile -Command "Compress-Archive -Path '%TARGET_DIR%\*' -DestinationPath '%ZIP_PATH%' -Force"

echo Deployment and Zipping Successful!
pause