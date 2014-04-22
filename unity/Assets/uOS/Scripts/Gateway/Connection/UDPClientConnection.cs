using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UOS
{
    public class UDPClientConnection : ClientConnection
    {
        private IPEndPoint peerAddress;
        private UdpClient udpClient;

        public override bool connected { get { return udpClient.Client.Connected; } }


        public UDPClientConnection(string host, int port)
            : base(new SocketDevice(host, port, EthernetConnectionType.UDP))
        {
            peerAddress = new IPEndPoint(IPAddress.Parse(host), port);
            udpClient = new UdpClient(peerAddress);
            udpClient.Connect(peerAddress);
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            throw new System.NotImplementedException();
        }

        public override void ReadAsync(ClientConnection.ReadCallback callback, object callerState)
        {
            Thread t = new Thread(new ThreadStart(
                delegate()
                {
                    try
                    {
                        udpClient.BeginReceive(
                            new System.AsyncCallback(
                                delegate(System.IAsyncResult ar)
                                {
                                    IPEndPoint ep = null;
                                    byte[] bytes = udpClient.EndReceive(ar, ref ep);
                                    callback(bytes, ar.AsyncState, null);
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

        public override void WriteAsync(byte[] buffer, WriteCallback callback, object callerState)
        {
            udpClient.BeginSend(
                buffer, buffer.Length,
                new System.AsyncCallback(
                    delegate(System.IAsyncResult ar)
                    {
                        int written = udpClient.EndSend(ar);
                        callback(written, ar.AsyncState, null);
                    }
                ),
                callerState
            );
        }

        public override void Close()
        {
            udpClient.Close();
        }
    }
}
