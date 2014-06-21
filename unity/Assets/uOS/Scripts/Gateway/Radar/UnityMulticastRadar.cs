using System.Collections.Generic;
using System.Threading;
using UOS.Net;
using UOS.Net.Sockets;


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
        private IPEndPoint localEndPoint;

        private System.DateTime lastCheck;
        private HashSet<string> lastAddresses;
        private HashSet<string> knownAddresses = new HashSet<string>();

        public UnityMulticastRadar(Logger logger)
            : base(logger) { }

        public override void Init()
        {
            base.Init();

            if (udpClient == null)
            {
                lastCheck = System.DateTime.Now;
                lastAddresses = new HashSet<string>();

                udpClient = new UdpClient();
                udpClient.ReuseAddress = true;
                udpClient.EnableBroadcast = true;
                udpClient.Bind(new IPEndPoint(IPAddress.Any, port));
                udpClient.ReceiveTimeout = 10 * 1000; // ten seconds

                localEndPoint = new IPEndPoint(IPAddress.GetLocal(), port);

                SendBeacon();

                var t = new Thread(new ThreadStart(
                    delegate()
                    {
                        while (udpClient != null)
                        {
                            try
                            {
                                IPEndPoint endPoint = null;
                                byte[] msg = udpClient.Receive(ref endPoint);
                                if (!localEndPoint.Equals(endPoint))
                                    PushEvent(new ReceiveEvent() { data = msg, remoteEndPoint = endPoint });
                            }
                            catch (SocketTimoutException)
                            {
                                // Timeout is expected!
                                SendBeacon();
                            }
                            catch (System.Exception e)
                            {
                                PushEvent(e);
                            }
                        }
                    }
                ));
                t.Start();
            }
        }

        public override void TearDown()
        {
            base.TearDown();

            udpClient.Close();
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
                System.DateTime now = System.DateTime.Now;
                if (now.Subtract(lastCheck).Seconds > 30)
                {
                    SendBeacon();
                    CheckLeftDevices();

                    lastCheck = now;
                }
            }
        }

        protected override void HandleEvent(object evt)
        {
            if (evt is ReceiveEvent)
            {
                ReceiveEvent e = (ReceiveEvent)evt;
                HandleBeacon(e.data, e.remoteEndPoint);
            }
            else if (evt is System.Exception)
                throw (System.Exception)evt;
        }

        private void SendBeacon()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, port);
            udpClient.BeginSend(new byte[] { 1 }, 1, endPoint, new System.AsyncCallback(OnSendDone), udpClient);
        }

        private void OnSendDone(System.IAsyncResult ar)
        {
            UdpClient client = (UdpClient)ar.AsyncState;
            client.EndSend(ar);
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
            lastAddresses.RemoveWhere(a => knownAddresses.Contains(a));
            foreach (var address in lastAddresses)
            {
                SocketDevice left = new SocketDevice(address, port, EthernetConnectionType.TCP);
                RaiseDeviceLeft(left);
            }

            lastAddresses = knownAddresses;
            knownAddresses = new HashSet<string>();
        }
    }
}
