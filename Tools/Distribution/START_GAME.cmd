@echo off
setlocal
pushd "%~dp0"
if not exist "MoriEchoes.exe" goto missing
if not exist "UnityPlayer.dll" goto missing
if not exist "MoriEchoes_Data\globalgamemanagers" goto missing
start "" /d "%~dp0" "%~dp0MoriEchoes.exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11
popd
exit /b 0
:missing
echo Please extract the entire ZIP to a folder, then run START_GAME.cmd again.
echo Keep MoriEchoes.exe, UnityPlayer.dll, MoriEchoes_Data and MonoBleedingEdge together.
pause
popd
exit /b 1
