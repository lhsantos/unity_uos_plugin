using System.Collections.Generic;
using System.Net;


namespace UOS
{
    public class UDPChannelManager : ChannelManager
    {
        private SocketDevice device;
        private IDictionary<string, UDPServerConnection> startedServers = new Dictionary<string, UDPServerConnection>();

        public UDPChannelManager(IPAddress localHost, string portRange)
        {
            device = new SocketDevice(
                localHost.ToString(),
                int.Parse(portRange.Split('-')[0]),
                EthernetConnectionType.UDP
            );
        }

        public string GetNetworkDeviceType()
        {
            return device.networkDeviceType;
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

        public NetworkDevice GetAvailableNetworkDevice()
        {
            return device;
        }
    }
}
