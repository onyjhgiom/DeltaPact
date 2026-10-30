@echo off
setlocal
set "mori_game=%~dp0Builds\Windows\MoriEchoes_20261029_173048_021\MoriEchoes.exe"
if not exist "%mori_game%" (
  echo Verified game build was not found. See docs/BOSS_PURSUIT_AND_TARGETING_ACCEPTANCE.md.
  pause
  exit /b 1
)
start "" "%mori_game%" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11
endlocal
