import cv2
import json
import sys
import numpy as np
import mediapipe as mp

# Optimize resolution for GTX 1650 VRAM efficiency
FRAME_WIDTH = 640  
FRAME_HEIGHT = 480

cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)

mp_holistic = mp.solutions.holistic
holistic = mp_holistic.Holistic(
    model_complexity=1, 
    smooth_landmarks=True, 
    min_detection_confidence=0.5, 
    min_tracking_confidence=0.5
)

try:
    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            continue

        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = holistic.process(frame_rgb)

        # PRIVACY ASSURANCE: Overwrite frame arrays immediately in memory
        frame.fill(0)
        frame_rgb.fill(0)

        packet = {"pose": [], "left_hand": [], "right_hand": []}

        if results.pose_landmarks:
            packet["pose"] = [{"x": lm.x, "y": lm.y, "z": lm.z, "v": lm.visibility} for lm in results.pose_landmarks.landmark]
        if results.left_hand_landmarks:
            packet["left_hand"] = [{"x": lm.x, "y": lm.y, "z": lm.z} for lm in results.left_hand_landmarks.landmark]
        if results.right_hand_landmarks:
            packet["right_hand"] = [{"x": lm.x, "y": lm.y, "z": lm.z} for lm in results.right_hand_landmarks.landmark]

        # Convert to a single text line and flush immediately out of the process pipe
        json_string = json.dumps(packet)
        sys.stdout.write(json_string + "\n")
        sys.stdout.flush()

finally:
    cap.release()
    holistic.close()
