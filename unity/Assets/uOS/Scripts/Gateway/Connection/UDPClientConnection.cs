using System.IO;
using System.Threading;
using UOS.Net;
using UOS.Net.Sockets;


namespace UOS
{
    public class UDPClientConnection : ClientConnection
    {
        private IPEndPoint peerAddress;
        private UdpClient udpClient;

        public override bool connected { get { return udpClient.Connected; } }


        public UDPClientConnection(string host, int port)
            : base(new SocketDevice(host, port, EthernetConnectionType.UDP))
        {
            peerAddress = new IPEndPoint(IPAddress.Parse(host), port);
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            udpClient.Connect(peerAddress);
        }

        public override byte[] Read()
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            throw new System.NotImplementedException();
        }

        public override void Close()
        {
            udpClient.Close();
        }
    }
}
