import cv2
import json
import sys
import numpy as np
import mediapipe as mp

# Strict resolution cap minimizes memory bandwidth bottlenecks on GTX 1650
FRAME_WIDTH = 640  
FRAME_HEIGHT = 480

# CAP_DSHOW enforces low-latency frame presentation on Windows operating systems
cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)  
cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
cap.set(cv2.CAP_PROP_FPS, 30)

mp_holistic = mp.solutions.holistic
# Complexity 1 offers optimal stability for real-time tracking loops without stalling voice tasks
holistic = mp_holistic.Holistic(
    model_complexity=1, 
    smooth_landmarks=True, 
    min_detection_confidence=0.55, 
    min_tracking_confidence=0.55
)

try:
    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            continue

        # Color channel conversion for MediaPipe tracking engine core
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = holistic.process(frame_rgb)

        # PRIVACY & MEMORY PURGE: Overwrite camera matrices instantly inside system memory
        frame.fill(0)
        frame_rgb.fill(0)

        packet = {"pose": [], "left_hand": [], "right_hand": []}

        # Optimized Extraction: Pull only the precise vector subsets required by C# structures
        if results.pose_landmarks:
            packet["pose"] = [
                {"x": lm.x, "y": lm.y, "z": lm.z, "v": lm.visibility} 
                for lm in results.pose_landmarks.landmark
            ]
            
        if results.left_hand_landmarks:
            packet["left_hand"] = [
                {"x": lm.x, "y": lm.y, "z": lm.z} 
                for lm in results.left_hand_landmarks.landmark
            ]
            
        if results.right_hand_landmarks:
            packet["right_hand"] = [
                {"x": lm.x, "y": lm.y, "z": lm.z} 
                for lm in results.right_hand_landmarks.landmark
            ]

        # Compact packet layout generation with minimal character spacing
        json_string = json.dumps(packet, separators=(',', ':'))
        
        # Write to system standard output pipe using low-level RAM presentation mechanics
        sys.stdout.write(json_string + "\n")
        sys.stdout.flush()

finally:
    cap.release()
    holistic.close()

