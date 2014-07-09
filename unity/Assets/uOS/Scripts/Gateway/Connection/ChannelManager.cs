using System.Collections.Generic;


namespace UOS
{
    public interface ChannelManager
    {
        string GetNetworkDeviceType();

        ClientConnection OpenPassiveConnection(string networkDeviceName);

        ClientConnection OpenActiveConnection(string networkDeviceName);

        List<string> ListHosts();

        NetworkDevice GetAvailableNetworkDevice();

        void TearDown();
    }
}
