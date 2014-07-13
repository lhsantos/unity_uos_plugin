using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UOS
{
    public class TCPClientConnection : ClientConnection
    {
        public const int TIMEOUT_TIME_MS = 20000;


        private TcpClient tcpClient;

        public override bool connected { get { return tcpClient.Connected; } }


        public TCPClientConnection(TcpClient tcpClient)
            : base(
                new SocketDevice(
                    ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString(),
                    ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port,
                    EthernetConnectionType.TCP
                )
            )
        {
            this.tcpClient = tcpClient;
        }

        public TCPClientConnection(string host, int port)
            : this(CreateClient(host, port)) { }

        private static TcpClient CreateClient(string host, int port)
        {
            var tcpClient = new TcpClient();
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tcpClient.Connect(host, port);
            tcpClient.ReceiveTimeout = TIMEOUT_TIME_MS;
            tcpClient.SendTimeout = TIMEOUT_TIME_MS;
            return tcpClient;
        }

        public override byte[] Read()
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanRead)
            {
                byte[] buffer = new byte[1024];
                List<byte> data = new List<byte>();
                do
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    data.Capacity = System.Math.Max(data.Count + read, data.Capacity);
                    for (int i = 0; i < read; ++i)
                        data.Add(buffer[i]);
                } while (stream.DataAvailable);

                return data.ToArray();
            }
            else
                throw new System.Exception("Can't read from this stream right now!");
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(buffer, offset, size);
            }
            else
                throw new System.Exception("Can't write to this stream right now!");
        }

        public override void Close()
        {
            tcpClient.Close();
        }
    }
}
