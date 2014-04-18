using System.Collections.Generic;
using System.Net;


namespace UOS
{
    public class TCPChannelManager : ChannelManager
    {
        private SocketDevice device;
        private IDictionary<string, TCPServerConnection> startedServers = new Dictionary<string, TCPServerConnection>();

        public TCPChannelManager(IPAddress localHost, string portRange)
        {
            device = new SocketDevice(
                localHost.ToString(),
                int.Parse(portRange.Split('-')[0]),
                EthernetConnectionType.TCP
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

        public NetworkDevice GetAvailableNetworkDevice()
        {
            return device;
        }
    }
}
