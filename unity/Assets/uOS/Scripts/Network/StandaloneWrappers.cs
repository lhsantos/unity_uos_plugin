//#if UNITY_EDITOR || UNITY_STANDALONE

namespace UOS.Net
{
    public class IPAddress
    {
        public static readonly IPAddress Any = new IPAddress(System.Net.IPAddress.Any);
        public static readonly IPAddress Broadcast = new IPAddress(System.Net.IPAddress.Broadcast);


        public System.Net.IPAddress internalAddress { get; private set; }
        public IPAddress(System.Net.IPAddress address)
        {
            this.internalAddress = address;
        }

        public override string ToString()
        {
            return internalAddress.ToString();
        }

        public static IPAddress Parse(string ipString)
        {
            return new IPAddress(System.Net.IPAddress.Parse(ipString));
        }

        public static string GetLocalHostName()
        {
            return System.Net.Dns.GetHostName();
        }

        public static IPAddress GetLocal()
        {
            return new IPAddress(
                System.Array.Find<System.Net.IPAddress>(
                    System.Net.Dns.GetHostEntry(GetLocalHostName()).AddressList,
                    a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                )
            );
        }
    }

    public class IPEndPoint
    {
        public System.Net.IPEndPoint internalEP { get; private set; }
        public IPEndPoint(System.Net.IPEndPoint ep)
        {
            internalEP = ep;
        }
        public IPEndPoint(IPAddress address, int port)
            : this(new System.Net.IPEndPoint(address.internalAddress, port)) { }

        public IPAddress Address
        {
            get { return new IPAddress(internalEP.Address); }
            set { internalEP.Address = value.internalAddress; }
        }

        public override string ToString()
        {
            return internalEP.ToString();
        }
    }
}

namespace UOS.Net.Sockets
{
    public class NetworkStream
    {
        public System.Net.Sockets.NetworkStream internalStream { get; private set; }
        public NetworkStream(System.Net.Sockets.NetworkStream stream)
        {
            internalStream = stream;
        }

        public bool CanRead { get { return internalStream.CanRead; } }

        public bool CanWrite { get { return internalStream.CanWrite; } }

        public bool DataAvailable { get { return internalStream.DataAvailable; } }

        public int Read(byte[] buffer, int offset, int size)
        {
            return internalStream.Read(buffer, offset, size);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            internalStream.Write(buffer, offset, size);
        }
    }

    public class TcpClient
    {
        private System.Net.Sockets.TcpClient internalClient = null;
        public TcpClient(System.Net.Sockets.TcpClient client)
        {
            this.internalClient = client;
        }
        public TcpClient()
            : this(new System.Net.Sockets.TcpClient()) { }

        public string Host
        {
            get { return ((System.Net.IPEndPoint)internalClient.Client.LocalEndPoint).Address.ToString(); }
        }

        public int Port
        {
            get { return ((System.Net.IPEndPoint)internalClient.Client.LocalEndPoint).Port; }
        }

        public bool Connected { get { return internalClient.Connected; } }

        public int ReceiveTimeout
        {
            get { return internalClient.ReceiveTimeout; }
            set { internalClient.ReceiveTimeout = value; }
        }

        public int SendTimeout
        {
            get { return internalClient.SendTimeout; }
            set { internalClient.SendTimeout = value; }
        }

        public bool ReuseAddress
        {
            get
            {
                return (bool)internalClient.Client.GetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress);
            }

            set
            {
                internalClient.Client.SetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress,
                    value);
            }
        }


        public void Connect(string host, int port)
        {
            internalClient.Connect(host, port);
        }

        public NetworkStream GetStream()
        {
            return new NetworkStream(internalClient.GetStream());
        }

        public void Close()
        {
            internalClient.Close();
        }
    }

    public class TcpListener
    {
        private System.Net.Sockets.TcpListener listener;
        public TcpListener(string host, int port)
        {
            this.listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse(host), port);
        }

        public bool ReuseAddress
        {
            get
            {
                return (bool)listener.Server.GetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress);
            }

            set
            {
                listener.Server.SetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress,
                    value);
            }
        }

        public TcpClient AcceptTcpClient()
        {
            return new TcpClient(listener.AcceptTcpClient());
        }

        public bool Pending()
        {
            return listener.Pending();
        }

        public void Start()
        {
            listener.Start();
        }

        public void Stop()
        {
            listener.Stop();
        }
    }

    public class UdpClient
    {
        private System.Net.Sockets.UdpClient internalClient = null;
        public UdpClient()
        {
            this.internalClient = new System.Net.Sockets.UdpClient();
        }

        public UdpClient(IPEndPoint ep)
        {
            this.internalClient = new System.Net.Sockets.UdpClient(ep.internalEP);
        }

        public bool EnableBroadcast
        {
            get { return internalClient.EnableBroadcast; }
            set { internalClient.EnableBroadcast = value; }
        }

        public bool Connected { get { return internalClient.Client.Connected; } }

        public bool ReuseAddress
        {
            get
            {
                return (bool)internalClient.Client.GetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress);
            }

            set
            {
                internalClient.Client.SetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress,
                    value);
            }
        }

        public int ReceiveTimeout
        {
            get { return internalClient.Client.ReceiveTimeout; }
            set { internalClient.Client.ReceiveTimeout = value; }
        }

        public void Bind(IPEndPoint localEP)
        {
            internalClient.Client.Bind(localEP.internalEP);
        }

        public void Connect(IPEndPoint endPoint)
        {
            internalClient.Connect(endPoint.internalEP);
        }

        public System.IAsyncResult BeginSend(byte[] datagram, int bytes, IPEndPoint ep, System.AsyncCallback requestCallback, object state)
        {
            return internalClient.BeginSend(datagram, bytes, ep.internalEP, requestCallback, state);
        }

        public int EndSend(System.IAsyncResult asyncResult)
        {
            return internalClient.EndSend(asyncResult);
        }

        public byte[] Receive(ref IPEndPoint remoteEP)
        {
            try
            {
                System.Net.IPEndPoint aux = null;
                byte[] data = internalClient.Receive(ref aux);
                remoteEP = new IPEndPoint(aux);
                return data;
            }
            catch (System.Net.Sockets.SocketException e)
            {
                if (e.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    throw new SocketTimoutException(e);
                throw;
            }
        }

        public void Close()
        {
            internalClient.Close();
        }
    }
}

//#endif
