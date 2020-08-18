@echo off
set NUGET_PATH=%~dp0\..\..\build\nuget.exe
"%NUGET_PATH%" pack Glacie.Data.Compression.runtime.nuspec -OutputDirectory bin
"%NUGET_PATH%" pack Glacie.Data.Compression.runtime.win-x64.nuspec -OutputDirectory bin
"%NUGET_PATH%" pack Glacie.Data.Compression.runtime.win-x86.nuspec -OutputDirectory bin
