using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace UOS
{
    public class TCPServerConnection : ServerConnection
    {
        private TcpListener tcpListener;
        private bool running;

        public TCPServerConnection(SocketDevice networkDevice)
            : base(networkDevice)
        {
            tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, networkDevice.port));
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tcpListener.Start();
            running = true;
        }

        public override ClientConnection Accept()
        {
            while (running)
            {
                if (tcpListener.Pending())
                    return new TCPClientConnection(tcpListener.AcceptTcpClient());
                Thread.Sleep(50);
            }

            return null;
        }

        public override void Close()
        {
            running = false;
            tcpListener.Stop();
        }
    }
}
