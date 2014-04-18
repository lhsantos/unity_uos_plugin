namespace UOS
{
    public abstract class ServerConnection
    {
        public NetworkDevice networkDevice { get; protected set; }

        public abstract ClientConnection Accept();

        public abstract void Close();

        public ServerConnection(NetworkDevice networkDevice)
        {
            this.networkDevice = networkDevice;
        }
    }
}
