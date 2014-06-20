using System.Collections.Generic;
using UOS.Net;


namespace UOS
{
    public class UDPChannelManager : ChannelManager
    {
        private int defaultPort;
        private List<NetworkDevice> passiveDevices = new List<NetworkDevice>();
        private int passiveIndex = 0;
        private IDictionary<string, UDPServerConnection> startedServers = new Dictionary<string, UDPServerConnection>();

        public UDPChannelManager(IPAddress localIP, int defaultPort, string portRange)
        {
            this.defaultPort = defaultPort;

            int lowerPort, upperPort;
            try
            {
                string[] range = portRange.Split('-');
                lowerPort = int.Parse(range[0]);
                upperPort = int.Parse(range[1]);

                if (upperPort < lowerPort)
                {
                    lowerPort = upperPort = defaultPort;
                }
            }
            catch (System.Exception)
            {
                lowerPort = upperPort = defaultPort;
            }

            string localHost = localIP.ToString();
            passiveDevices.Add(new SocketDevice(localHost, defaultPort, EthernetConnectionType.UDP));
            for (int i = lowerPort; i <= upperPort; ++i)
                passiveDevices.Add(new SocketDevice(localHost, i, EthernetConnectionType.UDP));
        }

        public string GetNetworkDeviceType()
        {
            return passiveDevices[0].networkDeviceType;
        }

        public ClientConnection OpenActiveConnection(string networkDeviceName)
        {
            string[] address = networkDeviceName.Split(':');

            if (address.Length != 2)
                throw new System.ArgumentException("Invalid parameters for creation of the channel.");

            string host = address[0];
            int port = int.Parse(address[1]);

            return new UDPClientConnection(host, port);
        }

        public ClientConnection OpenPassiveConnection(string networkDeviceName)
        {
            string[] address = networkDeviceName.Split(':');

            if (address.Length != 2)
                throw new System.ArgumentException("Invalid parameters for creation of the channel.");

            UDPServerConnection server = null;
            if (!startedServers.TryGetValue(networkDeviceName, out server))
            {
                string host = address[0];
                int port = int.Parse(address[1]);

                server = new UDPServerConnection(new SocketDevice(host, port, EthernetConnectionType.UDP));
                startedServers[networkDeviceName] = server;
            }

            return server.Accept();
        }

        public List<NetworkDevice> ListNetworkDevices()
        {
            return new List<NetworkDevice>(passiveDevices);
        }

        public NetworkDevice GetAvailableNetworkDevice()
        {
            NetworkDevice device = passiveDevices[passiveIndex];
            passiveIndex = (passiveIndex + 1) % passiveDevices.Count;
            return device;
        }

        public void TearDown()
        {
            foreach (var s in startedServers.Values)
                s.Close();
        }
    }
}
