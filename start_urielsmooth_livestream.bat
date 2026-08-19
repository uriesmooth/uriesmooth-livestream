@echo off
title Urielsmooth-livestream Launcher
echo ===================================================
echo [INITIALIZING] Urielsmooth-livestream Environment
echo ===================================================

:: Step 1: Boot the Python-MediaPipe Tracking Bridge
echo [1/3] Launching Local MediaPipe Tracking Bridge...
start /min "MediaPipe Bridge" python "%~dp0mediapipe_bridge.py"
timeout /t 3 /nobreak > nul

:: Step 2: Launch the Unity Clone Engine Build with High Priority
echo [2/3] Launching Unity Clone Engine...
:: Replace the path below with your compiled Unity project executable location
start "" /High "%~dp0Build\Urielsmooth-livestream.exe"
timeout /t 5 /nobreak > nul

:: Step 3: Launch OBS Studio configured for streaming
echo [3/3] Launching Production Studio (OBS)...
:: Adjust path if your OBS is installed in a custom directory
start "" "C:\Program Files\obs-studio\bin\64bit\obs64.exe"

echo ===================================================
echo [SUCCESS] All systems active. Monitor GPU loads.
echo ===================================================
pause
