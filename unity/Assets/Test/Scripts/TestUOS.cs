using System.Collections.Generic;
using UnityEngine;
using UOS;
using System.Net.Sockets;


[RequireComponent(typeof(uOS))]
public class TestUOS : MonoBehaviour
{
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
        uOS.Init();
    }

    /// <summary>
    /// Called once every frame.
    /// </summary>
    void Update()
    {
    }
}
