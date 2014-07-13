using System.Collections.Generic;
using System.Net;


namespace UOS
{
    public class TCPChannelManager : ChannelManager
    {
        private int defaultPort;
        private List<NetworkDevice> passiveDevices = new List<NetworkDevice>();
        private int passiveIndex = 0;
        private IDictionary<string, TCPServerConnection> startedServers = new Dictionary<string, TCPServerConnection>();

        public TCPChannelManager(int defaultPort, string portRange)
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

            string localHost = IPAddress.Any.ToString();
            passiveDevices.Add(new SocketDevice(localHost, defaultPort, EthernetConnectionType.TCP));
            for (int i = lowerPort; i <= upperPort; ++i)
                passiveDevices.Add(new SocketDevice(localHost, i, EthernetConnectionType.TCP));
        }

        public string GetNetworkDeviceType()
        {
            return passiveDevices[0].networkDeviceType;
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

            return new TCPClientConnection(host, port);
        }

        public ClientConnection OpenPassiveConnection(string networkDeviceName)
        {
            string[] address = networkDeviceName.Split(':');

            if (address.Length != 2)
                throw new System.ArgumentException("Invalid parameters for creation of the channel.");

            TCPServerConnection server = null;
            if (!startedServers.TryGetValue(networkDeviceName, out server))
            {
                string host = address[0];
                int port = int.Parse(address[1]);

                server = new TCPServerConnection(new SocketDevice(host, port, EthernetConnectionType.UDP));
                startedServers[networkDeviceName] = server;
            }

            return server.Accept();
        }

        public List<string> ListHosts()
        {
            var ips = Util.GetLocalIPs();
            var result = new List<string>();
            foreach (var ip in ips)
                result.Add(ip.ToString());
            return result;
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
