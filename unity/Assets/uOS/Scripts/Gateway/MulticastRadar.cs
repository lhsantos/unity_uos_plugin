using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;


namespace UOS
{
    /// <summary>
    /// Implements a UOS Radar using ping.
    /// </summary>
    public class MulticastRadar : UnityRadar
    {
        /// <summary>
        /// The port to be used by this Radar.
        /// </summary>
        public int port = 14984;

        private UdpClient udpClient;

        private float lastCheck;
        private float now;
        private HashSet<string> lastAddresses;
        private HashSet<string> knownAddresses = new HashSet<string>();

        protected override void Awake()
        {
            base.Awake();
            now = Time.time;
        }
        protected override void Update()
        {
            base.Update();
            now = Time.time;
        }

        /// <summary>
        /// The main radar thread.
        /// </summary>
        protected override void RadarThread()
        {
            lastCheck = now;
            lastAddresses = new HashSet<string>();

            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10 * 1000); // 10 seconds
            udpClient.ExclusiveAddressUse = false;
            udpClient.EnableBroadcast = true;

            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

            SendBeacon(new IPEndPoint(IPAddress.Broadcast, port));

            while (running)
            {
                ReceiveAnswers();
                CheckLeftDevices();
            }

            udpClient.Close();
            udpClient = null;
        }


        private void SendBeacon(IPEndPoint endPoint)
        {
            udpClient.Send(new byte[] { 1 }, 1, endPoint);
        }

        private void ReceiveAnswers()
        {
            try
            {
                IPEndPoint remoteEndPoint = null;
                var buffer = udpClient.Receive(ref remoteEndPoint);
                HandleBeacon(buffer, remoteEndPoint);
            }
            catch (SocketException) {/* timeout is expected to happen */}
        }

        private void HandleBeacon(byte[] message, IPEndPoint endPoint)
        {
            if (endPoint != null)
            {
                string address = endPoint.Address.ToString();
                if (!knownAddresses.Contains(address))
                {
                    NetworkDevice found = new NetworkDevice(address, port, EthernetConnectionType.TCP);
                    RaiseDeviceEntered(found);
                    SendBeacon(endPoint);
                    knownAddresses.Add(address);
                }
            }
        }

        private void CheckLeftDevices()
        {
            if ((now - lastCheck) > 30)
            {
                SendBeacon(new IPEndPoint(IPAddress.Broadcast, port));
                lastAddresses.RemoveWhere(a => knownAddresses.Contains(a));
                foreach (var address in lastAddresses)
                {
                    NetworkDevice left = new NetworkDevice(address, port, EthernetConnectionType.TCP);
                    RaiseDeviceLeft(left);
                }

                lastAddresses = knownAddresses;
                knownAddresses = new HashSet<string>();
                lastCheck = now;
            }
        }
    }
}
