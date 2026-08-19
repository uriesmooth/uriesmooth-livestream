using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

public class PipeManager : MonoBehaviour
{
    private Process pythonProcess;
    private Thread pipeReadThread;
    private readonly object dataLock = new object();
    private string latestTrackingData = "";

    void Start()
    {
        // Start the background tracking thread
        pipeReadThread = new Thread(ReadFromProcessPipe);
        pipeReadThread.IsBackground = true;
        pipeReadThread.Start();
    }

    private void ReadFromProcessPipe()
    {
        try
        {
            pythonProcess = new Process();
            // Point this directly to your local python executable
            pythonProcess.StartInfo.FileName = "python.exe"; 
            // Pass the absolute path to your pipe tracking script
            pythonProcess.StartInfo.Arguments = $"\"{Application.streamingAssetsPath}/mediapipe_pipe.py\"";
            
            // Critical Settings: Disable OS Shell and open standard RAM pipes
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;
            pythonProcess.StartInfo.CreateNoWindow = true;

            pythonProcess.Start();

            using (StreamReader reader = pythonProcess.StandardOutput)
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        lock (dataLock)
                        {
                            latestTrackingData = line;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Pipe Processing Error: {e.Message}");
        }
    }

    // Replace the call in HolisticLandmarkProcessor.cs to fetch from this function instead
    public string GetLatestPipeData()
    {
        lock (dataLock)
        {
            return latestTrackingData;
        }
    }

    void OnApplicationQuit()
    {
        // Clean up the process when quitting so Python doesn't get stuck in memory
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
        }

        if (pipeReadThread != null && pipeReadThread.IsAlive)
        {
            pipeReadThread.Abort();
        }
    }
}
