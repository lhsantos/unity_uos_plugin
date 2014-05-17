using System.Collections.Generic;
using UnityEngine;
using UOS;
using System.Net.Sockets;


[RequireComponent(typeof(uOS))]
public class TestUOS : MonoBehaviour, Logger
{
    string myLog = "";

    /// <summary>
    /// Called right before the first update.
    /// </summary>
    void Start()
    {
        //TcpListener tcpListener = new TcpListener(14984);
        //tcpListener.Start();
        //TcpClient client = tcpListener.AcceptTcpClient();
        //Debug.Log("aceitou!");
        //client.Close();
        //tcpListener.Stop();
        uOS.Init(this);
    }

    /// <summary>
    /// Called once every frame.
    /// </summary>
    void Update()
    {
    }

    void OnGUI()
    {
        myLog = GUI.TextArea(new Rect(10, 10, Screen.width - 10, Screen.height - 10), myLog);
    }



    public void Log(object message)
    {
        DoLog(message.ToString());
    }

    public void LogError(object message)
    {
        DoLog("ERROR: " + message);
    }

    public void LogException(System.Exception exception)
    {
        DoLog("ERROR: " + exception.StackTrace);
    }

    public void LogWarning(object message)
    {
        DoLog("WARNING: " + message);
    }

    private void DoLog(string msg)
    {
        myLog = msg + "\n" + myLog;
    }
}
