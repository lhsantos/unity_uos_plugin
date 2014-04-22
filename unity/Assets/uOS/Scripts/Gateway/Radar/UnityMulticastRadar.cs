using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UOS
{
    /// <summary>
    /// Implements a UOS Radar using ping.
    /// </summary>
    public class UnityMulticastRadar : UnityNetworkRadar
    {
        private struct ReceiveEvent
        {
            public IPEndPoint remoteEndPoint;
            public byte[] data;
        }

        /// <summary>
        /// The port to be used by this Radar.
        /// </summary>
        public int port = 14984;

        private UdpClient udpClient = null;

        private bool waiting;
        private System.DateTime receiveStart;
        private System.IAsyncResult receiveAsyncResult = null;
        private object _receive_lock = new object();
        private System.DateTime lastCheck;
        private HashSet<string> lastAddresses;
        private HashSet<string> knownAddresses = new HashSet<string>();

        public UnityMulticastRadar(Logger logger)
            : base(logger) { }

        public override void StartRadar()
        {
            base.StartRadar();

            if (udpClient == null)
            {
                lastCheck = System.DateTime.Now;
                lastAddresses = new HashSet<string>();

                udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.ExclusiveAddressUse = false;
                udpClient.EnableBroadcast = true;

                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                SendBeacon(new IPEndPoint(IPAddress.Broadcast, port));
            }
        }

        public override void StopRadar()
        {
            base.StopRadar();

            udpClient = null;
        }


        /// <summary>
        /// The main radar thread.
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (udpClient != null)
            {
                if (waiting)
                {
                    lock (_receive_lock)
                    {
                        if ((receiveAsyncResult != null) && (System.DateTime.Now.Subtract(receiveStart).Seconds > 10))
                        {
                            IPEndPoint ep = null;
                            udpClient.EndReceive(receiveAsyncResult, ref ep);
                            receiveAsyncResult = null;
                            logger.Log("Receive timout!");
                            ReceiveAnswers();
                        }
                    }
                }
                else
                    ReceiveAnswers();
            }
        }

        protected override void HandleEvent(object evt)
        {
            if (evt is ReceiveEvent)
            {
                ReceiveEvent e = (ReceiveEvent)evt;
                HandleBeacon(e.data, e.remoteEndPoint);

                SendBeacon(new IPEndPoint(IPAddress.Broadcast, port));
                CheckLeftDevices();
            }
            else if (evt is System.Exception)
            {
                throw (System.Exception)evt;
            }
        }

        private void SendBeacon(IPEndPoint endPoint)
        {
            waiting = true;
            udpClient.BeginSend(new byte[] { 1 }, 1, endPoint, new System.AsyncCallback(OnSendDone), udpClient);
        }

        private void OnSendDone(System.IAsyncResult ar)
        {
            UdpClient client = (UdpClient)ar.AsyncState;
            client.EndSend(ar);
            waiting = false;
        }

        private void ReceiveAnswers()
        {
            waiting = true;
            receiveStart = System.DateTime.Now;
            var t = new Thread(new ThreadStart(BeginReceive));
            t.Start();
        }

        private void BeginReceive()
        {
            try
            {
                lock (_receive_lock)
                {
                    receiveAsyncResult = udpClient.BeginReceive(new System.AsyncCallback(OnReceiveDone), udpClient);
                }
            }
            catch (System.Exception e) { PushEvent(e); }
        }

        private void OnReceiveDone(System.IAsyncResult ar)
        {
            ReceiveEvent e = new ReceiveEvent();
            UdpClient client = (UdpClient)ar.AsyncState;
            e.data = client.EndReceive(ar, ref e.remoteEndPoint);
            PushEvent(e);
            waiting = false;
            lock (_receive_lock)
            {
                receiveAsyncResult = null;
            }
        }

        private void HandleBeacon(byte[] message, IPEndPoint endPoint)
        {
            if (endPoint != null)
            {
                string address = endPoint.Address.ToString();
                if (!knownAddresses.Contains(address))
                {
                    knownAddresses.Add(address);
                    RaiseDeviceEntered(new SocketDevice(address, port, EthernetConnectionType.TCP));
                }
            }
        }

        private void CheckLeftDevices()
        {
            System.DateTime now = System.DateTime.Now;

            if (now.Subtract(lastCheck).Seconds > 30)
            {
                lastAddresses.RemoveWhere(a => knownAddresses.Contains(a));
                foreach (var address in lastAddresses)
                {
                    SocketDevice left = new SocketDevice(address, port, EthernetConnectionType.TCP);
                    RaiseDeviceLeft(left);
                }

                lastAddresses = knownAddresses;
                knownAddresses = new HashSet<string>();
                lastCheck = now;
            }
        }
    }
}
