namespace UOS
{
    public class UDPServerConnection : ServerConnection
    {

        public UDPServerConnection(SocketDevice networkDevice)
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
