using System.Threading;
using UOS.Net;
using UOS.Net.Sockets;


namespace UOS
{
    public class TCPServerConnection : ServerConnection
    {
        private TcpListener tcpListener;
        private bool running;

        public TCPServerConnection(SocketDevice networkDevice)
            : base(networkDevice)
        {
            tcpListener = new TcpListener(networkDevice.host, networkDevice.port);
            tcpListener.ReuseAddress = true;
            tcpListener.Start();
            running = true;
        }

        public override ClientConnection Accept()
        {
            while (running)
            {
                if (tcpListener.Pending())
                    return new TCPClientConnection(tcpListener.AcceptTcpClient());
                else
                    Thread.Sleep(100);
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
