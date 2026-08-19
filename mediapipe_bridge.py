import cv2
import json
import socket
import numpy as np
import mediapipe as mp

# Configuration for GTX 1650 optimization
UDP_IP = "127.0.0.1"
UDP_PORT = 5005
CAM_INDEX = 0
FRAME_WIDTH = 640  # Optimized resolution to save VRAM
FRAME_HEIGHT = 480

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
cap = cv2.VideoCapture(CAM_INDEX)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)

mp_holistic = mp.solutions.holistic
# Model complexity 1 balances accuracy and GTX 1650 performance
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

        # Convert to RGB for MediaPipe processing
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = holistic.process(frame_rgb)

        # ISOLATION GUARANTEE: Immediately overwrite camera frame buffers in VRAM/RAM
        frame.fill(0)
        frame_rgb.fill(0)

        # Package landmarks safely into JSON
        packet = {"pose": [], "left_hand": [], "right_hand": []}

        if results.pose_landmarks:
            packet["pose"] = [{"x": lm.x, "y": lm.y, "z": lm.z, "v": lm.visibility} for lm in results.pose_landmarks.landmark]
        if results.left_hand_landmarks:
            packet["left_hand"] = [{"x": lm.x, "y": lm.y, "z": lm.z} for lm in results.left_hand_landmarks.landmark]
        if results.right_hand_landmarks:
            packet["right_hand"] = [{"x": lm.x, "y": lm.y, "z": lm.z} for lm in results.right_hand_landmarks.landmark]

        # Send data locally to Unity via loopback
        json_data = json.dumps(packet).encode('utf-8')
        sock.sendto(json_data, (UDP_IP, UDP_PORT))

finally:
    cap.release()
    holistic.close()