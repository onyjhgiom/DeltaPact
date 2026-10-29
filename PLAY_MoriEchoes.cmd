@echo off
setlocal
set "mori_game=%~dp0Builds\Windows\MoriEchoes_20261028_171506_913\MoriEchoes.exe"
if not exist "%mori_game%" (
  echo Verified game build was not found. See docs/SWIM_ART_AND_BOSS_TECHNIQUES_ACCEPTANCE.md.
  pause
  exit /b 1
)
start "" "%mori_game%" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11
endlocal
