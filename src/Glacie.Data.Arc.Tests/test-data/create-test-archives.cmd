@echo off

call "%~dp0\create-test-archives-impl.cmd" "%~dp0\..\..\..\.tools\tq\ArchiveTool.exe" tq
if "%ERRORLEVEL%" neq "0" goto error

call "%~dp0\create-test-archives-impl.cmd" "%~dp0\..\..\..\.tools\gd\ArchiveTool.exe" gd
if "%ERRORLEVEL%" neq "0" goto error

exit /b 0

:error
echo Error.
exit /b 1
