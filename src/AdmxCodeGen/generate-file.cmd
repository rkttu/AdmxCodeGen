@echo off
setlocal enabledelayedexpansion

:: Check if a file path was provided
if "%~1"=="" (
    echo Usage: %~nx0 [path_to_admx_file]
    echo Example: %~nx0 C:\Windows\PolicyDefinitions\MSS.admx
    echo          %~nx0 .\MyPolicies\Custom.admx
    exit /b 1
)

:: Set variables
set "input_file=%~f1"
set "filename=%~n1"
set "output_dir=%~dp0outputs"
set "output_base=%output_dir%\%filename%"

:: Check if the input file exists and has .admx extension
if not exist "%input_file%" (
    echo Error: The file %input_file% does not exist.
    exit /b 1
)
if /i not "%~x1"==".admx" (
    echo Error: The input file must have a .admx extension.
    exit /b 1
)

:: Create outputs directory if it doesn't exist
if not exist "%output_dir%" mkdir "%output_dir%"

:: Run the dotnet command
echo Processing %filename%.admx...
dotnet run -- "%filename%" "%input_file%" "%output_base%" --generate-csproj "%filename%Test" --generate-linqpad "%filename%LinqpadTest" --generate-buildlog

if !errorlevel! neq 0 (
    echo Error: dotnet command failed with error code !errorlevel!
    pause
    exit /b !errorlevel!
)

echo Processing completed successfully.
echo Output files are located in: %output_dir%

pause
exit /b 0
