namespace UOS
{
    /// <summary>
    /// What are the possible types of connections over ethernet?
    /// </summary>
    public enum EthernetConnectionType
    {
        TCP,
        UDP,
        RTP
    }

    public class SocketDevice : NetworkDevice
    {
        private const string NETWORK_DEVICE_TYPE = "Ethernet";

        public string host { get; private set; }
        public int port { get; private set; }
        public EthernetConnectionType connectionType { get; private set; }

        public override string networkDeviceName { get { return host + ":" + port; } }
        public override string networkDeviceType { get { return NETWORK_DEVICE_TYPE + ":" + connectionType.ToString(); } }

        public SocketDevice(string host, int port, EthernetConnectionType connectionType)
        {
            this.host = host;
            this.port = port;
            this.connectionType = connectionType;
        }
    }
}
