@echo off
if "%~1"=="" (
    echo Usage: %~nx0 [PolicyDefinitions_path]
    exit /b 1
)
set "policy_dir=%~1"

pushd "%~dp0"

if not exist outputs mkdir outputs

setlocal enabledelayedexpansion

for %%F in ("%policy_dir%\*.admx") do (
    set "filename=%%~nF"
    admxcodegen !filename! "%%F" "%~dp0outputs\!filename!" --generate-csproj "!filename!Test" --generate-linqpad "!filename!LinqpadTest" --generate-buildlog
)

pause
:exit
popd
@echo on
