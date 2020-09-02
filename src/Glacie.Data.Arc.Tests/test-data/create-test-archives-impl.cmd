@echo off & setlocal

if "%~1" == "" goto missing_required_parameters
if "%~2" == "" goto missing_required_parameters

set ArchiveTool=%~1
set OutputPrefix=%~2

echo.  ArchiveTool = %ArchiveTool%
echo. OutputPrefix = %OutputPrefix%

:: 0: Empty Archive
call :arc 0 "-add .\empty ."
if "%ERRORLEVEL%" neq "0" goto error
:: echo. "%ArchiveTool%" "%OutPrefix%-0.arc" -add .\empty .

:: 1: Single Empty File
call :arc 1 "-add .\data\empty-file.bin ."
if "%ERRORLEVEL%" neq "0" goto error

:: 2: Single Small File (Uncompressed)
call :arc 2 "-add .\data\small-file.bin . 0"
if "%ERRORLEVEL%" neq "0" goto error

:: 3: Two Medium Files (Compressed)
call :arc 3 "-add .\data\TQ-ArchiveTool-Help.bin . 9"
call :arc 3 "-add .\data\GD-ArchiveTool-Help.bin . 9"
if "%ERRORLEVEL%" neq "0" goto error

:: 4: Many Files With Removals
call :arc 4 "-add .\data\TQ-ArchiveTool-Help.bin . 9"
call :arc 4 "-add .\data\empty-file.bin . 9"
call :arc 4 "-add .\data\small-file.bin . 9"
call :arc 4 "-add .\data\GD-ArchiveTool-Help.bin . 9"
call :arc 4 "-remove .\data\GD-ArchiveTool-Help.bin"
call :arc 4 "-remove .\data\small-file.bin"
call :arc 4 "-remove .\data\empty-file.bin"
if "%ERRORLEVEL%" neq "0" goto error

:: 5: File With MaybeStore Chunk
call :arc 5 "-add .\data\tokens.bin . 9"
if "%ERRORLEVEL%" neq "0" goto error

exit /b 0

:arc
setlocal

echo.Exec: "%ArchiveTool%" "%OutputPrefix%-%~1.arc" %~2
"%ArchiveTool%" "%OutputPrefix%-%~1.arc" %~2
if "%ERRORLEVEL%" neq "0" goto arc_error
endlocal & exit /b 0

:arc_error
echo Error: ArchiveTool exited with code %ERRORLEVEL%.
endlocal & exit /b 1

:missing_required_parameters
echo Error: missing required parameters.
endlocal & exit /b 1

:error
endlocal & exit /b 1
