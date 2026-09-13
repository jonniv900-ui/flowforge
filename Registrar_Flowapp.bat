@echo off
setlocal EnableExtensions
title FlowForge - Associar arquivos .flowapp

set "ROOT=%~dp0"
set "STUDIO=%ROOT%FlowForgeStudio\bin\Release\FlowForgeStudio.exe"
set "EDU=%ROOT%FlowForgeEducation\bin\Release\FlowForgeEducation.exe"
set "PROJECTICON=%ROOT%FlowForgeProject.ico"

if not exist "%STUDIO%" (
  echo.
  echo ERRO: FlowForgeStudio.exe nao foi encontrado em:
  echo "%STUDIO%"
  echo.
  echo Compile primeiro a versao Release executando build_all.bat.
  echo Depois execute este script novamente.
  echo.
  pause
  exit /b 1
)
if not exist "%PROJECTICON%" (
  echo ERRO: FlowForgeProject.ico nao foi encontrado.
  pause
  exit /b 1
)

echo Registrando .flowapp para o usuario atual...
reg add "HKCU\Software\Classes\.flowapp" /ve /d "FlowForge.Project" /f >nul
reg add "HKCU\Software\Classes\.flowapp" /v "Content Type" /d "application/x-flowforge-project" /f >nul
reg add "HKCU\Software\Classes\FlowForge.Project" /ve /d "Projeto FlowForge" /f >nul
reg add "HKCU\Software\Classes\FlowForge.Project" /v "FriendlyTypeName" /d "Projeto FlowForge (.flowapp)" /f >nul
reg add "HKCU\Software\Classes\FlowForge.Project\DefaultIcon" /ve /d "%PROJECTICON%,0" /f >nul
reg add "HKCU\Software\Classes\FlowForge.Project\shell\open" /ve /d "Abrir no FlowForge Studio" /f >nul
reg add "HKCU\Software\Classes\FlowForge.Project\shell\open\command" /ve /d "\"%STUDIO%\" \"%%1\"" /f >nul

if exist "%EDU%" (
  reg add "HKCU\Software\Classes\FlowForge.Project\shell\education" /ve /d "Abrir no FlowForge Education" /f >nul
  reg add "HKCU\Software\Classes\FlowForge.Project\shell\education" /v "Icon" /d "%EDU%,0" /f >nul
  reg add "HKCU\Software\Classes\FlowForge.Project\shell\education\command" /ve /d "\"%EDU%\" \"%%1\"" /f >nul
)

reg add "HKCU\Software\Classes\Applications\FlowForgeStudio.exe\SupportedTypes" /v ".flowapp" /d "" /f >nul

echo.
echo Associacao concluida.
echo Duplo clique em .flowapp agora abre o projeto no FlowForge Studio.
echo.
echo Se o Explorer mantiver o icone antigo, reinicie o Explorer ou entre novamente no Windows.
echo.
pause
