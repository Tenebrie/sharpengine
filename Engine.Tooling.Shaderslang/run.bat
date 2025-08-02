@echo off
REM build with detailed log
dotnet build "%~dp0Engine.Tooling.Shaderslang.csproj"
if errorlevel 1 (
  echo Build failed, see build.log
  exit /b 1
)
REM launch the freshly-built DLL
dotnet "%~dp0bin\Debug\net9.0\Engine.Tooling.Shaderslang.dll"