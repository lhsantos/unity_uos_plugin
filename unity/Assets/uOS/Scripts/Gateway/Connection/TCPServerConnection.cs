namespace UOS
{
    public class TCPServerConnection : ServerConnection
    {

        public TCPServerConnection(SocketDevice networkDevice)
            : base(networkDevice)
        {
        }

        public override ClientConnection Accept()
        {
            throw new System.NotImplementedException();
        }

        public override void Close()
        {
            throw new System.NotImplementedException();
        }
    }
}
