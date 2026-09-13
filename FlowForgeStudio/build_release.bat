@echo off
setlocal
set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
  for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    if not defined MSBUILD set "MSBUILD=%%i"
  )
)

if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2026\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2026\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2026\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2026\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2026\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2026\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

if not defined MSBUILD (
  echo MSBuild nao encontrado.
  echo Instale o Visual Studio 2026 com Desenvolvimento para desktop com .NET.
  echo Confirme tambem o Developer Pack/Targeting Pack do .NET Framework 4.8.
  pause
  exit /b 1
)
"%MSBUILD%" FlowForgeStudio.vbproj /t:Restore,Build /p:Configuration=Release /m
if errorlevel 1 pause & exit /b 1
echo.
echo Executavel: bin\Release\FlowForgeStudio.exe
pause
