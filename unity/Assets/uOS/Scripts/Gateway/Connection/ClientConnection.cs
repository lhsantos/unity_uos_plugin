namespace UOS
{
    public abstract class ClientConnection
    {
        public NetworkDevice clientDevice { get; protected set; }

        public abstract bool connected { get; }

        public abstract byte[] Read();

        public abstract void Write(byte[] buffer, int offset, int size);

        public abstract void Close();

        public ClientConnection(NetworkDevice clientDevice)
        {
            this.clientDevice = clientDevice;
        }
    }
}
