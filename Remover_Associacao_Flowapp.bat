@echo off
setlocal EnableExtensions
title FlowForge - Remover associacao .flowapp

echo Removendo associacao .flowapp do usuario atual...
reg delete "HKCU\Software\Classes\.flowapp" /f >nul 2>&1
reg delete "HKCU\Software\Classes\FlowForge.Project" /f >nul 2>&1
reg delete "HKCU\Software\Classes\Applications\FlowForgeStudio.exe\SupportedTypes" /v ".flowapp" /f >nul 2>&1

echo.
echo Associacao removida.
echo.
pause
