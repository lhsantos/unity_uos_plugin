namespace UOS
{
    public class WebSocketDevice : NetworkDevice
    {
        public const string NETWORK_DEVICE_TYPE = "WebSocket";

        public string host { get; private set; }
        public int port { get; private set; }

        public override string networkDeviceName { get { return host + ":" + port; } }
        public override string networkDeviceType { get { return NETWORK_DEVICE_TYPE; } }


        public WebSocketDevice(int port, string localHostName = "localhost")
        {
            this.host = localHostName;
            this.port = port;
        }
    }
}
