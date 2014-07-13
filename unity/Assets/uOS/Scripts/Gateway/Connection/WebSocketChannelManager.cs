using System.Collections.Generic;


namespace UOS
{
    public class WebSocketChannelManager : ChannelManager
    {
        private int defaultPort;
        private int timeout;
        private WebSocketDevice host;

        public WebSocketChannelManager(string hostname, int defaultPort, int timeout)
        {
            this.host = new WebSocketDevice(defaultPort, hostname);
            this.defaultPort = defaultPort;
            this.timeout = timeout;
        }

        public string GetNetworkDeviceType()
        {
            return WebSocketDevice.NETWORK_DEVICE_TYPE;
        }

        public ClientConnection OpenActiveConnection(string networkDeviceName)
        {
            string[] address = networkDeviceName.Split(':');

            string host;
            int port;
            if (address.Length == 1)
                port = defaultPort;
            else if (address.Length == 2)
                port = int.Parse(address[1]);
            else
                throw new System.ArgumentException("Invalid parameters for creation of the channel.");

            host = address[0];

            return new WebSocketClientConnection(host, port, timeout);
        }

        public ClientConnection OpenPassiveConnection(string networkDeviceName)
        {
            throw new System.NotImplementedException();
        }

        public List<string> ListHosts()
        {
            return new List<string>(new string[] { host.host });
        }

        public NetworkDevice GetAvailableNetworkDevice()
        {
            return host;
        }

        public void TearDown()
        {
        }
    }
}
