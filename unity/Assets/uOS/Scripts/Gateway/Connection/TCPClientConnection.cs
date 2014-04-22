using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UOS
{
    public class TCPClientConnection : ClientConnection
    {
        public const int TIMEOUT_TIME_MS = 10000;


        private TcpClient tcpClient;

        public override bool connected { get { return tcpClient.Connected; } }


        public TCPClientConnection(TcpClient tcpClient)
            : base(new SocketDevice((IPEndPoint)tcpClient.Client.LocalEndPoint, EthernetConnectionType.TCP))
        {
            this.tcpClient = tcpClient;
        }

        public TCPClientConnection(string host, int port)
            : this(CreateClient(host, port)) { }

        private static TcpClient CreateClient(string host, int port)
        {
            var tcpClient = new TcpClient();
            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tcpClient.Connect(new IPEndPoint(IPAddress.Parse(host), port));
            tcpClient.ReceiveTimeout = TIMEOUT_TIME_MS;
            tcpClient.SendTimeout = TIMEOUT_TIME_MS;
            return tcpClient;
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanRead)
            {
                return stream.Read(buffer, offset, size);
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

        public override void ReadAsync(ReadCallback callback, object callerState)
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanRead)
            {
                Thread t = new Thread(new ThreadStart(delegate()
                {
                    try
                    {
                        byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            byte[] aux = new byte[read];
                            System.Array.Copy(buffer, aux, read);
                            buffer = aux;
                        }
                        else
                            buffer = null;

                        callback(buffer, callerState, null);
                    }
                    catch (System.Exception e)
                    {
                        callback(null, callerState, e.InnerException);
                    }
                }));
                t.Start();
            }
            else
                throw new System.Exception("Can't read from this stream right now!");
        }

        public override void WriteAsync(byte[] buffer, WriteCallback callback, object callerState)
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanWrite)
            {
                Thread t = new Thread(new ThreadStart(delegate()
                {
                    try
                    {
                        stream.Write(buffer, 0, buffer.Length);
                        callback(buffer.Length, callerState, null);
                    }
                    catch (System.Exception e)
                    {
                        callback(0, callerState, e.InnerException);
                    }
                }));
                t.Start();
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
