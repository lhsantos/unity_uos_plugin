using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UOS
{
    public class TCPClientConnection : ClientConnection
    {
        private TcpClient tcpClient;

        public override bool connected { get { return tcpClient.Connected; } }


        public TCPClientConnection(string host, int port)
            : base(new SocketDevice(host, port, EthernetConnectionType.TCP))
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(new IPEndPoint(IPAddress.Parse(host), port));
        }

        public override void ReadAsync(ReadCallback callback, object callerState)
        {
            NetworkStream stream = tcpClient.GetStream();
            if (stream.CanRead)
            {
                Thread t = new Thread(new ThreadStart(
                    delegate()
                    {
                        try
                        {
                            byte[] bytes = new byte[tcpClient.ReceiveBufferSize];
                            stream.BeginRead(bytes, 0, bytes.Length,
                                new System.AsyncCallback(
                                    delegate(System.IAsyncResult ar)
                                    {
                                        int read = stream.EndRead(ar);
                                        byte[] aux = null;
                                        if (read > 0)
                                        {
                                            aux = new byte[read];
                                            System.Array.Copy(bytes, aux, read);
                                        }

                                        callback(aux, ar.AsyncState, null);
                                    }
                                ),
                                callerState
                            );
                        }
                        catch (System.Exception e) { callback(null, callerState, e); }
                    }
                ));
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
                Thread t = new Thread(new ThreadStart(
                    delegate()
                    {
                        try
                        {
                            stream.BeginWrite(buffer, 0, buffer.Length,
                                new System.AsyncCallback(
                                    delegate(System.IAsyncResult ar)
                                    {
                                        stream.EndWrite(ar);
                                        callback(buffer.Length, ar.AsyncState, null);
                                    }
                                ),
                                callerState
                            );
                        }
                        catch (System.Exception e) { callback(0, callerState, e); }
                    }
                ));
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
