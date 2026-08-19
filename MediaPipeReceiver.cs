using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class MediaPipeReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private const int port = 5005;
    
    [HideInInspector]
    public string lastReceivedPacket = "";
    private readonly object lockObject = new object();

    void Start()
    {
        udpClient = new UdpClient(port);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                
                lock (lockObject)
                {
                    lastReceivedPacket = text;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"UDP Receive Error: {e}");
            }
        }
    }

    public string GetLatestPacket()
    {
        lock (lockObject)
        {
            return lastReceivedPacket;
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
        if (udpClient != null) udpClient.Close();
    }
}